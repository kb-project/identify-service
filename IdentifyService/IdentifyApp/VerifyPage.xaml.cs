using IdentifyApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace IdentifyApp
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class VerifyPage : Page
    {
        Person person = new Person();
        VerifyResult verify = new VerifyResult();
       
        private const int TotalNumberOfAttempts = 10;
       
        public VerifyPage()
        {
            this.InitializeComponent();        
        }
        

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Parameter로 넘겨 받은 값 저장
            person = (Person)e.Parameter;
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            var taskCompletionSource = new TaskCompletionSource<bool>();
            
            await dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>            
            {
                int maxRetry = 5;
                do
                {
                    --maxRetry;
                    string requestUrl = App.baseUrl + "/persongroups/" + person.personGroupId + "/training";
                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(requestUrl).Result;

                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine("Http 요청에 실패했습니다.");
                        }

                        response.EnsureSuccessStatusCode();
                        
                        // JSON 리턴 값 저장 
                        var jsonResult = response.Content.ReadAsStringAsync().Result;

                        string trainResult = JsonConvert.DeserializeObject<TrainingResult>(jsonResult).status;

                        if (trainResult == "succeeded")
                        {
                            Debug.WriteLine("Training 이 완료되었습니다!");
                            taskCompletionSource.SetResult(true);
                            return;                            
                        }
                        else
                        {
                            Debug.WriteLine("아직 Training이 완료되지 않았습니다!");
                            //2초후 재시도 
                            await Task.Delay(2000);
                        }
                    }
                } while (maxRetry>0);                
            });
            await taskCompletionSource.Task;

            await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                verifyRequest();
            });
            
        }



        private async void verifyRequest()
        {
            int maxRetry = 10;
            
            do
            {
                --maxRetry;
                using (var client = new HttpClient())
                {
                    string requestUrl = App.baseUrl + "/verify/" + person.personGroupId + "/" + person.personId + "/" + person.faceId;
                    Debug.WriteLine(requestUrl);

                    await client.PostAsync(requestUrl, null);
                    var response = client.PostAsync(requestUrl, null).Result;
                    
                    if(response.IsSuccessStatusCode)
                    {
                        client.Dispose();

                        // JSON 리턴 값 저장 
                        var jsonResult = response.Content.ReadAsStringAsync().Result;

                        // JSON 파싱
                        verify = JsonConvert.DeserializeObject<VerifyResult>(jsonResult);

                        // Confidence 결과 값 퍼센트로 변환
                        double confidenceResult = verify.confidence * 100;
                        confidenceTxt.Text = confidenceResult.ToString();

                        // 결과로 받아온 confidence 값(verify.confidence)이 기준치(person.confidence)랑 비교했을때 큰지 작은지 여부 
                        if (person.confidence <= verify.confidence)
                        {
                            Debug.WriteLine(verify.confidence);
                            checkResultTxt.Visibility = Visibility.Collapsed;
                            progress1.Visibility = Visibility.Collapsed;
                            VerifyTxt.Visibility = Visibility.Visible;
                            confidenceArea.Visibility = Visibility.Visible;
                            restartBtn.Visibility = Visibility.Visible;
                            return;
                        }
                        else
                        {
                            Debug.WriteLine(verify.confidence);
                            checkResultTxt.Visibility = Visibility.Collapsed;
                            progress1.Visibility = Visibility.Collapsed;
                            notVerifyTxt.Visibility = Visibility.Visible;
                            confidenceArea.Visibility = Visibility.Visible;
                            restartBtn.Visibility = Visibility.Visible;
                            return;
                        }
                    }                    

                    await Task.Delay(3000);
                    Debug.WriteLine("3초후 재시도!");
                }

            } while (maxRetry > 0); 
            
            if(maxRetry == 0)
            {
                checkResultTxt.Visibility = Visibility.Collapsed;
                progress1.Visibility = Visibility.Collapsed;
                serviceErrorTxt.Visibility = Visibility.Visible;
            }
        }

        private void restartBtnClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }

   
}