using IdentifyWeb.Models;
using Microsoft.Azure;
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
    public class FaceVerifyHelper
    {
        
        public static async Task<IRestResponse> FaceVerifyPostAsync(string key, string faceId, string personId, string personGroupId)
        {

            try
            {
                List<string> contents = new List<string>();

                var client = new RestClient("https://eastasia.api.cognitive.microsoft.com/face/v1.0/verify");
                var request = new RestRequest(Method.POST);

                request.AddHeader("ocp-apim-subscription-key", key);
                request.AddHeader("content-type", "application/json");

        
                Trace.WriteLine(JsonConvert.SerializeObject(new FaceVerifyRequestBody(faceId, personId, personGroupId)));
                request.AddParameter("application/json", JsonConvert.SerializeObject(new FaceVerifyRequestBody(faceId, personId, personGroupId)), ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                Trace.WriteLine(response.Content);

 
    

                return response;

            }
            catch (System.Exception e)
            {
                Trace.WriteLine($"Exception: {e.Data}");
                throw e;
            }
        }
    }
}
