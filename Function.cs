using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using Amazon.Lambda.Core;
using AWSWrapper.EC2;
using AWSWrapper.ELB;
using GITWrapper.GitHub;
using AWSLauncher.Models;
using AsmodatStandard.Networking;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace AWSLauncher
{
    public partial class Function
    {
        private EC2Helper _EC2;
        private ELBHelper _ELB;
        private SMHelper _SM;
        private GitHubHelper _GIT;
        private ILambdaLogger _logger;
        private ILambdaContext _context;
        private bool _verbose;
        
        public Function()
        {
            _EC2 = new EC2Helper();
            _ELB = new ELBHelper();
            _SM = new SMHelper();
        }

        private void Log(string msg)
        {
            if (msg.IsNullOrEmpty() || !_verbose)
                return;

            _logger.Log(msg);
        }

        public async Task FunctionHandler(ILambdaContext context)
        {
            var sw = Stopwatch.StartNew();
            _context = context;
            _logger = _context.Logger;
            _logger.Log($"{context?.FunctionName} => {nameof(FunctionHandler)} => Started");

            _verbose = Environment.GetEnvironmentVariable("verbose").ToBoolOrDefault(true);
            var githubToken = Environment.GetEnvironmentVariable("github_token");


            if (githubToken.IsNullOrEmpty())
                throw new Exception("Environment Variable 'github-token' was not defined!");

            if (Environment.GetEnvironmentVariable("test_connection").ToBoolOrDefault(false))
                Log($"Your Internet Connection is {(SilyWebClientEx.CheckInternetAccess(timeout: 5000) ? "" : "NOT")} available.");

            string accessToken;

            if(githubToken.IsHex())
            {
                Log($"Property 'githubToken' was determined to be a hexadecimal and will be used as accessToken");
                accessToken = githubToken;
            }
            else
            {
                Log($"Fetching github access token '{githubToken ?? "undefined"}' from secrets manager.");
                accessToken = (await _SM.GetSecret(githubToken)).JsonDeserialize<AmazonSecretsToken>()?.token;
            }

            _GIT = new GitHubHelper(new GITWrapper.GitHub.Models.GitHubRepoConfig
            {
                user = Environment.GetEnvironmentVariable("github_user"),
                branch = Environment.GetEnvironmentVariable("github_branch"),
                repository = Environment.GetEnvironmentVariable("github_repository"),
                accessToken = accessToken,
                userAgent = Environment.GetEnvironmentVariable("user_agent") ?? "Asmodat Launcher Toolkit",
            });

            try
            {
                Log($"Loading instance informations....");
                var instances = await _EC2.ListInstances();
                Log($"Found {instances?.Length ?? 0} instances. Processing...");
                await Processing(instances);
            }
            finally
            {
                _logger.Log($"{context?.FunctionName} => {nameof(FunctionHandler)} => Stopped, Eveluated within: {sw.ElapsedMilliseconds} [ms]");
                await Task.Delay(2500);
            }
        }
    }
}
