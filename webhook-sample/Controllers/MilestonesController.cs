using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebHookSample.Misc;
using WebHookSample.Repos;
using WebHookSample.ViewModels;

namespace WebHookSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MilestonesController : ControllerBase
    {
        public IIssuesRepo _issuesRepo;      
        public IAuthentication _authentication;

        public MilestonesController(IIssuesRepo issuesRepo, IAuthentication authentication)
        {
            _issuesRepo = issuesRepo;          
            _authentication = authentication;
        }

        // POST
        [HttpPost]
        public IActionResult Post([FromBody] JObject body)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();

            Request.Headers.TryGetValue("X-Hub-Signature", out StringValues signature);                
            
            //check for empty signature
            if (string.IsNullOrEmpty(signature))
            {
                response.Message = "Missing signature header value";
                return new StandardResponseObjectResult(response, StatusCodes.Status401Unauthorized);
            }

            //make sure something did not go wrong
            if (body == null)
            {
                response.Message = "Posted object cannot be null.";

                return new StandardResponseObjectResult(response, StatusCodes.Status400BadRequest);
            }

            var payload = JsonConvert.SerializeObject(body);

            //check body and signature to match against secret
            var isGitHubPushEventAllowed = _authentication.IsValidGitHubWebHookRequest(payload, signature);

            //if we passed the secret check, then continue
            if (! isGitHubPushEventAllowed)
            {
                response.Message = "Invalid signature.";
                
                return new StandardResponseObjectResult(response, StatusCodes.Status401Unauthorized);
            }                       

            WorkingViewModel vm = this.BuildWorkingViewModel(body);

            //if the event action is somethign other than closed, then exit
            if (vm.action != "closed")
            {
                response.Success = true;
                response.Message = "Milestone state is open. No further action.";
                    
                return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
            }

            //if there are not open issues, then exit
            if (vm.open_issues == 0)
            {
                response.Success = true;
                response.Message = "No open issues for this milestone. No further action.";

                return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
            }          

            //get list of open issues for the posted milestone
            var list = _issuesRepo.GetOpenIssuesForMilestone(vm);
          
            //check and make sure we ahve items in the list
            if (list != null && list.Count > 0)
            {
                string msg = "Issues updated: ";

                //for each issue in the list go update the label
                foreach (Octokit.Issue issue in list)
                {
                    vm.issue = issue;
                    _issuesRepo.UpdateLabel(vm, "Needs Attention!");

                    msg += issue.Number + ",";
                }

                msg = msg.Remove(msg.Length - 1, 1);

                //compile final response message of successful
                response.Success = true;
                response.Message = msg;

                return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
            } 
            else
            {
                response.Success = true;
                response.Message = "No open issues found.";

                return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
            }           
        }

        private WorkingViewModel BuildWorkingViewModel(JObject body)
        {
            WorkingViewModel vm = new WorkingViewModel();

            vm.action = (string)body["action"];
            vm.full_name = (string)body["repository"]["full_name"];
            vm.milestone_number = (int)body["milestone"]["number"];
            vm.open_issues = (int)body["milestone"]["open_issues"];            
                       
            string[] split = vm.full_name.Split("/");

            vm.organization = split[0];
            vm.repository = split[1];           

            return vm;
        }
    }
}
