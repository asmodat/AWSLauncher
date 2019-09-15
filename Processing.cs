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
using AWSWrapper.Extensions;
using AWSLauncher.Models;
using AWSWrapper.EC2;

namespace AWSLauncher
{
    public partial class Function
    {
        public async Task<Instance[]> TagsClenup(Instance[] instances)
        {
            if (instances.IsNullOrEmpty())
                return new Instance[0];

            var inactives = instances?.Where(x => x.HasTags() && x.IsTerminating());

            var clean = new List<Instance>();
            foreach(var i in instances)
            {
                if (!i.HasTags())
                    continue;

                if (i.IsTerminating()) //if has tags and is terminating or terminated
                {
                    Log($"Removing Tags from the instance {i.InstanceId}...");
                    var deleteTags = await _EC2.DeleteAllInstanceTags(i.InstanceId);
                }
                else //is clean
                    clean.Add(i);
            }

            return clean.ToArray();
        }

        public async Task Processing()
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


            Log($"Loading instances...");
            var instances = await _EC2.ListInstances();
            instances = await TagsClenup(instances);

            Log($"Found {configs.Count} configuration files and {instances?.Count() ?? 0} active instances.");
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