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

### Settings

