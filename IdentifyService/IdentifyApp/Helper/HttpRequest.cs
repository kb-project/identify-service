using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace IdentifyApp.Helper
{
    class HttpRequest
    {        
        public async static Task httpPost(string url)
        {
            using(var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, new StringContent(""));

                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                await Task.Run(() => JsonObject.Parse(content));
            }
            
        }

        public async static Task httpGet(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                await Task.Run(() => JsonObject.Parse(content));
            }
            
        }
    }
}
