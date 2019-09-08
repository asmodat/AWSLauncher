using AsmodatStandard.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace GITWrapper.GitHub.Models
{
    public static class GitHubBlobEx
    {
        public static T JsonDeserialize<T>(this GitHubBlob blob)
        {
            if ((blob?.content).IsNullOrEmpty())
                return default(T);

            if (blob.encoding != "base64")
                throw new Exception($"Unknown blob encoding: '{blob.encoding ?? "undefined"}', url: '{blob.url ?? "undefined"}'");

            return blob.content.Base64Decode().JsonDeserialize<T>();
        }
    }

    public class GitHubBlob
    {
        public string sha { get; set; }
        public string node_id { get; set; }
        public long size { get; set; }
        public string url { get; set; }
        public string content { get; set; }
        public string encoding { get; set; }
    }
}
