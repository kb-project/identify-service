using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types

using RestSharp;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web;
using System.Diagnostics;
using IdentifyWeb.Models;

namespace IdentifyWeb.Controllers
{
    public class PhotoController : ApiController
    {
        CloudStorageAccount storageAccount;
        CloudQueueClient queueClient;
        CloudQueue queue;
        CloudBlobClient blobClient;
        CloudBlobContainer container;
        MultipartFormDataStreamProvider provider;

        List<UrlBlob> listUrlBlob;
        string blobPrefixString;

        //PostUploadPhotoAndAddQueue
        [Route("api/photo/{personGroupId}/{personId}")]
        [HttpPost]
        public async Task<HttpResponseMessage> ProcessPhotoPostAsync(string personGroupId, string personId)
        {
            InitEnvironment();

            #region Step1-Step2. 첨부된 파일을 Web App의 Map Path에 복사하고, 이를 Blob Container에 업로드
            MultipartFormdataStreamBlobUploader multipartFormdataStreamBlobUploader = new MultipartFormdataStreamBlobUploader(provider, storageAccount, container);
            listUrlBlob = await multipartFormdataStreamBlobUploader.UploadAttachedFileToBlobContainer(this.Request, blobPrefixString);
            #endregion

            // 변수 만들기 PhotoUploadedResult
            // try 안에서 할당
            // 스텝4에서 활용
            PhotoUploadedResult photoUploadedResult;

            HttpResponseMessage message;

            #region Step3. 저장한 blob 위치를 json body로 반환
            try
            {
           
                if (listUrlBlob.Count > 0)
                {
                    photoUploadedResult = new PhotoUploadedResult(personGroupId, personId, listUrlBlob[0].url); 
                    message = Request.CreateResponse(HttpStatusCode.OK, photoUploadedResult);
                }
                else
                {
                    photoUploadedResult = new PhotoUploadedResult(personGroupId, personId,"");
                    message = Request.CreateResponse(HttpStatusCode.OK, photoUploadedResult);
                }
       
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception occurred while reading returningBlobUrl.");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
            #endregion

            #region Step4. Queue에도 전송

            try { 
            
            CloudQueueMessage messageQueue = new CloudQueueMessage(JsonConvert.SerializeObject(photoUploadedResult));
            queue.AddMessage(messageQueue);

            Trace.WriteLine("BlobInQueue: " + JsonConvert.SerializeObject(photoUploadedResult));

            }
            catch(Exception e)
            {
            Trace.WriteLine("Exception occurred while sending message to queue.");
            return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }

            return message;
            
            #endregion
        }


        private void InitEnvironment()
        {
            storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("PhotoQueueName"));
            queue.CreateIfNotExists();

            blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(CloudConfigurationManager.GetSetting("TempBlobContainerNamePhoto"));

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = HttpContext.Current.Server.MapPath("~/App_Data");
            provider = new MultipartFormDataStreamProvider(root);

            blobPrefixString = CloudConfigurationManager.GetSetting("TempBlobRelativeLocationPhoto");
        }
    }
}