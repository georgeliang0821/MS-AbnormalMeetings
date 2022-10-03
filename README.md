# Data Platform for Log Analytics
> Especially for Abnormal Meetings.

<!-- > Live demo [_here_](https://www.example.com). If you have the project hosted somewhere, include the link here. -->

## Table of Contents
* [Steps to Demo](#steps-to-demo)
* [General Info](#general-information)
* [Setup](#setup)
* [Features](#features)
* [Technologies Used](#technologies-used)
* [Project Status](#project-status)
* [Room for Improvement](#room-for-improvement)
* [Acknowledgements](#acknowledgements)
* [Contact](#contact)
<!-- * [Screenshots](#screenshots) -->
<!-- * [Usage](#usage) -->
<!-- * [License](#license) -->


## Steps To Demo
### Prerequisite
1. Having an ***admin account*** of an organization or having a ***client secret*** with [required API permissions](#required-api-permissions)
2. [Create a blob storage with several containers](#create-bolb-storage-and-containers)
### Deploy
- Step 1: Clone this project.
- Step 2: Create a [Function App named "AbnormalMeetings"](#create-function-app)
- Step 3: [Deploy source code to Azure.](#deploy-source-code)
- Step 4: [Setup environment variables.](#setting-environment-variables-configuration)
### Demo
#### CallRecords
- Step 1: [Join/Create] a [call/meeting] in teams.
- Step 2: Wait others to join.
- Step 3: [Share/not share] your screen.
- Step 4: After all tests, end the [call/meeting].
- Step 5: **Wait 30 minutes.** 
- Step 6: Go to `blob storage` to check your log!
#### UserEvents
- Step 1: [Create/Modify] a event in [outlook/teams].
- Step 2: **Wait 30 minutes.**
- Step 3: Go to `blob storage` to check your log!

## General Information
### Requirement and Scenario
- TSMC want to know whether the insiders exist in Teams.
- The abnormal meeting has two characteristics.
    1. 1 on 1 meeting, especially meeting with the one using personal email.
    2. Screen sharing in the meeting.
- This project is tended to find out 
    1. How many people are there in the call?
    2. Is screen sharing in the meeting?
    3. Email addresses of the call participates.
- Finally, we write the information to Azure storage container.
<!-- ### Why 30 minutes waiting is needed?
- Logs about the call will be recorded in the blob storage 30 minutes later after the meeting ends.
    - For example. If the meeting ends at 11:10. Logs about the call will be sent to blob storage at 11:40. -->

<!-- ### Environment variables
- All variables which has been `highlighted`, are variables in environment. -->

<!-- ## Screenshots
- Successfully Run in VS
    ![successRunInVS_20220919](./img/successRunInVS_20220919.png) -->
<!-- If you have screenshots you'd like to share, include them here. -->

## Setup
### Required API permissions
> Please make sure that you have all API permissions below.
- User.Read.All
    - For "List Users"
- Calendars.Read
    - For "List Events"
- CallRecords.Read.All
    - For "CallRecords"
#### How to add permissions?
![](./img/azPortal_ApiPermissions.png)

### Create Bolb storage and containers
- we create a blob storage named `callrecordsaved`
- then create three container
    - callrecord-save
    - subscription-list
    - userevents
- add a blank json file named `subscriptionList.json` in container `subscription-list`
    ![](./img/azPortal_containers.png)

### Create Function App
- Go to Azure Portal
    - Go to "Function App" -> Create a function app named "AbnormalMeetings" 
        ![](./img/azPortal_createApp.png)
### Deploy Source Code
- Open project in `src` folder using Visual Studio 2022
- Right click "Publish.." in Visual Studio 2022
    ![vsPublish_withSteps](./img/vsPublish_withSteps.png)
- Choose to publish in "Azure" -> "Azure Function App (Windows) -> "AbnormalMeetings"
- Click "finish" botton
    ![vsPublish_profile](./img/vsPublish_profile.png)
- Then click "publish" button.
    ![](./img/vsPublish_profilePublish.png)
- **Well done!** The function app will be deployed in Azure.
### Setting Environment Variables (Configuration)
<!--
- The specified location will be 
    1. Deployed Azure: in the environment variable
    2. Local develop (Visual Studio): local.setting.json
-->
- Belows are all needed configuration variables.
- If you are deploying them on Azure, please go to "AbnormalMeetings" -> "Configuration" (in settings) and specified them.
    ![azPortal_functionApp_configuration](./img/azPortal_functionApp_configuration.png)
- If you are developing them locally, please go to "local.settings.json" and specified them.

#### Example Settings
> You can copy this example if you needed.
```json
[
  {
    "name": "ApiUrl",
    "value": "https://graph.microsoft.com/",
    "slotSetting": false
  },
  {
    "name": "APPINSIGHTS_INSTRUMENTATIONKEY",
    "value": "6565020d-029b-4064-98df-d28c37c64d80",
    "slotSetting": true
  },
  {
    "name": "APPLICATIONINSIGHTS_CONNECTION_STRING",
    "value": "InstrumentationKey=6565020d-029b-4064-98df-d28c37c64d80;IngestionEndpoint=https://japaneast-1.in.applicationinsights.azure.com/;LiveEndpoint=https://japaneast.livediagnostics.monitor.azure.com/",
    "slotSetting": false
  },
  {
    "name": "AzureWebJobs.RenewSubscription.Disabled",
    "value": "0",
    "slotSetting": false
  },
  {
    "name": "AzureWebJobsSecretStorageType",
    "value": "files",
    "slotSetting": false
  },
  {
    "name": "AzureWebJobsStorage",
    "value": "DefaultEndpointsProtocol=https;AccountName=abnormalmeetings;AccountKey=VEHXXyFn2v7bX4VMi3WRbTDnwx6gRd57woSRDKknsaEy4HYDCcrEsswL+f4I05vYZE7OM8b4zA/7+AStfUmgjg==;EndpointSuffix=core.windows.net",
    "slotSetting": false
  },
  {
    "name": "BlobConnectionString",
    "value": "DefaultEndpointsProtocol=https;AccountName=callrecordsaved;AccountKey=41EWWoju5Iqj3F7JBLgvq4rLS29FoY8YxrRAqT0H7biLkUAzquDzKvOsSpMU7k6Bc+4xJyPxyoIS+AStZOjSsw==;EndpointSuffix=core.windows.net",
    "slotSetting": false
  },
  {
    "name": "BlobContainerName_CallRecords",
    "value": "callrecord-save",
    "slotSetting": false
  },
  {
    "name": "BlobContainerName_SubscriptionList",
    "value": "subscription-list",
    "slotSetting": false
  },
  {
    "name": "BlobContainerName_UserEvents",
    "value": "userevents",
    "slotSetting": false
  },
  {
    "name": "BlobFileName",
    "value": "subscriptionList.json",
    "slotSetting": false
  },
  {
    "name": "ClientId",
    "value": "8af4b707-a9dd-4107-ac93-e0f6280cea72",
    "slotSetting": false
  },
  {
    "name": "ClientSecret",
    "value": "8IV8Q~AGX1654mEETqJCFSUM7Mb7vUTQ3plKJbLQ",
    "slotSetting": false
  },
  {
    "name": "FUNCTIONS_EXTENSION_VERSION",
    "value": "~4",
    "slotSetting": false
  },
  {
    "name": "FUNCTIONS_WORKER_RUNTIME",
    "value": "dotnet",
    "slotSetting": false
  },
  {
    "name": "Instance",
    "value": "https://login.microsoftonline.com/{0}",
    "slotSetting": false
  },
  {
    "name": "subscriptionId",
    "value": "ddef3572-767f-44e3-83fc-5f0b0a98a1c1",
    "slotSetting": false
  },
  {
    "name": "Tenant",
    "value": "37e86d06-0eef-46b9-90a7-117121b71fd3",
    "slotSetting": false
  },
  {
    "name": "Webhook_CallRecords",
    "value": "https://abnormalmeetings.azurewebsites.net/api/GetCallRecords_Http?code=Bk93Z90dgvoWvZ1ZUxqiJH62QENjfOYgO1yTr7bwkEomAzFunj8YHg==",
    "slotSetting": false
  },
  {
    "name": "Webhook_UserEvents",
    "value": "https://abnormalmeetings.azurewebsites.net/api/CallRecord_UserEvents?code=dFUajqD9Upi_Poef6TuJyns4iHzSkR0Z-kUULLcbicxoAzFudX5C9A==",
    "slotSetting": false
  },
  {
    "name": "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING",
    "value": "DefaultEndpointsProtocol=https;AccountName=abnormalmeetings;AccountKey=VEHXXyFn2v7bX4VMi3WRbTDnwx6gRd57woSRDKknsaEy4HYDCcrEsswL+f4I05vYZE7OM8b4zA/7+AStfUmgjg==;EndpointSuffix=core.windows.net",
    "slotSetting": false
  },
  {
    "name": "WEBSITE_CONTENTSHARE",
    "value": "abnormalmeetings9f51",
    "slotSetting": false
  }
]
```
#### Variables Needed Modified
#### Blob storage related
- "BlobConnectionString",
<!-- - "BlobContainerName_CallRecords",
- "BlobContainerName_UserEvents",
- "BlobContainerName_SubscriptionList": "subscription-list",
- "BlobFileName": "subscriptionList.json", -->
##### Explanation
- `BlobConnectionString` is the blob connectionstring, where you want to store your data
<!-- - `BlobContainerName_CallRecords`, `BlobContainerName_UserEvents` are the containers that are used to store results after calling graph api
- `BlobContainerName_SubscriptionList` is the container that used to store the `BlobFileName`, which will be used in subscription. -->
#### Azure Active Directory
<!-- - "Instance": "https://login.microsoftonline.com/{0}",
- "ApiUrl": "https://graph.microsoft.com/", -->
- "Tenant",
- "ClientId",
- "ClientSecret",
##### How to get `Tenant`, `ClientId`, `ClientSecret`
- You have already get the `ClientSecret` [here](#required-api-permissions)
- Go to "Azure Active Dirrectory" -> "App Registrations" -> "Overview" and record several infornmation
    - `Tenant`: Specified in "Directory (tenant) ID"
    - `ClientId`: Specified in "Application (client) ID"
#### Function URLs (for webhook)
- "Webhook_CallRecords",
- "Webhook_UserEvents"
##### How to get `Webhook_CallRecords`, `Webhook_UserEvents`
-After you deployed the function app to Azure, go to "AbnormalMeetings" -> "Funtions" -> "GetCallRecords" or "GetUserEvents" -> "get function url" to get the webhook urls

<!-- ## Usage
- After deployment and setting configuration, the function will start working!
- You can go to "Monitor" in functions to monitor the logs of azure functions -->

## Features 

### RenewSubscription.cs
- All subscription-related tasks will be done here.
- The program will read the json file specifiled in `BlobFileName`, under the container specified in `BlobContainerName_SubscriptionList`. 
- The json file's format is, for example, 
    ```json
    {
        "value":[
            {
                "UserId":"7f5b5e74-ba40-4aed-95be-d55a57e684fa",
                "SubscriptionId":"b32157a5-3dfc-4743-b84b-5dcb6c90c0ed"
            },
            ...,
            {
                "UserId":"callRecordId","SubscriptionId":"b6978c22-d086-47de-8f0b-31e68a5302f8"
            }
        ]
    }        
    ```
#### Renew CallRecords Subscription
- If the CallRecords have been subscribed before and have been recorded in `BlobFileName` ("UserId":"callRecordId"), just simply renew the subscription; otherwise, create a subscription and record it into `BlobFileName`.
#### Renew UserEvent Subscriptions
- I will get all users under the target tenant, if the user have been subscribed before and have been recorded in `BlobFileName` ("UserId":"{TheUserId}}"), just simply renew the subscription; otherwise, create a subscription and record it into `BlobFileName`.
### GetCallRecords.cs
- When the program recieves http request from the subscription resource, the program will be triggered.
- Use the call Id to get the callrecord and save it as a json file to container specified in `BlobContainerName_CallRecords`, using graph api SDK
    ```C#
    CallRecord callrecord = await graphServiceClient.
    // Here we used "Expand" to get full infornmation
    // in callrecord
    Communications.CallRecords[call_Id]
                    .Request()
                    .Expand("sessions($expand=segments)")
                    .GetAsync();
    ```
### GetUserEvents.cs
- When the program recieve http request from the subscription resource, the program will be triggered.
- Use the `resource` to get the event object and save it as a json file to container specified in `BlobContainerName_UserEvents`, using Http request.
    ```C#
    // The resource are the target
    // of user event subscription resource
    string resource = subscriptionData.value[0].resource;
    string webApiUrl = $"{config.ApiUrl}v1.0/{resource}";
    HttpResponseMessage response = await httpClient.GetAsync(webApiUrl);
    ```

## Technologies Used
### Azure Functions Core Tools
- Core Tools Version: 4.0.4736 Commit hash: N/A  (64-bit)
- Function Runtime Version: 4.8.1.18957

### PackageReference
- "Microsoft.NET.Sdk.Functions" - Version="4.0.1"
- "Microsoft.Graph" - Version="4.35.0"
- "Microsoft.Identity.Client" - Version="4.45.0"
- "Microsoft.Identity.Web" - Version="1.25.1"
- "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs" - Version="5.0.1"


## Project Status
Project is: _complete_
<!--
Project is: _in progress_ / _complete_ / _no longer being worked on_. If you are no longer working on it, provide reasons why.
-->

## Room for Improvement
- Error handling functions 

## Acknowledgements
Many thanks to Goerge Liang, my mentor in MS.
<!-- 
Give credit here.
- This project was inspired by...
- This project was based on [this tutorial](https://www.example.com).
- Many thanks to...
-->

## Contact
Created by [@Eric](https://github.com/yhlu0923/) - feel free to contact me!
<!-- 
Created by [@flynerdpl](https://www.flynerd.pl/) - feel free to contact me!
-->

<!-- Optional -->
<!-- ## License -->
<!-- This project is open source and available under the [... License](). -->

<!-- You don't have to include all sections - just the one's relevant to your project -->
