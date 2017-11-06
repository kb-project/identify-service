using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types

namespace IdentifyWeb.Controllers
{
    public class UploadController : ApiController
    {
        public async Task<HttpResponseMessage> PostFormData()
        {
            // Azure Queue Storage - 셋업 관련 코드
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("ocr");
            queue.CreateIfNotExists();

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);


            //사진 파일을 App_Data 폴더 밑에 임시로 저장
            try
            {
                List<string> contents = await CognitiveServiceCall(provider, "https://eastasia.api.cognitive.microsoft.com/vision/v1.0/ocr?language=ko&detectOrientation=true");

                foreach (string content in contents)
                {
                    CloudQueueMessage message = new CloudQueueMessage(content);
                    queue.AddMessage(message);

                }
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
            
            

        }

        public async Task<List<string>> CognitiveServiceCall(MultipartFormDataStreamProvider provider, string url)
        {

            try
            {
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                List<string> contents = new List<string>();

                // This illustrates how to get the file names.
                foreach (MultipartFileData file in provider.FileData)
                {
                    Trace.WriteLine(file.Headers.ContentDisposition.FileName);
                    Trace.WriteLine("Server file path: " + file.LocalFileName);

                    var client = new RestClient(url);
                    var request = new RestRequest(Method.POST);

                    //사진 파일에 대해 OCR 분석 관련 HTTP POST 요청 
                    request.AddHeader("content-type", "application/octect-stream");
                    request.AddHeader("ocp-apim-subscription-key", "601a9f2e62d043ca807f55060769b550");
                    request.AddFile("test.png", file.LocalFileName);
                    IRestResponse response = client.Execute(request);
                    Trace.WriteLine(response.Content);


                    contents.Add(response.Content);
                    
                }
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
