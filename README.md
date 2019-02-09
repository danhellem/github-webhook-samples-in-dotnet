# ecosystem-danhellem-webhook-part-1

This is a simple web hook reciever endpoint. The endpoint looks for the "milestone" event from GitHub. If the milestone is set to close but has open issues, the reciever will update those issues with a "Needs Attention!" tag. 

In this scenario milestones should not be closed if there are still open issues attached to it.

![sample image](https://github.com/github-interviews/ecosystem-danhellem-webhook-part-1/blob/master/sample-image.PNG "sample")

## basic information

* Written in C# and .net core 2.2
* Unit tests are written for the MilestonesController using MSTest and Moq
* appsettings.json is where your secret and token need to be stored
