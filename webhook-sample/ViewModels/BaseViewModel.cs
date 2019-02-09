using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebHookSample.ViewModels
{
    public class BaseViewModel
    {
        public string repository {  get; set; }
        public string organization {  get; set; }
        public string token {  get; set; }
        public string appName {  get; set; }
    }
}
