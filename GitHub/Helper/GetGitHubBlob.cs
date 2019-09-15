using AsmodatStandard.Extensions;
using GITWrapper.GitHub.Models;
using System.Threading.Tasks;

namespace GITWrapper.GitHub
{
    public partial class GitHubHelper
    {
        public async Task<GitHubBlob> GetGitHubBlob(GitHubObject obj)
        {
            var at = await _config.GetAccessToken();
            var accessToken = at.IsNullOrWhitespace() ? "" : $"?access_token={at}";
            var request = $"{obj.url}{accessToken}";

            return await HttpHelper.GET<GitHubBlob>(
                                    requestUri: request,
                                    ensureStatusCode: System.Net.HttpStatusCode.OK,
                                    defaultHeaders: new (string, string)[] {
                                        ("User-Agent", _config.userAgent)
                                    });
        }
    }
}
