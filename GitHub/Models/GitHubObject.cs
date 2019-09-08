using System;
using System.Collections.Generic;
using System.Text;

namespace GITWrapper.GitHub.Models
{
    public class GitHubObject
    {
        public enum ObjectType
        {
            none = 0,
            blob = 1,
            tree = 1 << 1,
            any = blob | tree
        }

        public string sha { get; set; }
        public string path { get; set; }
        public string type { get; set; }
        public string mode { get; set; }
        public long size { get; set; }
        public string url { get; set; }

        public GitHubTree tree { get; set; }
        public GitHubObject[] objects { get => tree?.tree; }

        public ObjectType GetObjectType()
        {
            if (IsBlob())
                return ObjectType.blob;

            if (IsTree())
                return ObjectType.tree;

            return ObjectType.none;
        }

        public bool IsBlob()
        {
            if (type?.ToLower()?.Trim() == "blob")
                return true;

            return false;
        }

        public bool IsTree()
        {
            if (type?.ToLower()?.Trim() == "tree")
                return true;

            return false;
        }

    }
}
