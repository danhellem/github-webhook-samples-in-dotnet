using WebHookSample.Models;

namespace WebHookSample.ViewModels
{
    public class WorkingViewModel : BaseViewModel
    {
        public string action { get; set; }
        public int open_issues { get; set; }
        public string full_name { get; set; }
        public int milestone_number { get; set; }   
        public Octokit.Issue issue { get; set; }
    }
}
