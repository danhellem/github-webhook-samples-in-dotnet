
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Octokit;
using WebHookSample.Models;
using WebHookSample.ViewModels;

namespace WebHookSample.Repos
{
    /// <summary>
    /// Issues Repo - Used to connect to GitHub Issues
    /// </summary>
    public class IssuesRepo : IIssuesRepo
    {
        private string _token = "";
        private string _appName = "";

        private IOptions<AppSettings> _appSettings;

        public IssuesRepo(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;

            _token = _appSettings.Value.GitHubToken;
            _appName = _appSettings.Value.GitHubAppName;
        }

        /// <summary>
        /// Get open issues for a milestone
        /// </summary>
        /// <param name="vm">IssuesConfigViewModel</param>
        /// <returns>List of open issues</returns>
        public IReadOnlyList<Octokit.Issue> GetOpenIssuesForMilestone(WorkingViewModel vm)
        {
            //connect to client
            var client = new GitHubClient(new Octokit.ProductHeaderValue(_appName));
            var tokenAuth = new Credentials(_token);
            client.Credentials = tokenAuth;

            //create filter to pull just the open issues per milestone number
            RepositoryIssueRequest filter = new RepositoryIssueRequest
            {               
                Milestone = vm.milestone_number.ToString(),
                State = ItemStateFilter.Open       
            };

            //fill list with results
            IReadOnlyList<Octokit.Issue> issues = client.Issue.GetAllForRepository(vm.organization, vm.repository, filter).Result;

            return issues;
        }

        /// <summary>
        /// Update Lable for a given issue
        /// </summary>
        /// <param name="vm">IssuesConfigViewModel</param>
        /// <returns>Issue object</returns>
        public Octokit.Issue UpdateLabel(WorkingViewModel vm, string label)
        {
            //connect and build client
            var client = new GitHubClient(new Octokit.ProductHeaderValue(_appName));
            var tokenAuth = new Credentials(_token);
            client.Credentials = tokenAuth;            
                 
            //issue we want to update
            IssueUpdate issueUpdate = vm.issue.ToUpdate();

            //add label to issue
            issueUpdate.AddLabel(label);

            //complete update and return results
            var result = client.Issue.Update(vm.organization, vm.repository, vm.issue.Number, issueUpdate).Result;

            return result;
        }
    }

    public interface IIssuesRepo
    {
        IReadOnlyList<Octokit.Issue> GetOpenIssuesForMilestone(WorkingViewModel vm);
        Octokit.Issue UpdateLabel(WorkingViewModel vm, string label);       
    }
}




