using AsmodatStandard.Extensions;
using System.Diagnostics;
using System.Threading;
using AsmodatStandard.Extensions.Threading;
using System.Threading.Tasks;

namespace GITWrapper.GitHub.Models
{
    public class GitHubRepoConfig
    {
        private SemaphoreSlim ss = new SemaphoreSlim(1);
        /// <summary>
        /// Max requests per second
        /// </summary>
        public int maxRatePerHour { get; private set; }
        private readonly static object _locker = new object();
        private Stopwatch sw;
        private string accessToken;
        public GitHubRepoConfig(string accessToken, int maxRatePerHour)
        {
            this.maxRatePerHour = maxRatePerHour > 0 ? maxRatePerHour : 600;
            sw = Stopwatch.StartNew();
            this.accessToken = accessToken;
        }

        public string user { get; set; }
        public string branch { get; set; }
        public string repository { get; set; }
        public string userAgent { get; set; } = "Asmodat Launcher Toolkit";

        public async Task<string> GetAccessToken()
        {
            if (accessToken.IsNullOrEmpty())
                return accessToken;

            async Task<string> getToken()
            {
                var span = 3600 / maxRatePerHour;
                var sleep = (span * 1000) - sw.ElapsedMilliseconds;

                if (sleep > 0 && sleep < int.MaxValue)
                    await Task.Delay((int)sleep);

                sw.Restart();
                return accessToken;
            }

            return await ss.LockAsync(getToken());
        }
    }
}
