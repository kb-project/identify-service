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
using IdentifyWeb.Models;
using IdentifyWeb.Utility;

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
        string blobPrefixString;

        [HttpPost]
        public async Task<HttpResponseMessage> ProcessIdcardPostAsync()
        {
            InitEnvironment();

            #region Step1-Step2. 첨부된 파일을 Web App의 Map Path에 복사하고, 이를 Blob Container에 업로드
            MultipartFormdataStreamBlobUploader multipartFormdataStreamBlobUploader = new MultipartFormdataStreamBlobUploader(provider, storageAccount, container);
            listUrlBlob = await multipartFormdataStreamBlobUploader.UploadAttachedFileToBlobContainer(this.Request, blobPrefixString);
            #endregion

            #region Step3. 저장한 blob 위치를 인지서비스에 전달하여 OCR 및 Face 정보 추출
            try
            {
                foreach (UrlBlob urlBlob in listUrlBlob)
                {
                    //OCR 호출
                    List<string> contentsOcr = await CognitiveServicesCallHelper.CognitiveServicePostAsync(
                        CloudConfigurationManager.GetSetting("CognitiveServicesKeyVision"),
                        "https://eastasia.api.cognitive.microsoft.com/vision/v1.0/ocr?language=ko&detectOrientation=true",
                        urlBlob.url);

                    //OCR 결과를 건별로 Queue에 넣음, trace 표시
                    foreach (string content in contentsOcr)
                    {
                        CloudQueueMessage message = new CloudQueueMessage(content);
                        queue.AddMessage(message);

                        Trace.WriteLine("OCR: " + content);
                    }

                    //Face Detection 호출
                    List<string> contentsFace = await CognitiveServicesCallHelper.CognitiveServicePostAsync(
                        CloudConfigurationManager.GetSetting("CognitiveServicesKeyVision"),
                        "https://eastasia.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceAttributes=age,gender,headPose,glasses,accessories",
                        urlBlob.url);

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

            blobPrefixString = CloudConfigurationManager.GetSetting("TempBlobRelativeLocationIdcard");

        }


    }
    
}
