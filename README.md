# Data Platform for Log Analytics
> Especially for Abnormal Meetings.

<!-- > Live demo [_here_](https://www.example.com). If you have the project hosted somewhere, include the link here. -->

## Table of Contents
* [General Info](#general-information)
* [Technologies Used](#technologies-used)
* [Features](#features)
* [Screenshots](#screenshots)
* [Setup](#setup)
* [Usage](#usage)
* [Project Status](#project-status)
* [Room for Improvement](#room-for-improvement)
* [Acknowledgements](#acknowledgements)
* [Contact](#contact)
<!-- * [License](#license) -->


## General Information
- TSMC want to know whether the insiders exist in Teams.
- The abnormal meeting has two characteristics.
    1. 1 on 1 meeting, especially meeting with the one using personal email.
    2. Screen sharing in the meeting.
- This project is tend to find out 
    1. How many people in the call?
    2. Is screen sharing in the meeting?
    3. Email addresses of the call participates.
- Finally, we write the information to Azure storage container.


## Technologies Used
### Azure Functions Core Tools
- Core Tools Version: 4.0.4736 Commit hash: N/A  (64-bit)
- Function Runtime Version: 4.8.1.18957

### PackageReference
"Microsoft.NET.Sdk.Functions" - Version="4.0.1"
"Microsoft.Graph" - Version="4.35.0"
"Microsoft.Identity.Client" - Version="4.45.0"
"Microsoft.Identity.Web" - Version="1.25.1"
"Microsoft.Azure.WebJobs.Extensions.Storage.Blobs" - Version="5.0.1"

## Features 
- The specified location will be 
    1. Deployed Azure: in the environment variable
    2. Local develop (Visual Studio): local.setting.json
- 
### RenewSubscription.cs
- All subscription-related tasks will be done here.
- The program will read the json file specifiled in `BlobFileName`, under the container specified in `BlobContainerName_SubscriptionList`. 
- The json file's format is, for example, 
    ```
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
#### Renew CallRecords
- If the CallRecords have been subscribed before and have been recorded in `BlobFileName` ("UserId":"callRecordId"), just simply renew the subscription; otherwise, create a subscription and record it into `BlobFileName`.
#### Renew UserEvents
- I will get all users under the target tenant, if the user have been subscribed before and have been recorded in `BlobFileName` ("UserId":"{TheUserId}}"), just simply renew the subscription; otherwise, create a subscription and record it into `BlobFileName`.
### GetCallRecords.cs
- When the program recieve http request from the subscription resource, the program will be triggered.
- Use the call Id to get the callrecord and save it as a json file to container specified in `BlobContainerName_CallRecords`, using graph api SDK
    ```C#
    CallRecord callrecord = await graphServiceClient.Communications.CallRecords[call_Id]
                    .Request()
                    .Expand("sessions($expand=segments)")
                    .GetAsync();
    ```
### GetUserEvents.cs
- When the program recieve http request from the subscription resource, the program will be triggered.
- Use the `resource` to get the event object and save it as a json file to container specified in `BlobContainerName_UserEvents`, using Http request.
    ```C#
    string resource = subscriptionData.value[0].resource;
    string webApiUrl = $"{config.ApiUrl}v1.0/{resource}";
    HttpResponseMessage response = await httpClient.GetAsync(webApiUrl);
    ```

## Screenshots
- Successfully Run in VS
    ![successRunInVS_20220919](./img/successRunInVS_20220919.png)
<!-- If you have screenshots you'd like to share, include them here. -->


## Setup
What are the project requirements/dependencies? Where are they listed? A requirements.txt or a Pipfile.lock file perhaps? Where is it located?

Proceed to describe how to install / setup one's local environment / get started with the project.


## Usage
How does one go about using it?
Provide various use cases and code examples here.

`write-your-code-here`


## Project Status
Project is: _in progress_ / _complete_ / _no longer being worked on_. If you are no longer working on it, provide reasons why.


## Room for Improvement
Include areas you believe need improvement / could be improved. Also add TODOs for future development.

Room for improvement:
- Improvement to be done 1
- Improvement to be done 2

To do:
- Feature to be added 1
- Feature to be added 2



## Acknowledgements
Give credit here.
- This project was inspired by...
- This project was based on [this tutorial](https://www.example.com).
- Many thanks to...


## Contact
Created by [@flynerdpl](https://www.flynerd.pl/) - feel free to contact me!


<!-- Optional -->
<!-- ## License -->
<!-- This project is open source and available under the [... License](). -->

<!-- You don't have to include all sections - just the one's relevant to your project -->