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
        
        public Function()
        {
            _EC2 = new EC2Helper();
            _ELB = new ELBHelper();
            _SM = new SMHelper();
        }

        public async Task FunctionHandler(ILambdaContext context)
        {
            var sw = Stopwatch.StartNew();
            _context = context;
            _logger = _context.Logger;
            _logger.Log($"{context?.FunctionName} => {nameof(FunctionHandler)} => Started");

            var githubToken = Environment.GetEnvironmentVariable("github_token");

            if (githubToken.IsNullOrEmpty())
                throw new Exception("Environment Variable 'github-token' was not defined!");

            var accessToken = (await _SM.GetSecret(githubToken)).JsonDeserialize<AmazonSecretsToken>()?.token;

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
                var instances = await _EC2.ListInstances();
                await Processing(instances, context.Logger);
            }
            finally
            {
                context.Logger.Log($"{context?.FunctionName} => {nameof(FunctionHandler)} => Stopped, Eveluated within: {sw.ElapsedMilliseconds} [ms]");
                await Task.Delay(2500);
            }
        }
    }
}
