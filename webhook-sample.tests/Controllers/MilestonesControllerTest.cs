using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using WebHookSample.Controllers;
using WebHookSample.Misc;
using WebHookSample.Repos;
using WebHookSample.ViewModels;
using Octokit;
using Newtonsoft.Json.Linq;
using System.IO;

namespace WebHookSample.Tests.Controllers
{
    [TestClass]
    public class MilestoneControllerTest
    {
        private Mock<IIssuesRepo> _mockIssuesRepo;
        private Mock<IAuthentication> _mockAuthentication;
        private MilestonesController _controller;
      
        [TestInitialize]
        public void TestInitialize()
        {
            _mockIssuesRepo = new Mock<IIssuesRepo>();
            _mockAuthentication = new Mock<IAuthentication>();

            //arrange
            _controller = new MilestonesController(_mockIssuesRepo.Object, _mockAuthentication.Object);

            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        }

        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_NoSignatureHeader()
        {
            //arrange
            JObject body = new JObject();

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status401Unauthorized, standardResponse.StatusCode);
            Assert.IsFalse(apiResponse.Success);
            Assert.AreEqual("Missing signature header value", apiResponse.Message);

            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_EmptySignature()
        {
            //arrange       
            _controller.ControllerContext.HttpContext.Request.Headers["X-Hub-Signature"] = "";

            JObject body = new JObject();

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status401Unauthorized, standardResponse.StatusCode);
            Assert.IsFalse(apiResponse.Success);
            Assert.AreEqual("Missing signature header value", apiResponse.Message);

            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_InvalidSignature()
        {
            //arrange       
            _controller.ControllerContext.HttpContext.Request.Headers["X-Hub-Signature"] = "12345";
            _mockAuthentication.Setup(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            JObject body = null;

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status401Unauthorized, standardResponse.StatusCode);
            Assert.IsFalse(apiResponse.Success);
            Assert.AreEqual("Invalid signature.", apiResponse.Message);

            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Never());
        }


        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_NullPostedPayload()
        {
            //arrange       
            _controller.ControllerContext.HttpContext.Request.Headers["X-Hub-Signature"] = "sha1=51e97b3fb9b73a7bf500511bb6ce9823d8079ecb";
            _mockAuthentication.Setup(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            JObject body = null;

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status400BadRequest, standardResponse.StatusCode);
            Assert.IsFalse(apiResponse.Success);
            Assert.AreEqual("Posted object cannot be null.", apiResponse.Message);

            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_ActionIsOpen()
        {
            //arrange       
            _controller.ControllerContext.HttpContext.Request.Headers["X-Hub-Signature"] = "sha1=51e97b3fb9b73a7bf500511bb6ce9823d8079ecb";
            _mockAuthentication.Setup(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            string json = @"
            {
                'action': 'open',
                'milestone': {
                    'number': 1,
                    'open_issues': 2    
                },
                'repository': {                
                    'full_name': 'danhellem/github-sample'
                }
            }";
        
            JObject body = JObject.Parse(json);           

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status200OK, standardResponse.StatusCode);
            Assert.IsTrue(apiResponse.Success);
            Assert.AreEqual("Milestone state is open. No further action.", apiResponse.Message);
            
            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_NoOpenIssues()
        {
            //arrange       
            _controller.ControllerContext.HttpContext.Request.Headers["X-Hub-Signature"] = "sha1=51e97b3fb9b73a7bf500511bb6ce9823d8079ecb";
            _mockAuthentication.Setup(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            string json = @"
            {
                'action': 'closed',
                'milestone': {
                    'number': 1,
                    'open_issues': 0    
                },
                'repository': {                
                    'full_name': 'danhellem/github-sample'
                }
            }";

            JObject body = JObject.Parse(json);

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status200OK, standardResponse.StatusCode);
            Assert.IsTrue(apiResponse.Success);
            Assert.AreEqual("No open issues for this milestone. No further action.", apiResponse.Message);

            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Never());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_NoIssuesFound()
        {
            //arrange       
            _controller.ControllerContext.HttpContext.Request.Headers["X-Hub-Signature"] = "sha1=51e97b3fb9b73a7bf500511bb6ce9823d8079ecb";
            _mockAuthentication.Setup(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            string json = @"
            {
                'action': 'closed',
                'milestone': {
                    'number': 1,
                    'open_issues': 2    
                },
                'repository': {                
                    'full_name': 'danhellem/github-sample'
                }
            }";
                       
            JObject body = JObject.Parse(json);

            IReadOnlyList<Octokit.Issue> list = new List<Octokit.Issue>();           
            _mockIssuesRepo.Setup(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>())).Returns(list);

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status200OK, standardResponse.StatusCode);
            Assert.IsTrue(apiResponse.Success);
            Assert.AreEqual("No open issues found.", apiResponse.Message);

            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        [TestCategory("Controllers")]
        [Priority(0)]
        public void MilestonesController_Success()
        {
            //arrange       
            _controller.ControllerContext.HttpContext.Request.Headers["X-Hub-Signature"] = "sha1=51e97b3fb9b73a7bf500511bb6ce9823d8079ecb";
            _mockAuthentication.Setup(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            string json = @"
            {
                'action': 'closed',
                'milestone': {
                    'number': 1,
                    'open_issues': 2    
                },
                'repository': {                
                    'full_name': 'danhellem/github-sample'
                }
            }";

            JObject body = JObject.Parse(json);

            IReadOnlyList<Octokit.Issue> list = new List<Octokit.Issue>()
            {
                new Octokit.Issue() { }, new Octokit.Issue() { }
            };

            _mockIssuesRepo.Setup(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>())).Returns(list);

            //act
            var result = _controller.Post(body);
            var standardResponse = (StandardResponseObjectResult)result;
            var apiResponse = (ApiResponseViewModel)standardResponse.Value;

            //assert
            Assert.IsInstanceOfType(result, typeof(IActionResult), "'result' type must be of IActionResult");
            Assert.AreEqual(StatusCodes.Status200OK, standardResponse.StatusCode);
            Assert.IsTrue(apiResponse.Success);
            Assert.AreEqual("Issues updated: 0,0", apiResponse.Message);

            _mockAuthentication.Verify(x => x.IsValidGitHubWebHookRequest(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.GetOpenIssuesForMilestone(It.IsAny<WorkingViewModel>()), Times.Once());
            _mockIssuesRepo.Verify(x => x.UpdateLabel(It.IsAny<WorkingViewModel>(), It.IsAny<string>()), Times.Exactly(2));
        }      
    }
}
