﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Types;
using AWSLauncher.Models.Infrastructure;
using AWSWrapper.EC2;
using AWSWrapper.Extensions;
using static AWSWrapper.EC2.EC2Helper;

namespace AWSLauncher
{
    public partial class Function
    {
        public async Task Launcher(Instance[] instances, EC2InstanceConfig cfg)
        {
            if (cfg?.enabled != true)
                throw new Exception($"Config {cfg?.name ?? "undefined"} is disabled and will not be processed.");

            var targetName = cfg?.name?.Trim()?.ToLower();
            if (targetName.IsNullOrEmpty())
                throw new Exception($"Can't process config, name property was not defined in the configuration file.");

            var namedInstances = instances?
                .Where(x => x?.Tags?.Any(y => y?.Key?.ToLower() == "name" && y?.Value?.ToLower()?.Trim() == targetName) == true);

            if(namedInstances?.Count() != 1)
                throw new Exception($"WARNING! More then one instance with name '{targetName}' were found, config will not be processed.");

            var instance = namedInstances?.FirstOrDefault();

            var tags = instance?.Tags?.ToDictionary(x => x.Key, y => y.Value) ?? new Dictionary<string, string>();
            var isRunning = (instance?.State.Code ?? -1) == (int)InstanceStateCode.running;
            var isStopped = (instance?.State.Code ?? -1) == (int)InstanceStateCode.stopped;
            var isTerminated = (instance?.State.Code ?? -1) == (int)InstanceStateCode.terminated;
            var isStateDefined = isRunning || isStopped || isTerminated;
            var on = cfg.on.IsNullOrEmpty() ? -2 : cfg.on.ToCron().Compare(DateTime.UtcNow);
            var off = cfg.off.IsNullOrEmpty() ? -2 : cfg.off.ToCron().Compare(DateTime.UtcNow);
            var kill = cfg.kill.IsNullOrEmpty() ? -2 : cfg.kill.ToCron().Compare(DateTime.UtcNow);

            if (off == 0 && on == 0)
                throw new Exception($"CRON On ({cfg?.on ?? "undefined"}) and Off ({cfg?.off ?? "undefined"}) of the {cfg?.name ?? "undefined"} instance are in invalid state, both are enabled, if possible set the cron not to overlap.");

            if(!isStateDefined)
                throw new Exception($"Instance state is undefined and can't be processed.");

            if (instance == null || isTerminated) //if instance was not found or is terminated
            {
                if ((on == 0 || off == 0) && //if is on or off
                    kill != 0 && //and not killed
                    !cfg.terminate) //and should not be terminated, then create
                {
                    Log($"{cfg?.name ?? "undefined"} => CREATING New Instance.");
                    instance = await CreateInstance(cfg);
                    Log($"{cfg?.name ?? "undefined"} => RESULT: New Instance {instance?.InstanceId} was created.");
                }
                return;
            }

            var forceKill = tags["redeploy"].ToIntOrDefault(0) < cfg.redeploy || cfg.terminate;
            var forceRestart = tags["restart"].ToIntOrDefault(0) < cfg.restart || cfg.shutdown;

            if (forceKill) // trigger instane kill
            {
                kill = 0;
                on = -2;
            }
            else if (forceRestart) //trigger instance halt
            {
                off = 0;
                on = -2;
            }

            if (isRunning && (off == 0 || kill == 0))
            {
                Log($"{cfg?.name ?? "undefined"} => STOPING Instance {instance.InstanceId} ({instance.GetTagValueOrDefault("Name")}), Cron: {cfg.off ?? "undefined"}");
                var result = await _EC2.StopInstance(instanceId: instance.InstanceId, force: false);
                Log($"{cfg?.name ?? "undefined"} => RESULT: Instance {instance.InstanceId} ({instance.GetTagValueOrDefault("Name")}), StateChange: {result.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");
                return;
            }
            else if (isStopped && (on == 0 && kill != 0))
            {
                Log($"{cfg?.name ?? "undefined"} => STARTING Instance {instance.InstanceId}, Cron: {cfg.on ?? "undefined"}");
                var result = await _EC2.StartInstance(instanceId: instance.InstanceId, additionalInfo: $"AWSLauncher Auto On, Cron: {cfg.on}");
                Log($"{cfg?.name ?? "undefined"} => RESULT: Instance {instance.InstanceId}, StateChange: {result.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");
                return;
            }
            else if (isStopped && kill == 0)
            {
                
                Log($"{cfg?.name ?? "undefined"} => TERMINATING Instance {instance.InstanceId} and removing Tag's, Cron: {cfg.off ?? "undefined"}");
                var wiper = await _EC2.DeleteAllInstanceTags(instanceId: instance.InstanceId);
                var result = await _EC2.TerminateInstance(instanceId: instance.InstanceId);
                Log($"{cfg?.name ?? "undefined"} => Instance {instance.InstanceId}, StateChange: {result.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");
                return;
            }

            if(!cfg.GetTags().CollectionEquals(tags)) //if tags changed
            {
                Log($"{cfg?.name ?? "undefined"} =>  UPDATING Instance Tags.");
                var tagUpdate = await UpdateTagsAsync(_EC2,instance.InstanceId, cfg.GetTags());
                Log($"{cfg?.name ?? "undefined"} =>  Instance Tags were {(tagUpdate ? "" : "NOT")} updated.");
            }
        }

        public async Task<bool> UpdateTagsAsync(EC2Helper ec2, string instanceId, Dictionary<string, string> tags, CancellationToken cancellationToken = default(CancellationToken))
        {
            var instance = await ec2.GetInstanceById(instanceId);
            var deleteTags = await ec2.DeleteAllInstanceTags(instanceId);

            if (tags.IsNullOrEmpty())
            {
                instance = await ec2.GetInstanceById(instanceId);
                return instance.Tags.IsNullOrEmpty();
            }

            var createTags = await ec2.CreateTagsAsync(
                resourceIds: new List<string>() { instanceId },
                tags: tags);

            instance = await ec2.GetInstanceById(instanceId);
            return instance.Tags.ToDictionary(x => x.Key, y => y.Value).CollectionEquals(tags, trim: true);
        }

        private async Task<Instance> CreateInstance(EC2InstanceConfig cfg)
        {
            var instanceType = cfg.instanceType.ToInstanceType();

            var result = await _EC2.CreateInstanceAsync(
                imageId: cfg.imageId,
                instanceType: instanceType,
                keyName: cfg.key,
                securityGroupIDs: cfg.securityGroups,
                subnetId: cfg.subnetId,
                roleName: cfg.roleName,
                shutdownBehavior: ShutdownBehavior.Stop,
                associatePublicIpAddress: cfg.publicIp,
                tags: cfg.GetTags(), 
                ebsOptymalized: cfg.ebsOptymalized,
                rootDeviceName: cfg.rootDeviceName,
                rootVolumeType: cfg.rootVolumeType,
                rootVolumeSize: cfg.rootVolumeSize,
                rootSnapshotId: cfg.rootSnapshotId,
                rootIOPS: cfg.rootIOPS);

            return result.Reservation.Instances.FirstOrDefault();
        }
    }
}