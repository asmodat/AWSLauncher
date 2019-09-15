using Amazon.Lambda.Core;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using GITWrapper.GitHub.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GITWrapper.GitHub
{
    public partial class GitHubHelper
    {
        public Task<GitHubTree> GetGitHubTrees(GitHubObject go) => GetGitHubTrees(go?.url);
        public async Task<GitHubTree> GetGitHubTrees(string request = null)
        {
            var at = await _config.GetAccessToken();
            var accessToken = at.IsNullOrWhitespace() ? "" : $"?access_token={at}";
            request = request.IsNullOrEmpty() ? $"https://api.github.com/repos/{_config.user}/{_config.repository}/git/trees/{_config.branch}{accessToken}" :
                                                $"{request}{accessToken}";

            var root = await HttpHelper.GET<GitHubTree>(
                                    requestUri: request,
                                    ensureStatusCode: System.Net.HttpStatusCode.OK,
                                    defaultHeaders: new (string, string)[] {
                                        ("User-Agent", _config.userAgent)
                                    });

            if (!(root?.tree).IsNullOrEmpty())
                foreach(var o in root.tree)
                    if(o.IsTree())
                        o.tree = await this.GetGitHubTrees(o);

            return root;
        }

    }
}
