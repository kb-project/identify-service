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

        List<string> listUrlBlobString;


        public async Task<HttpResponseMessage> PostUploadPhotoAndAddQueue(string personGroupId, string personId)
        {
            throw new NotImplementedException();
        }




    }
}
