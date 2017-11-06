using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace IdentifyApp
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //개체 초기화
        MediaCapture mediaCapture;
        bool isPreviewing;
        

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void TakePhotoAsync()
        {
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();
            //mediaCapture.Failed += MediaCapture_Failed;

            // Prepare and capture photo
            // 캡쳐한 사진을 바로 XAML에서 보여줌
            var lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));

            var capturedPhoto = await lowLagCapture.CaptureAsync();
            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;

            await lowLagCapture.FinishAsync();

            //임시로 저장된 거를 XAML에서 표시
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
            softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);

            // Set the source of the Image control
            imageControl.Source = source;
        }

        

        public async void takePhoto()
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
           // captureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }
            StorageFolder destinationFolder =
            await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePhotoFolder",
                CreationCollisionOption.OpenIfExists);

            await photo.CopyAsync(destinationFolder, "ProfilePhoto.jpg", NameCollisionOption.ReplaceExisting);
            //await photo.DeleteAsync();


            IRandomAccessStream stream = await photo.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            imageControl.Source = bitmapSource;
        }

        private void takePhotoBtnClicked(object sender, RoutedEventArgs e)
        {
            takePhoto();
            //TakePhotoAsync();
           
        }

        private async void confirmBtnClicked(object sender, RoutedEventArgs e)
        {            
            //확인 버튼을 누를 경우 - 이미지 파일로 저장
            //var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
            //StorageFile file = await myPictures.SaveFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);

            //using (var captureStream = new InMemoryRandomAccessStream())
            //{
            //    await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

            //    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            //    {
            //        var decoder = await BitmapDecoder.CreateAsync(captureStream);
            //        var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

            //        var properties = new BitmapPropertySet {
            //            { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) }
            //        };
            //        await encoder.BitmapProperties.SetPropertiesAsync(properties);

            //        await encoder.FlushAsync();
            //    }
            //}

            //다음 페이지로 이동
            Frame.Navigate(typeof(FrontViewPage));
        }

        private void retakeBtnClicked(object sender, RoutedEventArgs e)
        {
            takePhoto();
            //TakePhotoAsync();
        }

        
    }
}
