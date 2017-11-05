using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace IdentifyApp
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class VideoCapturePage : Page
    {
        // For listening to media property changes
        private readonly SystemMediaTransportControls _systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
        
        MediaCapture mediaCapture;
        MediaCapture _mediaCapture;
        bool isPreviewing;
        LowLagMediaRecording _mediaRecording;
        bool isVideoStarted = false;
        MediaFrameReader mediaFrameReader;
        private bool _isInitialized = false;
        private StorageFolder _captureFolder = null;


        // Information about the camera device
        private bool _mirroringPreview = false;
        private bool _externalCamera = false;


        public VideoCapturePage()
        {
            this.InitializeComponent();
        }
      
        private void takeVideoBtnClicked(object sender, RoutedEventArgs e)
        {
            TakeVideoAsync();
           
        }

        private async void TakeVideoAsync()
        {
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            if (isVideoStarted == false)
            {

                var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
                StorageFile file = await myVideos.SaveFolder.CreateFileAsync("video.mp4", CreationCollisionOption.GenerateUniqueName);
                _mediaRecording = await mediaCapture.PrepareLowLagRecordToStorageFileAsync(
                        MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);

                await _mediaRecording.StartAsync();
                isVideoStarted = true;
            }
            else
            {
                await _mediaRecording.StopAsync();
                await _mediaRecording.FinishAsync();


            }

        }

      
        private void stopVideoBtnClicked(object sender, RoutedEventArgs e)
        {

        }

        public async Task<IInputStream> GetThumbnailAsync(StorageFile file)
        {
            var mediaClip = await MediaClip.CreateFromFileAsync(file);
            var mediaComposition = new MediaComposition();
            mediaComposition.Clips.Add(mediaClip);
            


            return await mediaComposition.GetThumbnailAsync(
                TimeSpan.FromMilliseconds(5000), 0, 0, VideoFramePrecision.NearestFrame);
        }

        private async void extractImageBtnClicked(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            foreach (string extension in FileExtensions.Video)
            {
                openPicker.FileTypeFilter.Add(extension);
            }
            StorageFile file = await openPicker.PickSingleFileAsync();

            //var composition = new MediaComposition();
            //TimeSpan interval = new TimeSpan(0,0,1); //1초
            //composition.Clips.Add(await MediaClip.CreateFromImageFileAsync(file, interval));
            //await composition.RenderToFileAsync(file);

            var thumbnail = await GetThumbnailAsync(file);
            BitmapImage bitmapImage = new BitmapImage();
            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
            await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
            randomAccessStream.Seek(0);
            
            bitmapImage.SetSource(randomAccessStream);
            imageControl1.Source = bitmapImage;

        }
        internal class FileExtensions
        {
            public static readonly string[] Video = new string[] { ".mp4", ".wmv" };
        }
        private void confirmBtnClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(VideoCapturePage));
        }

        private void retakeBtnClicked(object sender, RoutedEventArgs e)
        {
            TakeVideoAsync();
        }

        private async void selectBtnClicked(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
