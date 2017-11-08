using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IdentifyWeb.Models
{ 
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





    public class CreatePersonResult
    {
        public string personId { get; set; }
        public CreatePersonResult(string personIdString)
        {
            personId = personIdString;
        }
    }

    public class PersonGroupTrainingStatus
    {
        public string status { get; set; }
        public string createDataTime { get; set; }
        public string lastActionDateTime { get; set; }
        public string message { get; set; }

    }

    public class PhotoUploadedResult
    {
        public string personGroupId { get; set; }
        public string personId { get; set; }
        public string blobUrl { get; set; }
        public PhotoUploadedResult(string personGroupIdString, string personIdString, string blobUrlString)
        {
            personGroupId = personGroupIdString;
            personId = personIdString;
            blobUrl = blobUrlString;
        }
    }

    public class FaceVerifyRequestBody
    {
        public string faceId { get; set; }
        public string personId { get; set; }
        public string personGroupId { get; set; }
        public FaceVerifyRequestBody(string faceIdString, string personIdString, string personGroupIdString)
        { 
            faceId = faceIdString;
            personId = personIdString;
            personGroupId = personGroupIdString;
        }
    }

    public class FaceVerifyResponseBody
    {
        public string isIdentical { get; set; }
        public double confidence { get; set; }
        public FaceVerifyResponseBody(string isIdenticalString, double confidenceDouble)
        {
            isIdentical = isIdenticalString;
            confidence = confidenceDouble;

        }
    }

}