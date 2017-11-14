using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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

using IdentifyApp;
using IdentifyApp.Models;
using Newtonsoft.Json;
using IdentifyApp.Helpers;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace IdentifyApp
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //개체 초기화
        private StorageFolder _captureFolder = null;
        
        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;
        private string imagePath;
        private string imageFileName = "idcard.jpg";

        Person person = new Person();


        public MainPage()
        {
            this.InitializeComponent();            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Suspending += Application_Resuming;

            await InitializeCameraAsync();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Application.Current.Suspending -= Application_Suspending;
            Application.Current.Suspending -= Application_Resuming;

            await InitializeCameraAsync();
        }

        private void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                await InitializeCameraAsync();
                deferral.Complete();
            });
        }

        private void Application_Resuming(object sender, object o)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                await InitializeCameraAsync();
            });
        }
        private async Task InitializeCameraAsync()
        {
            Debug.WriteLine("InitializeCameraAsync");
            if(_mediaCapture == null)
            {
                var cameraDevice = await CameraHelper.FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);

                if(cameraDevice == null)
                {
                    Debug.WriteLine("No camera device found!");
                    return;
                }

                _mediaCapture = new MediaCapture();
                _mediaCapture.Failed += MediaCapture_Failed;
                
                try
                {
                    await _mediaCapture.InitializeAsync();
                    _isInitialized = true;

                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("카메라 접근 권한이 없어서 종료되었습니다.");
                }

                if (_isInitialized)
                {                    
                    await StartPreviewAsync();
                }
            }            
        }

        private async Task StartPreviewAsync()
        {
            // Set the preview source in the UI and mirror it if necessary
            PreviewControl.Source = _mediaCapture;
            
            // preview 시작
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;
        }


        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.WriteLine("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message);

            await CleanupCameraAsync();

            //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateCaptureControls());
        }

        private async Task CleanupCameraAsync()
        {
            Debug.WriteLine("CleanupCameraAsync");

            if (_isInitialized)
            {
                if (_isPreviewing)
                {
                    await StopPreviewAsync();
                }
                _isInitialized = false;
            }
        }

        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            await _mediaCapture.StopPreviewAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PreviewControl.Source = null;

            });
        }

        private async void TakePhotoAsync()
        {
            var stream = new InMemoryRandomAccessStream(); ;

            Debug.WriteLine("Taking Photo...");

            var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            _captureFolder = picturesLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;

            await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

            try
            {                
                var file = await _captureFolder.CreateFileAsync(imageFileName, CreationCollisionOption.ReplaceExisting);
                Debug.WriteLine("사진이 찍혔고, 다음 위치에 저장될 예정입니다 : " + file.Path);
                imagePath = file.Path;                

                //XAML에서 사진 미리 보여주고 뒤이어 바로 사진저장
                await CameraHelper.ReencodeAndSavePhotoAsync(stream, file);
                Debug.WriteLine("사진이 저장되었습니다");

                ConfirmBtn.Visibility = Visibility.Visible;

                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(fileStream);
                    imageControl.Source = bitmapImage;
                    
                }
                Debug.WriteLine("사진을 미리봅니다");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("사진을 찍는동안 문제가 발생하였습니다: " + ex.ToString());
            }            
        }

        private void takePhotoBtnClicked(object sender, RoutedEventArgs e)
        {            
            TakePhotoAsync();           
        }

        private async void confirmBtnClicked(object sender, RoutedEventArgs e)
        {
            // api/idcard 요청 및 faceId 저장
            await Task.Run(() =>
            {
                httpPost();
            });
                        
            if (person.faceId != null)
            {
                await CleanupCameraAsync();

                // 다음 페이지로 이동
                Frame.Navigate(typeof(PhotoPage), person);                
            }            
        }

        private void httpPost()
        {
            // 찍은 사진을 불러온 후..
           using (var fileStream = new FileStream(imagePath, FileMode.Open))
            {
                HttpContent fileStreamContent = new StreamContent(fileStream);
                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {
                    string requestUrl = App.baseUrl + "/idcard";

                    formData.Add(fileStreamContent, imageFileName, imageFileName);
                    var response = client.PostAsync(requestUrl, formData).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("Http 요청에 실패했습니다.");
                    }

                    response.EnsureSuccessStatusCode();
                    client.Dispose();

                    // JSON 리턴 값 저장 
                    var jsonResult = response.Content.ReadAsStringAsync().Result;

                    // JSON 파싱
                    person.faceId = JsonConvert.DeserializeObject<Person>(jsonResult).faceId;
                    Debug.WriteLine(person.faceId);
                }
            }            
        }
        
    }
}
