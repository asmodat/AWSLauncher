using System.Collections.Generic;
using System.Linq;

namespace AWSLauncher.Models.Infrastructure
{
    public static class EC2InstanceConfigEx
    {
        public static Dictionary<string, string> GetTags(this EC2InstanceConfig cfg)
        {
            var t = cfg.tags.ToDictionary(x => x.Key, y => y.Value);

            t["Name"] = cfg.name ?? "";
            t["restart"] = cfg.restart.ToString();
            t["redeploy"] = cfg.redeploy.ToString();

            return t;
        }
    }

        /// <summary>
        /// Infrastructures as a Code configuration file
        /// </summary>
    public class EC2InstanceConfig
    {
        public string name { get; set; }

        /// <summary>
        /// Instance Type
        /// </summary>
        public string instanceType { get; set; }

        public string imageId { get; set; }

        public string instanceId { get; set; }
        public string subnetId { get; set; }
        public string roleName { get; set; }


        public string[] securityGroups { get; set; }

        public string key { get; set; }

        public KeyValuePair<string, string>[] tags { get; set; }

        /// <summary>
        /// defines wheather or not machine should be restarted
        /// </summary>
        public long restart { get; set; }

        /// <summary>
        /// defines wheather or not machine should be redeployed
        /// </summary>
        public long redeploy { get; set; }

        /// <summary>
        /// Refer to https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/device_naming.html
        /// /dev/xvda or /dev/sda1
        /// </summary>
        public string rootDeviceName { get; set; } = "/dev/sda1";
        public int rootVolumeSize { get; set; }
        public string rootVolumeType { get; set; } = "GP2";
        public string rootSnapshotId { get; set; }
        public int rootIOPS { get; set; } = 100;
        public bool enabled { get; set; }
        public bool publicIp { get; set; } = true;
        public bool ebsOptymalized { get; set; } = true;
        
        public bool terminate { get; set; } = false;
        public bool shutdown { get; set; } = false;

        public string on { get; set; } = "* * * * * *";
        public string off { get; set; } = null;
        public string kill { get; set; } = null;
    }
}
