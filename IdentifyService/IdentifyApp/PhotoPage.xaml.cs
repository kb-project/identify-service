using IdentifyApp.Helpers;
using IdentifyApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace IdentifyApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PhotoPage : Page
    {
        //개체 초기화
        private StorageFolder _captureFolder = null;

        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;
        private string imagePath;
        private string frontFaceImageName = "frontFace.jpg";
        private string RightFaceImageName = "RightFace.jpg";
        private string LeftFaceImageName = "LeftFace.jpg";
        private string imageFileName;
        Person person = new Person();

        private int stepNum = 1;
        public PhotoPage()
        {

            this.InitializeComponent();
            createPersonId();
            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Suspending += Application_Resuming;

            person = (Person)e.Parameter;
            createPersonId();

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
            if (_mediaCapture == null)
            {
                var cameraDevice = await CameraHelper.FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);

                if (cameraDevice == null)
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
            // 단계에 따라 저장되는 사진 이름이 다름            
            if(stepNum == 1)
            {
                imageFileName = frontFaceImageName;
            }
            else if(stepNum == 2)
            {
                imageFileName = RightFaceImageName;
            }
            else
            {
                imageFileName = LeftFaceImageName;
            }

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
                    ImageControl.Source = bitmapImage;

                }
                Debug.WriteLine("사진을 미리봅니다");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("사진을 찍는동안 문제가 발생하였습니다: " + ex.ToString());
            }
        }

        // PersonId 생성
        private void createPersonId()
        {
            string requestUrl = App.baseUrl + "/persongroups/" + person.personGroupId + "/persons";
            using (var client = new HttpClient())
            {
                var response = client.PostAsync(requestUrl, null).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Http 요청에 실패했습니다.");
                }

                response.EnsureSuccessStatusCode();
                client.Dispose();

                // JSON 리턴 값 저장 
                var jsonResult = response.Content.ReadAsStringAsync().Result;

                // JSON 파싱
                person.personId = JsonConvert.DeserializeObject<Person>(jsonResult).personId;
                Debug.WriteLine(person.personId);

            }
        }

        private void sendPhoto()
        {
            string requestUrl = App.baseUrl + "/photo/" + person.personGroupId + "/" + person.personId;

            using (var fileStream = new FileStream(imagePath, FileMode.Open))
            {
                HttpContent fileStreamContent = new StreamContent(fileStream);
                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {   
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
                    //person.faceId = JsonConvert.DeserializeObject<Person>(jsonResult).faceId;
                    //Debug.WriteLine(person.faceId);
                }
            }
        }

    

        private void takePhotoBtnClicked(object sender, RoutedEventArgs e)
        {
            TakePhotoAsync();
        }

        private async void confirmBtnClicked(object sender, RoutedEventArgs e)
        {
            ConfirmBtn.Visibility = Visibility.Collapsed;

            await Task.Run(() =>
            {
                sendPhoto();
            });

            if(stepNum != 3)
            {
                stepNum++;
                statusChange();
            }
            else
            {
                //사진 3장 찍기 완료 후 다음 단계로 넘어가기 위한 버튼 활성화
                ConfidenceArea.Visibility = Visibility.Visible;
                NextPageBtn.Visibility = Visibility.Visible;
            }
            
        }

        private void statusChange()
        {
            if(stepNum == 2)
            {
                GuideMessage.Text = "Step2. 오른쪽으로 45 각도로 바라보세요";
                imageFileName = RightFaceImageName;
            }
            else if(stepNum == 3)
            {
                GuideMessage.Text = "Step3. 왼쪽으로 45 각도로 바라보세요";
                imageFileName = LeftFaceImageName;
            }
        }
        private async void NextBtnClicked(object sender, RoutedEventArgs e)
        {
            double inputValue;
            bool isDouble = Double.TryParse(ConfidenceValue.Text, out inputValue);
            if (isDouble)
            {
                if (inputValue > 0 && inputValue < 1)
                {
                    await CleanupCameraAsync();
                    Frame.Navigate(typeof(VerifyPage), person);
                }

                else
                {
                    MessageDialog showDialog = new MessageDialog("0에서 1사이의 값을 입력하세요!");
                    showDialog.Commands.Add(new UICommand("확인"));
                    await showDialog.ShowAsync();
                    ConfidenceValue.Text = String.Empty;
                }
            }
            else
            {
                MessageDialog showDialog = new MessageDialog("0에서 1사이의 값을 입력하세요!");
                showDialog.Commands.Add(new UICommand("확인"));
                await showDialog.ShowAsync();
                ConfidenceValue.Text = String.Empty;
            }
            
        }

        
    }
}
