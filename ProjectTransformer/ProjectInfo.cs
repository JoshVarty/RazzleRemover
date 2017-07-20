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
        public IList<string> ProjectReferences { get; } = new List<string>();
        public IList<ProjectReference> NuGetReferences { get; } = new List<ProjectReference>();
        public IList<string> SdkReferences { get; } = new List<string>();
        public IList<string> OtherFiles { get; } = new List<string>();
        public IList<EmbeddedResource> ResourceFiles { get; } = new List<EmbeddedResource>();
        public string[] CodeFiles { get; set; }
        public string AssemblyAttributeClsCompliant { get; set; }

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