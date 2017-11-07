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
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types

using Microsoft.ProjectOxford.Common;
using Newtonsoft;
using Newtonsoft.Json;

namespace IdentifyWeb.Controllers
{

    
    public class IdcardController : ApiController
    {
        public async Task<HttpResponseMessage> PostFormData()
        {
            // Azure Queue Storage - 셋업 관련 코드
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("ocr");
            queue.CreateIfNotExists();

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("temp-idcard");
            


            // Now we're not going to use multipart/form-data. 
            // Instead, upload image to temporary storage accound, and relay the url to Cognitive Services.


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
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception occurred while reading Request Content.");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }

            List<string> listUrlBlobString = new List<string>();


            //Upload image to temporary storage account
            try
            {
                
                // This illustrates how to get the file names.
                foreach (MultipartFileData file in provider.FileData)
                {
                    Trace.WriteLine(file.Headers.ContentDisposition.FileName);
                    Trace.WriteLine("Server file path: " + file.LocalFileName);

                    //set blob name: only use filename excluding folder location
                    string blobname = $"testblob/{file.LocalFileName.Substring(file.LocalFileName.LastIndexOf("\\") + 1)}";
                    CloudBlockBlob blob = container.GetBlockBlobReference(blobname);
                    blob.UploadFromFile(file.LocalFileName);

                    blob.FetchAttributes();
                    bool success = blob.Properties.Length == new System.IO.FileInfo(file.LocalFileName).Length;
                    if (!success)
                    {
                        blob.Delete();
                        throw new StorageException();

                    }

                    listUrlBlobString.Add(blob.Uri.AbsoluteUri);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception occurred while uploading file.");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);

            }



            // 저장한 blob 위치를 인지서비스에 전달하여 OCR 및 Face 정보 추출
            try
            {

                foreach (string urlBlob in listUrlBlobString)
                {
                    //OCR 호출
                    List<string> contentsOcr = await CognitiveServiceCallAsync(urlBlob,
                        "https://eastasia.api.cognitive.microsoft.com/vision/v1.0/ocr?language=ko&detectOrientation=true",
                        "601a9f2e62d043ca807f55060769b550");

                    //OCR 결과를 건별로 Queue에 넣음, trace 표시
                    foreach (string content in contentsOcr)
                    {
                        CloudQueueMessage message = new CloudQueueMessage(content);
                        queue.AddMessage(message);

                        Trace.WriteLine("OCR: " + content);
                    }

                    //Face Detection 호출
                    List<string> contentsFace = await CognitiveServiceCallAsync(urlBlob,
                        "https://eastasia.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceAttributes=age,gender,headPose,glasses,accessories",
                        "d34788525010436ba92a2fdea1463ec4");

                    //Face 결과를 trace 표시
                    foreach (string content in contentsFace)
                    {
                        
                        List<FaceDetectResult> faceDetectResults = JsonConvert.DeserializeObject<List<FaceDetectResult>>(content);

                        if (faceDetectResults.Count > 0)
                        {
                            Trace.WriteLine("Face: " + content);
                            Trace.WriteLine("FaceId: " + faceDetectResults[0].faceId);


                            HttpResponseMessage message = Request.CreateResponse(HttpStatusCode.OK, new JsonFaceId(faceDetectResults[0].faceId));

                            return message;
                        }
                    }

                }

                return Request.CreateResponse(HttpStatusCode.OK, new JsonFaceId(""));
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
            
        }


        public async Task<List<string>> CognitiveServiceCallAsync(string urlBlob, string urlServices, string key)
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


    public class JsonUrlBlob
    {
        public string url { get; set; }

        public JsonUrlBlob(string urlstring)
        {
            url = urlstring;
        }

        
    }


    public class JsonFaceId
    {
        public string faceId { get; set; }

        public JsonFaceId(string faceIdString)
        {
            faceId = faceIdString;
        }
    }


    public class FaceDetectResult
    {
        public string faceId { get; set; }
        public object faceRectangle { get; set; }
        public object faceLandmarks { get; set; }
        public object faceAttributes { get; set; }
        
    }



}
