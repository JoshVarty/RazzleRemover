using System;
using System.Collections.Generic;

namespace ProjectTransformer
{
    internal class ProjectInfo
    {
        public string AssemblyName { get; set; }
        public string NoWarn { get; set; }
        public string[] ProjectReferences { get; set; }
        public IList<ProjectReference> NuGetReferences { get; set; } = new List<ProjectReference>();
        public string[] SdkReferences { get; set; }
        public string[] ExternalReferences { get; set; }
        public string[] OtherFiles { get; set; }
        public string[] CodeFiles { get; set; }
        public string[] ResourceFiles { get; set; }

        public override string ToString() => AssemblyName;

        internal class ProjectReference
        {
            public string Name { get; set; }
            public string Version { get; set; }
        }

    }
}