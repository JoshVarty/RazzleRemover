using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ProjectTransformer
{
    internal class ProjectInfo
    {
        public string AssemblyName { get; set; }
        public string RootNamespace { get; set; }
        public string NoWarn { get; set; }
        public string[] ProjectReferences { get; set; }
        public IList<ProjectReference> NuGetReferences { get; set; } = new List<ProjectReference>();
        public string[] SdkReferences { get; set; }
        public string[] ExternalReferences { get; set; }
        public IList<string> OtherFiles { get; set; } = new List<string>();
        public IList<EmbeddedResource> ResourceFiles { get; set; } = new List<EmbeddedResource>();
        public string[] CodeFiles { get; set; }

        public override string ToString() => AssemblyName;

        internal class ProjectReference
        {
            public string Name { get; set; }
            public string Version { get; set; }
        }

        internal class EmbeddedResource
        {
            public string ResX { get; set; }
            public string Generator { get; set; }
            public string LastGenOutput { get; set; }
        }
    }
}