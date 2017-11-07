using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
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

using Newtonsoft.Json;


namespace IdentifyWeb.Controllers
{
    public class PersonGroupsController : ApiController
    {
        //public async Task<HttpResponseMessage> PostFormData()
        //{
        //    // Azure Queue Storage - 셋업 관련 코드
        //    //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
        //    //    CloudConfigurationManager.GetSetting("StorageConnectionString"));
        //    //CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        //    //CloudQueue queue = queueClient.GetQueueReference("ocr");
        //    //queue.CreateIfNotExists();

        //    // Check if the request contains multipart/form-data.
        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }

        //    string root = HttpContext.Current.Server.MapPath("~/App_Data");
        //    var provider = new MultipartFormDataStreamProvider(root);


        //    //사진 파일을 App_Data 폴더 밑에 임시로 저장
        //    try
        //    {
        //        // Read the form data.
        //        await Request.Content.ReadAsMultipartAsync(provider);

        //        // This illustrates how to get the file names.
        //        foreach (MultipartFileData file in provider.FileData)
        //        {
        //            Trace.WriteLine(file.Headers.ContentDisposition.FileName);
        //            Trace.WriteLine("Server file path: " + file.LocalFileName);

        //            var client = new RestClient("https://eastasia.api.cognitive.microsoft.com/vision/v1.0/ocr?language=ko&detectOrientation=true");
        //            var request = new RestRequest(Method.POST);

        //            //사진 파일에 대해 OCR 분석 관련 HTTP POST 요청 
        //            request.AddHeader("content-type", "application/octect-stream");
        //            request.AddHeader("ocp-apim-subscription-key", "601a9f2e62d043ca807f55060769b550");
        //            request.AddFile("test.png", file.LocalFileName);
        //            IRestResponse response = client.Execute(request);
        //            Trace.WriteLine(response.Content);

        //            //CloudQueueMessage message = new CloudQueueMessage(response.Content);
        //            //queue.AddMessage(message);
        //        }
        //        return Request.CreateResponse(HttpStatusCode.OK);
        //    }
        //    catch (System.Exception e)
        //    {
        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
        //    }


        //}


        [Route("api/persongroups/{personGroupId}/persons")]
        [HttpPost]
        public async Task<HttpResponseMessage> PostCreatePersonAsync(string personGroupId)
        {
            var client = new RestClient($"https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/{personGroupId}/persons");
            var request = new RestRequest(Method.POST);
            request.AddHeader("ocp-apim-subscription-key", "d34788525010436ba92a2fdea1463ec4");
            request.AddHeader("content-type", "application/json");

            //GUID 생성 코드 추가
            Guid guid = Guid.NewGuid();

            request.AddParameter("application/json", "{\"name\":\"" + guid + "\",\"userData\":\"User-provided data attached to the person\"}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);



            CreatePersonResult createPersonResult = JsonConvert.DeserializeObject<CreatePersonResult>(response.Content);

            return Request.CreateResponse(HttpStatusCode.OK, createPersonResult);
        }
    }


    public class CreatePersonResult
    {
        public string personId { get; set; }
        public CreatePersonResult(string personIdString)
        {
            personId = personIdString;
        }
    }
}
