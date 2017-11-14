using IdentifyApp.Model;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace IdentifyApp
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class VerifyPage : Page
    {
        public string faceId = "";
        public string personId = "";
        public VerifyPage()
        {            
            this.InitializeComponent();
            verify();
        }

        private void verify()
        {
            if (httpGet())
            {
                if (httpPost())
                {
                    //학습이 완료되었구, 동일인이라고 판정되면 
                    verifyTxt.Visibility = Visibility.Visible;
                    
                }

                //학습이 완료되었지만, 동일인이 아니라고 판정되면
                notVerifyTxt.Visibility = Visibility.Visible;
            }

        }

        private bool httpGet()
        {
            using (var client = new HttpClient())
            {               
                string url = "http://kbdwr-web.azurewebsites.net/api/persongroups/persongroup1/training";
                var response = client.GetAsync(url).Result;

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                return true;
            }
        }

        private bool httpPost()
        {
            using(var client = new HttpClient())
            {
                
                string url = "http://kbdwr-web.azurewebsites.net/api/verify/persongroup1/"+personId+"/"+faceId;
                var response = client.PostAsync(url, new StringContent("")).Result;

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                return true;

            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PersonInfo myPerson = e.Parameter as PersonInfo;
            faceId = myPerson.faceId;
            personId = myPerson.personId;
        }
    }
}
