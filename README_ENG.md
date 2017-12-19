---
layout: post
title:  "Customer CaaP Hackfest"
author: "Eunji Kim"
author-link: "#"
#author-image: "{{ site.baseurl }}/images/authors/photo.jpg"
date:   2017-11-24
categories: CaaP
color: "blue"
#image: "{{ site.baseurl }}/images/imagename.png" #should be ~350px tall
excerpt: This article is aimed a providing a template to create DevOps Hackfest articles.
---

KB is one of the largest bank in Korea. Banking industry is conservative to adopt new technologies  such as cloud services. They are considering the adoption of cloud, starting with Microsoft's AI technology. In this project, we tried to use Microsoft's AI services to examine non-facing authentication scenarios. 

## Key Technologies ## 

- [Microsoft Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/) (Face, Vision)
- [Azure Functions](https://azure.microsoft.com/en-us/services/functions/)
- [Azure API Apps](https://azure.microsoft.com/en-us/services/app-service/api/)
- [Azure Storage](https://azure.microsoft.com/en-us/services/storage/?v=16.50) (Blob, Queue)
- [Universal Windows Platform](https://docs.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide) (Windows 10 App) 

## Core Project Team ##
![hackfest2](./images/hackfest2.PNG)
* Microsoft
	* [Eunji Kim](https://github.com/angie4u) (Software Engineer, Microsoft)
	* Hyewon Ryu (Program Manager, Microsoft)
	* Seok jin Han (GBB AA/AI TSP, Microsoft)
	* Seungmin Cho (STU TSP, Microsoft)
* KB Bank
	* Sung ho Hong (Senior Manager, KB Smart platform Team) 
	* Mi sun Yoo (Assistant Manager, KB Smart platform Team) 
	* Sung hak Kim (Assistant Manager, KB Smart platform Team) 
 
## Customer Profile ##
![kblogo](./images/kblogo.jpg)

As a core affiliate of KB Financial Group Inc.(KB FG), KB Kookmin Bank is one of the four largest banks ranked by asset value in South Korea since its establishment in 1963. KB FG is a comprehensive financial group that has assets of 299 trillion KRW and also has the largest domestic customer base  based on the widest network, and many branches in Korea. Not only does KB FG have domestic affiliates but also owns worldwide affiliates : KB Asset Management, KB Real Estate Trust, KB Investment, KB Futures, KB Credit Information, KB Date Systems, Kookmin Bank Hong Kong Ltd. (UK), Kookmin Bank International Ltd.(UK), and KB Investment & Securities Hong Kong Ltd.(HK)

## Problem Statement ##

Online banking service was finot the new area and it already has been serviced to many customers at bank branches. In 2017, the first “Internet Bank” was introduced with no on-site bank branches but only services through online. It was spotlighted by many people due to its convenience. High attentions has brought its rapid growth of value of internet banks but at the same time proportionally it became more struggle to build more competitive services. 

One of their ideas was a non face-to-face service that allowed customers could do their businesses without going to a bank. The most important issue for this service was User Authentication. KB Bank hoped to build the service that could verify a user by comparing the photo on  user’s ID card by our AI technology.

![hackfest](./images/hackfest.PNG)

## Solutions, Steps, and Delivery ##

Based on the [Uber customer story](https://customers.microsoft.com/en-US/story/uber), we derived a scenario that uses [Microsoft Cognitive Services - Face API](https://azure.microsoft.com/en-us/services/cognitive-services/face/) to authenticate the user. We created a client app(Windows 10 Application) for users to take and send photos. All processing, including request to Cognitive Services and authentication were handled on the server side. The architecture and detailed implementation of this project are as follows.

### Architecture

![architecture](./images/architecture-eng.PNG)
This service was consists of three parts: Client app(UWP), Azure API apps and Azure functions.
* Client app : Takes pictures and sends it to a server to process
* Azure API Apps : Calls a cognitive services when it receives a request from an application and stores the information to Azure Storage
* Azure functions : Triggers and executes when an event occurs to the queue. Process OCR data, add a photo of that person and train person group.  


### Tech Demo Video 

[![techdemo](./images/techdemo.PNG)](https://1drv.ms/v/s!AsVbhtDr37iriA2GX8HCMukdqXMb)

### Technical details of how this was implemented

**STEP1. Get ID card photo** 

With the client app, users can take an ID card photo and send it to the server that processes the photo. 
The Server saves the ID card photo to temporary storage and processes them using FACE API and OCR API. Users can get the FACE ID from FACE API to identify the user. Users can also use the OCR API to retrieve information from an image and store it to Azure Queue Storage.

[IdentifyWeb - Controllers/IdcardController.cs]	
```
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
			CloudConfigurationManager.GetSetting("CognitiveServicesKeyFace"),
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
```

**STEP 2. GET Photo from user**

Using the application, users can take photo several times and send them to a server that processes photos. 
This step creates personal ID. It can be used for save several photos and verify user.
To implement this,  the server creates a Personal ID and stores photos in the Blob storage and sends the information to Azure Queue Storage. It will be processed by Azure Functions.

[IdnetifyWeb - Controllers/PersonGroupsController]
```
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
```

[IdcardWeb - Controllers/PhotoController.cs]
```
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
catch(Exception e)	{
	Trace.WriteLine("Exception occurred while sending message to queue.");
	return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
}

return message;

#endregion
```

**STEP3. Add photos to PersonalID and train PersonalID** 

The server sends the Personal ID and image URL information to Azure Queue Storage Azure function is triggered by the input and processes the image with the Personal ID. 

[kbdwrfunctions - ProcessPhotoQueue.cs]
```
[FunctionName("ProcessPhotoQueue")]
public static void Run([QueueTrigger("photo", Connection = "StorageConnection")]string myQueueItem, TraceWriter log)
{
	log.Info($"C# Queue trigger function processed: {myQueueItem}");
	var jsonData = myQueueItem;

	Photo photo = new Photo();
	photo = JsonConvert.DeserializeObject<Photo>(jsonData);

	// Add Face 
	var photoResult = AddPhoto(photo);
	log.Info(photoResult);

	// Training PersonGroup
	var trainResult = TrainPersonGroup(photo);
	log.Info(trainResult);
}

private static string AddPhoto(Photo photo)
{
	string baseUrl = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/";
	string addPhotoUrl = baseUrl + "persongroups/" + photo.personGroupId + "/persons/" + photo.personId + "/persistedFaces";

	using (var client = new HttpClient())
	{
		client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<FACE API KEY>");

		string httpBody = "{\"url\":\"" +
			photo.blobUrl + "\"}";
		var httpContent = new StringContent(httpBody, Encoding.UTF8, "application/json");

		var response = client.PostAsync(addPhotoUrl, httpContent).Result;

		if (!response.IsSuccessStatusCode)
		{
			return "Http Request was successful.";
		}

		response.EnsureSuccessStatusCode();
		client.Dispose();

		return response.Content.ReadAsStringAsync().Result;
	}
}

	private static string TrainPersonGroup(Photo photo)
{
	string baseUrl = "https://eastasia.api.cognitive.microsoft.com/face/v1.0/";
	string trainGroupUrl = baseUrl + "persongroups/" + photo.personGroupId + "/train";

	using (var client = new HttpClient())
	{
		client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<FACE API KEY>");

		var response = client.PostAsync(trainGroupUrl, null).Result;

		if (!response.IsSuccessStatusCode)
		{
			return "Http Request was successful.";
		}

		response.EnsureSuccessStatusCode();
		client.Dispose();

		return response.Content.ReadAsStringAsync().Result;
	}
}
```

**STEP4. Verify person with ID card and photo**

Use the FACE API and the Person ID to identify the user.

[IdentifyWeb - Controllers/VerifyController.cs]
```
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
```
### Application Screenshots

![scenario](./images/scenario.PNG)
* Take an ID photo and then take several pictures of yourself to verify.
* In this case, recognizing that two persons are the same person with a probability of 66.167%.
* If the camera quality is poor, or the ID photo is old, the recognition rate could be low.
* There is a possibility that it can be used as an additional authentication method.
 
## General lessons ##
* There were many legal restrictions on the use of Public Cloud in the Korean finance industry recently. If it has been included personal information, it will be impossible to handle the data in the Public Cloud. The problem also became a big obstacle in this case. In the case of a project related to personal information from financial industry, it is necessary to discuss deeply about the legal issues.
* Tried to refine the OCR results through the post-processing but we were only able to put the raw OCR results into the table storage due to time constraint.
* Had problem that anyone could request service to server as a unauthorized user could call and use the API. To solve this problem, it was necessary to implement AD or Azure AD to the service. 

## Conclusion ##

With the power of Microsoft AI Services, we can figure out the possibility of face authentication services. Traditionally, banks are one of the most conservative business areas to adopt new technology such as cloud services. During the Hackfest, customers were impressed by the ease of use cloud services such as rapid deployment of servers, or they can easily implement and set up their development environment. The Microsoft team was able to fine-tune the customer scenarios for the bank and it was a great opportunity to collaborate with other roles, such as GBB or TSP. We are in discussions with KB Bank to apply this to real business scenario and also we hope this scenario to be the first step towards introducing Azure in the banking industry.

## Resources ##
* Explore [identify-service github repo](https://github.com/kb-project/identify-service)
* [FACE API testing console](https://westus.dev.cognitive.microsoft.com/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f30395236)
* [ASP.NET Web API: File Upload](https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/sending-html-form-data-part-2)
* [Customer Case Story - Uber](https://customers.microsoft.com/en-US/story/uber)

