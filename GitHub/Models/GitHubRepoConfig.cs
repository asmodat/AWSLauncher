using System;
using System.Collections.Generic;
using System.Text;

namespace GITWrapper.GitHub.Models
{
    public class GitHubRepoConfig
    {
        public string user { get; set; }
        public string branch { get; set; }
        public string repository { get; set; }
        public string userAgent { get; set; } = "Asmodat Deployment Toolkit";
        public string accessToken { get; set; }
    }
}
