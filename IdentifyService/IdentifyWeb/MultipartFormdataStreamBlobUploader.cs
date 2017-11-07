using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace IdentifyWeb
{
    public class MultipartFormdataStreamBlobUploader
    {
        MultipartFormDataStreamProvider _provider { get; set; }
        CloudStorageAccount _storageAccount { get; set; }
        CloudBlobContainer _container { get; set; }



        public MultipartFormdataStreamBlobUploader(MultipartFormDataStreamProvider provider, CloudStorageAccount storageAccount, CloudBlobContainer container)
        {
            _provider = provider;
            _storageAccount = storageAccount;
            _container = container;
        }


        public async Task<List<UrlBlob>> UploadAttachedFileToBlobContainer(HttpRequestMessage Request)
        {
            //Step1. Copy Attached File to App_Data folder (in web apps)
            await CopyAttachedFileToMapPathAsync(Request);

            //Step2. Upload image to temporary storage account
            return UploadMapPathFilesToTempBlobContainer();
        }

        internal async Task<string> CopyAttachedFileToMapPathAsync(HttpRequestMessage Request)
        {
            //// 사진 파일을 App_Data 폴더 밑에 임시로 저장
            try
            {
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(_provider);
                return "OK";
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception occurred while reading Request Content.");
                throw e;
            }
            
        }


        private List<UrlBlob> UploadMapPathFilesToTempBlobContainer()
        {
            List<UrlBlob> listUrlBlob = new List<UrlBlob>();

            try
            {
                // This illustrates how to get the file names.
                foreach (MultipartFileData file in _provider.FileData)
                {
                    Trace.WriteLine(file.Headers.ContentDisposition.FileName);
                    Trace.WriteLine("Server file path: " + file.LocalFileName);

                    //set blob name: only use filename excluding folder location
                    string blobname = $"{CloudConfigurationManager.GetSetting("TempBlobRelativeLocationIdcard")}/{file.LocalFileName.Substring(file.LocalFileName.LastIndexOf("\\") + 1)}";
                    CloudBlockBlob blob = _container.GetBlockBlobReference(blobname);
                    blob.UploadFromFile(file.LocalFileName);

                    blob.FetchAttributes();
                    bool success = blob.Properties.Length == new System.IO.FileInfo(file.LocalFileName).Length;
                    if (!success)
                    {
                        blob.Delete();
                        throw new StorageException();
                    }

                    listUrlBlob.Add(new UrlBlob(blob.Uri.AbsoluteUri));
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception occurred while uploading file.");
                throw e;
            }

            return listUrlBlob;
        }

    }

    public class UrlBlob
    {
        public string url { get; }

        public UrlBlob()
        {
            url = "";
        }
        public UrlBlob(string urlString)
        {
            url = urlString;
        }
    }
}