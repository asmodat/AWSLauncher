using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using Amazon.Lambda.Core;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Extensions.Threading;
using AWSLauncher.Models.Infrastructure;
using GITWrapper.GitHub.Models;

/// <summary>
/// Starts, stops and termintes instances based on Auto On, Auto Off, Auto Kill tags with the use of Cron format for the value
/// https://docs.aws.amazon.com/lambda/latest/dg/tutorial-scheduled-events-schedule-expressions.html
/// </summary>
namespace AWSLauncher
{
    public partial class Function
    {
        public async Task Processing(Instance[] instances, ILambdaLogger logger)
        {
            var files = await _GIT.GetGitHubTrees();
            var instancesPath = Environment.GetEnvironmentVariable("instances_path");
            var instanceObjects = files.GetObjectsByPath(path: instancesPath)?.Where(x => x.IsTree());

            var configs = new List<EC2InstanceConfig>();
            foreach (var instanceObject in instanceObjects)
            {
                var configObject = instanceObject?.objects.FirstOrDefault(x => x.path.ToLower() == "config.json");

                if (configObject?.IsBlob() != true)
                {
                    _logger.LogLine($"Failed to load config file from {instancesPath}/{instanceObject?.path ?? "undefined"}");
                    continue;
                }

                var blob = await _GIT.GetGitHubBlob(configObject);
                var cfg = blob.JsonDeserialize<EC2InstanceConfig>();
                configs.Add(cfg);
            }

            var parallelism = Environment.GetEnvironmentVariable("parallelism").ToIntOrDefault(1);

            if (!configs.IsNullOrEmpty())
                await ParallelEx.ForEachAsync(configs, async cfg =>
                {
                    /*/// TESTNET
                    await Launcher(instances, cfg);
                    /*/// PRODUCTION
                    try
                    {
                        await Launcher(instances, cfg);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Failed to process {cfg?.name ?? "undefined"}, error: {ex.JsonSerializeAsPrettyException()}");
                    }
                    //*/
                }, maxDegreeOfParallelism: parallelism);
        }
    }
}