using AsmodatStandard.Extensions;
using GITWrapper.GitHub.Models;
using System.Threading.Tasks;

namespace GITWrapper.GitHub
{
    public partial class GitHubHelper
    {
        public Task<GitHubBlob> GetGitHubBlob(GitHubObject obj)
        {
            var accessToken = _config.accessToken.IsNullOrWhitespace() ? "" : $"?access_token={_config.accessToken}";
            var request = $"{obj.url}{accessToken}";

            return HttpHelper.GET<GitHubBlob>(
                                    requestUri: request,
                                    ensureStatusCode: System.Net.HttpStatusCode.OK,
                                    defaultHeaders: new (string, string)[] {
                                        ("User-Agent", _config.userAgent)
                                    });
        }
    }
}
