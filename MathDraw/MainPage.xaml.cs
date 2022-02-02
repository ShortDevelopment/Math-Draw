using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MathDraw
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        bool initialized = false;
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            initialized = true;
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("inputSettings"))
            {

                ApplicationDataCompositeValue inputSettings = roamingSettings.Values["inputSettings"] as ApplicationDataCompositeValue;
                MouseInputCheckBox.IsChecked = (bool)inputSettings["MouseInput"];
                TouchInputCheckBox.IsChecked = (bool)inputSettings["TouchInput"];
            }
            else
            {
                MainCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Touch | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
                SettingsTeachingTip1.IsOpen = true;
            }
        }

        #region CommandBar
        private void UndoAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var strokeContainer = MainCanvas.InkPresenter.StrokeContainer;
            InkStroke stroke = strokeContainer.GetStrokes().LastOrDefault();
            if (stroke != null)
            {
                stroke.Selected = true;
                strokeContainer.DeleteSelected();
            }
        }

        private async void CopyAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            IRandomAccessStream stream = await RenderCanvasToStream();
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
            Clipboard.SetContent(dataPackage);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker picker = new FileSavePicker();
            picker.FileTypeChoices.Add("Image", new List<string>() { ".jpg" });
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                IRandomAccessStream stream = await RenderCanvasToStream();
                using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    var buffer = new byte[(int)stream.Size];
                    reader.ReadBytes(buffer);
                    await FileIO.WriteBytesAsync(file, buffer);
                }
            }
        }

        #endregion

        private async Task<IRandomAccessStream> RenderCanvasToStream(int dpi = 200)
        {
            IRandomAccessStream stream = new InMemoryRandomAccessStream();
            using (CanvasDevice device = CanvasDevice.GetSharedDevice())
            using (CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)MainCanvas.ActualWidth, (int)MainCanvas.ActualHeight, dpi))
            {
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    ds.DrawInk(MainCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }
                await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Jpeg);
            }

            stream.Seek(0);
            return stream;
        }

        private void InputSettingsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!initialized)
                return;

            CoreInputDeviceTypes allowedInputDevices = CoreInputDeviceTypes.Pen;
            if (MouseInputCheckBox.IsChecked.HasValue && MouseInputCheckBox.IsChecked.Value)
                allowedInputDevices = allowedInputDevices | CoreInputDeviceTypes.Mouse;
            if (TouchInputCheckBox.IsChecked.HasValue && TouchInputCheckBox.IsChecked.Value)
                allowedInputDevices = allowedInputDevices | CoreInputDeviceTypes.Touch;
            MainCanvas.InkPresenter.InputDeviceTypes = allowedInputDevices;

            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue inputSettings = new ApplicationDataCompositeValue();
            inputSettings["MouseInput"] = MouseInputCheckBox.IsChecked;
            inputSettings["TouchInput"] = TouchInputCheckBox.IsChecked;
            roamingSettings.Values["InputSettings"] = inputSettings;
        }
    }
}
