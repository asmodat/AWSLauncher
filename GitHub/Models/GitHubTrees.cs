using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsmodatStandard.Extensions.IO;
using AsmodatStandard.Extensions;

namespace GITWrapper.GitHub.Models
{
    public static class GitHubTreeEx
    {
        public static GitHubObject[] GetObjectsByPath(this GitHubTree root, string path)
            => GetObjectsByPath(root?.tree, path);

        public static GitHubObject[] GetObjectsByPath(this IEnumerable<GitHubObject> objects, string path)
        
        {
            if (path.IsNullOrEmpty())
                return objects?.ToArray()?.DeepCopy();

            var nests = path.ToLinuxPath().Trim('/').Split('/');

            var objs = objects?.ToArray()?.DeepCopy();

            foreach (var nest in nests)
                objs = objs?.FirstOrDefault(
                    x => x.IsTree() && x.path.ToLower().Trim() == nest.ToLower().Trim())
                    ?.tree?.tree;

            return objs;
        }
    }

    public class GitHubTree
    {
        public string sha { get; set; }
        public string url { get; set; }
        public GitHubObject[] tree { get; set; }
        public bool truncated { get; set; }
    }
}
