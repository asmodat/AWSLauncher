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
using AsmodatStandard.Networking;

namespace AWSLauncher
{
    public partial class Function
    {
        public async Task Processing(Instance[] instances)
        {
            Log($"Loading github configuration files...");
            var files = await _GIT.GetGitHubTrees();
            var instancesPath = Environment.GetEnvironmentVariable("instances_path");
            var instanceObjects = files.GetObjectsByPath(path: instancesPath)?.Where(x => x.IsTree());

            var configs = new List<EC2InstanceConfig>();
            foreach (var instanceObject in instanceObjects)
            {
                var configObject = instanceObject?.objects.FirstOrDefault(x => x.path.ToLower() == "config.json");

                if (configObject?.IsBlob() != true)
                {
                    Log($"Failed to load config file from {instancesPath}/{instanceObject?.path ?? "undefined"}");
                    continue;
                }

                var blob = await _GIT.GetGitHubBlob(configObject);
                var cfg = blob.JsonDeserialize<EC2InstanceConfig>();
                configs.Add(cfg);
            }

            var parallelism = Environment.GetEnvironmentVariable("parallelism").ToIntOrDefault(1);

            Log($"Found {configs.Count} configuration files.");
            if (!configs.IsNullOrEmpty())
                await ParallelEx.ForEachAsync(configs, async cfg =>
                {
                    /*/// TESTNET
                    await Launcher(instances, cfg);
                    /*/// PRODUCTION
                    try
                    {
                        Log($"Processing '{cfg?.name ?? "undefined"} config'...");
                        await Launcher(instances, cfg);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"Failed to process {cfg?.name ?? "undefined"}, error: {ex.JsonSerializeAsPrettyException()}");
                    }
                    //*/
                }, maxDegreeOfParallelism: parallelism);
            Log($"Done, all configuration files were processed.");
        }
    }
}