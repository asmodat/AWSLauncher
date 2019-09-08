using AsmodatStandard.Extensions;
using GITWrapper.GitHub.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GITWrapper.GitHub
{
    public partial class GitHubHelper
    {
        public Task<GitHubRepoCommits> GetGitHubCommits()
        {
            var accessToken = _config.accessToken.IsNullOrWhitespace() ? "" : $"?access_token={_config.accessToken}";
            var request = $"https://api.github.com/repos/{_config.user}/{_config.repository}/commits/{_config.branch}{accessToken}";

            return HttpHelper.GET<GitHubRepoCommits>(
                                    requestUri: request,
                                    ensureStatusCode: System.Net.HttpStatusCode.OK,
                                    defaultHeaders: new (string, string)[] {
                                        ("User-Agent", _config.userAgent)
                                    });
        }
    }
}
