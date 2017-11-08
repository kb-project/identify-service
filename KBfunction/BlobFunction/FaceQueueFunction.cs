using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Newtonsoft.Json;
using RestSharp;

namespace KBfunction
{
    public static class FaceQueueFunction
    {
        [FunctionName("FaceQueueFunction")]
        public static void Run([QueueTrigger("photo", Connection = "kbConnection")]string myQueueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");


            //myQueueItem Sample
            /*
            Format
            {
                "personGroupId": "persongroup1",
                "personId": "7573189b-eceb-4ff7-a95c-479d9cc34381",
                "blobUrl": "https://kbdwrstorage.blob.core.windows.net/sample/%EC%A0%84%EC%A7%80%ED%98%841.jpg"
            }
            */

            // JsonText to a Class
            ImageToTrain imgToTrain = JsonConvert.DeserializeObject<ImageToTrain>(myQueueItem);


            // key setting
            String myKeyFaceAPI = "d34788525010436ba92a2fdea1463ec4";
            String baseURL = "";
            String baseURL2 = "";
            String baseURL3 = "";

            // Add a person Face
            //testURL = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/persongroup1/persons/fd85771d-cff6-4d36-92f3-f190ebfa0f41";
            baseURL = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/";
            baseURL2 = "/persons/";
            baseURL3 = "/persistedFaces";
            String reqURLaddFace = baseURL + imgToTrain.personGroupId + baseURL2 + imgToTrain.personId + baseURL3;
            String imageURL = "{\"url\":\"" + imgToTrain.blobUrl + "\"\r\n}";
            
            var client = new RestClient(reqURLaddFace);
            var request = new RestRequest(Method.POST);
            request.AddHeader("ocp-apim-subscription-key", myKeyFaceAPI);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", imageURL, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            log.Info("얼굴이미지 인식 결과: "+response.Content.ToString());

            // Train the PersonGroup
            //testURL = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/persongroup1/train";
            baseURL = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/";
            baseURL2 = "/train";
            String reqURLtrainPersonGroup = baseURL + imgToTrain.personGroupId + baseURL2;

            client = new RestClient(reqURLtrainPersonGroup);
            request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("ocp-apim-subscription-key", myKeyFaceAPI);
            response = client.Execute(request);

            log.Info("얼굴이미지 학습 결과: " + response.Content.ToString());
        }
    }
    public class ImageToTrain
    {
        public string personGroupId { get; set; }
        public string personId { get; set; }
        public string blobUrl { get; set; }
    }
}
