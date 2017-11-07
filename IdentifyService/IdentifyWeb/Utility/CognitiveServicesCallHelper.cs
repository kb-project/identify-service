using IdentifyWeb.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace IdentifyWeb.Utility
{
    public class CognitiveServicesCallHelper
    {
        
        public static async Task<List<string>> CognitiveServicePostAsync(string urlBlob, string urlServices, string key)
        {
            try
            {
                List<string> contents = new List<string>();

                Trace.WriteLine("urlBlob: " + urlBlob);
                Trace.WriteLine("urlServices: " + urlServices);

                var client = new RestClient(urlServices);
                var request = new RestRequest(Method.POST);

                //사진 파일에 대해 OCR 분석 관련 HTTP POST 요청 
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("ocp-apim-subscription-key", key);
                //request.AddFile("imagefile1", file.LocalFileName);

                Trace.WriteLine(JsonConvert.SerializeObject(new JsonUrlBlob(urlBlob)));
                request.AddParameter("application/json", JsonConvert.SerializeObject(new JsonUrlBlob(urlBlob)), ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                Trace.WriteLine(response.Content);

                contents.Add(response.Content);

                return contents;
            }
            catch (System.Exception e)
            {
                Trace.WriteLine($"Exception: {e.Data}");
                throw e;
            }
        }
    }
}
