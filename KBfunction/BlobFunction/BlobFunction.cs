using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using RestSharp;

namespace BlobFunction
{
    public static class BlobFunction
    {
        [FunctionName("BlobFunction")]
        public static void Run([BlobTrigger("photo/{name}", Connection = "kbConnection")]Stream myBlob, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");


            var client = new RestClient("https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/persongroup1/persons/fd85771d-cff6-4d36-92f3-f190ebfa0f41/persistedFaces");
            var request = new RestRequest(Method.POST);
            request.AddHeader("postman-token", "f506f550-81d0-abd4-db7b-0a95636595f3");
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("ocp-apim-subscription-key", "d34788525010436ba92a2fdea1463ec4");
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", "{\r\n    \"url\":\"https://kbdwrstorage.blob.core.windows.net/sample/ang.jpg\"\r\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);


        }
    }
}
