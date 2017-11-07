﻿using RestSharp;
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
        CloudStorageAccount storageAccount;
        CloudQueueClient queueClient;
        CloudQueue queue;
        CloudBlobClient blobClient;
        CloudBlobContainer container;
        MultipartFormDataStreamProvider provider;

        List<UrlBlob> listUrlBlob;

        public async Task<HttpResponseMessage> PostFormData()
        {
            InitEnvironment();

            #region Step1-Step2. 첨부된 파일을 Web App의 Map Path에 복사하고, 이를 Blob Container에 업로드
            MultipartFormdataStreamBlobUploader multipartFormdataStreamBlobUploader = new MultipartFormdataStreamBlobUploader(provider, storageAccount, container);            
            listUrlBlob = await multipartFormdataStreamBlobUploader.UploadAttachedFileToBlobContainer(this.Request);
            #endregion

            #region Step3. 저장한 blob 위치를 인지서비스에 전달하여 OCR 및 Face 정보 추출
            try
            {
                foreach (UrlBlob urlBlob in listUrlBlob)
                {
                    //OCR 호출
                    List<string> contentsOcr = await CognitiveServiceCallAsync(urlBlob.url,
                        "https://eastasia.api.cognitive.microsoft.com/vision/v1.0/ocr?language=ko&detectOrientation=true",
                         CloudConfigurationManager.GetSetting("CognitiveServicesKeyVision"));

                    //OCR 결과를 건별로 Queue에 넣음, trace 표시
                    foreach (string content in contentsOcr)
                    {
                        CloudQueueMessage message = new CloudQueueMessage(content);
                        queue.AddMessage(message);

                        Trace.WriteLine("OCR: " + content);
                    }

                    //Face Detection 호출
                    List<string> contentsFace = await CognitiveServiceCallAsync(urlBlob.url,
                        "https://eastasia.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceAttributes=age,gender,headPose,glasses,accessories",
                        CloudConfigurationManager.GetSetting("CognitiveServicesKeyFace"));

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
                // return empty FaceId if no faces were found.
                return Request.CreateResponse(HttpStatusCode.OK, new JsonFaceId(""));
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
            #endregion

        }

        private void InitEnvironment()
        {
            storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("OcrQueueName"));
            queue.CreateIfNotExists();

            blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(CloudConfigurationManager.GetSetting("TempBlobContainerNameIdcard"));

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = HttpContext.Current.Server.MapPath("~/App_Data");
            provider = new MultipartFormDataStreamProvider(root);

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
