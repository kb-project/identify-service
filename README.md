# Cogntive Service를 활용한 비대면 인증 시나리오 검토

Microsoft Cognitive Service는 마이크로소프트의 인공지능 기술을 개발자들이 쉽게 사용할 수 있도록 서비스화 해놓은 것 입니다. 간단히 API호출 만으로도 언어에 상관없이 편리하게 인지와 관련된 여러가지 서비스를 사용할 수 있습니다. 이번 프로젝트에서는 Cognitive Serivces 중에서도 Vision에 해당하는 서비스인 Computer Vision API와 Face API를 이용하여 신분증 만으로도 비대면 인증이 가능한지를 검토해 볼 예정입니다. 
Cognitive Serivces와 관련된 자세한 정보는 아래에서 참고하실 수 있습니다. 

* [Microsoft Cognitive Services](https://azure.microsoft.com/ko-kr/services/cognitive-services/)
* [Computer Vision API](https://azure.microsoft.com/ko-kr/services/cognitive-services/computer-vision/)
* [Face API](https://azure.microsoft.com/ko-kr/services/cognitive-services/face/)  

## 시나리오 

사용자는 본인인증용 애플리케이션을 실행하고 신분증 사진을 찍습니다. 그 다음 지시에 맞추어 고개를 돌려가며 자신의 모습을 찍습니다. 신분증 사진과 방금 찍은 사용자 사진을 기반으로 Cognitive Services를 이용하여 두 인물이 동일인이 맞는지 확인합니다. 부가적으로 OCR 기술을 활용하여 신분증상의 개인 정보를 인지하고 DB에 저장합니다. 

![001](./images/001.png)
![002](./images/002.PNG)

## Architecture

![003](./images/003.PNG)

클라이언트 앱에서는 사진 촬영 및 전송에 해당하는 기본적인 기능만 수행하고, 나머지 주요한 로직은 전부 마이크로소프의 클라우드 서비스인 Azure에서 수행되게 됩니다. 추후 확장성과 설계의 유연함을 고려하여 Azure Functions이라는 이벤트 기반의 서비스를 이용하여 해당하는 요청이 발생될 때마다 특정 동작이 수행되도록 설계하였습니다. 

## Settings

본 프로젝트에서 이용할 프로그램들은 다음과 같습니다. 

1. Visual Studio 2017

Client Application으로는 윈도우 10 기기에서 동작하는 애플리케이션인 UWP App으로 개발할 예정입니다. 또한 Web API를 이용하여 간단하게 서버사이드 로직을 구현할 예정입니다. 이를 위해서는 컴퓨터에 [Visual Studio 2017을 설치](https://www.visualstudio.com/ko/)한 후에 Visual Studio Installer에서 다음의 옵션들을 선택하고 다운로드 받으시면 됩니다. 

![004](./images/004.PNG)

2. Chrome 및 Postman 

개발자 도구 및 API 테스트를 편리하게 할 수 있는 아주 유용한 도구인 POSTMAN 사용을 위해 크롬을 설치한 후 -> POSTMAN을 설치하시기 바랍니다.  

* [Chrome 설치](https://support.google.com/chrome/answer/95346?co=GENIE.Platform%3DDesktop&hl=ko) 
* [POSTMAN 설치](https://chrome.google.com/webstore/detail/postman/fhbjgbiflinjbdggehcddcbncdddomop) 

![005](./images/005.PNG)

3. Azure Storage Explorer

Azure Storage에 있는 파일 목록을 쉽게 확인하고 파일 추가 및 삭제 권한 관리등이 가능합니다. 

* [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/)

![006](./images/006.PNG)

4. Git

소스코드 형상관리를 위해 Git을 사용할 예정입니다. 

* [Git 다운로드](https://git-scm.com/downloads)
* [Git 사용법](http://rogerdudler.github.io/git-guide/index.ko.html)

## Detail Information

### Server-side
1. [POST] api/persongroups/{PersonGroupId}/persons
** parameter: PersonGroupId
** return: PersonId
: 위의 API를 요청하면 다음과 같은 PersonId 리턴함

2. [POST] api/upload
** parameter: 이미지 전송
** return: FaceId
Blob 스토리지에 이미지를 저장 (Blob/idcard)
-> Vision/OCR 호출한 후 JSON 데이터를 Queue에 전송 
-> Face API / Dectect를 호출한 후 FaceId 클라이언트에 반환 

3. [POST] api/photo
-> Blob Storage에 사진파일 저장
-> PersonId 랑 Blob Storage URL, PersonGroupId 큐에 저장 

4. [GET] api/persoungroup/{personGroupId}/training
** parameter: personGroupId
학습 완료 여부 알려줌 

5. [POST] api/verify
** parameter: personGroupId, FaceId, PersonId
동일인인지 여부 (퍼센트 소수점 숫자 반환)

### Client-side