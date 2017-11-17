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
using IdentifyWeb.Models;

namespace IdentifyWeb.Controllers
{
    public class PersonGroupsController : ApiController
    {
        
        [Route("api/persongroups/{personGroupId}/persons")]
        [HttpPost]
        public async Task<HttpResponseMessage> CreatePersonPostAsync(string personGroupId)
        {
            var client = new RestClient($"https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/{personGroupId}/persons");
            var request = new RestRequest(Method.POST);
            request.AddHeader("ocp-apim-subscription-key", CloudConfigurationManager.GetSetting("CognitiveServicesKeyFace"));
            request.AddHeader("content-type", "application/json");

            //GUID 생성 코드 추가
            Guid guid = Guid.NewGuid();

            request.AddParameter("application/json", "{\"name\":\"" + guid + "\",\"userData\":\"User-provided data attached to the person\"}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            CreatePersonResult createPersonResult = JsonConvert.DeserializeObject<CreatePersonResult>(response.Content);

            return Request.CreateResponse(HttpStatusCode.OK, createPersonResult);
        }


        [Route("api/persongroups/{personGroupId}/training")]
        [HttpGet]
        public async Task<HttpResponseMessage> PersonGroupTrainingStatusGetAsync(string personGroupId)
        {
            var client = new RestClient("https://eastasia.api.cognitive.microsoft.com/face/v1.0/persongroups/"+personGroupId+"/training");
            var request = new RestRequest(Method.GET);
            request.AddHeader("ocp-apim-subscription-key", CloudConfigurationManager.GetSetting("CognitiveServicesKeyFace"));
            //request.AddHeader("content-type", "application/json");

            IRestResponse response = client.Execute(request);

            if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
            {
                PersonGroupTrainingStatus personGroupTrainingStatus = JsonConvert.DeserializeObject<PersonGroupTrainingStatus>(response.Content);

                return Request.CreateResponse(HttpStatusCode.OK, personGroupTrainingStatus);
            }
            else
            {
                return Request.CreateResponse(response.StatusCode, response.Content);
            }
        }

    }
    
}
