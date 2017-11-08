using IdentifyWeb.Models;
using IdentifyWeb.Utility;
using Microsoft.Azure;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace IdentifyWeb.Controllers
{
    public class VerifyController : ApiController
    {
        List<UrlBlob> listUrlBlob;
        string blobPrefixString;

        //PostUploadPhotoAndAddQueue
        [Route("api/verify/{personGroupId}/{personId}/{faceId}")]
        [HttpPost]
        public async Task<HttpResponseMessage> VerifyPostAsync(string personGroupId, string personId, string faceId)
        {
            InitEnvironment();

            #region 저장한 verify결과를 json body로 반환
            try
            {

                //Verify 호출
                IRestResponse response = await FaceVerifyHelper.FaceVerifyPostAsync(
                    CloudConfigurationManager.GetSetting("CognitiveServicesKeyFace"),
                    faceId, personId, personGroupId);

                if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
                {
                    FaceVerifyResponseBody faceVerifyResponseBody = JsonConvert.DeserializeObject<FaceVerifyResponseBody>(response.Content);

                    return Request.CreateResponse(HttpStatusCode.OK, faceVerifyResponseBody);
                }
                else
                {
                    return Request.CreateResponse(response.StatusCode, response.Content);
                }
               
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception occurred while verifying face.");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
            #endregion
        }

        private void InitEnvironment()
        {
            //do nothing
        }
    }
}
