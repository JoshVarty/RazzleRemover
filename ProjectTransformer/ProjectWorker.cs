using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace ProjectTransformer
{
    internal class ProjectWorker
    {
        const string MSBuildPath = @"C:\Program Files (x86)\Microsoft Visual Studio\gotoval\MSBuild\15.0\Bin\MSBuild.exe";

        static readonly Regex ExtractVersionFromHintPath = new Regex(@"[\\](\w|[.])*([a-zA-Z]|[.])+[.]((\d+[.]?)+([-]\w+)?)[\\]"); // Verify at https://regex101.com with test strings
        // "..\\..\\..\\..\\Packages\\Microsoft.Diagnostics.Tracing.EventSource.Redist.1.1.16-beta\\lib\\net45\\Microsoft.Diagnostics.Tracing.EventSource.dll"
        // "..\\..\\..\\..\\Packages\\Microsoft.VisualStudio.Imaging.Interop.14.0.DesignTime.15.0.25726-Preview5\\lib\\Microsoft.VisualStudio.Imaging.Interop.14.0.DesignTime.dll"
        // "..\..\..\..\Packages\Microsoft.VisualStudio.Threading.15.3.23\lib\net45\Microsoft.VisualStudio.Threading.dll"
        const int VersionGroupNumber = 3; // which capture group contains the version number

        internal static void ProcessProject(string sourcePath, string destinationPath, string solutionFolder = null)
        {
            Console.WriteLine($"Processing single project: {sourcePath}");
            try
            {
                var data = new ProjectInfo();
                data = GetDataFromCSProj(sourcePath, data);

                var newProjectPath = WriteProject(data, destinationPath, solutionFolder);
                Console.WriteLine($":) New project saved at {destinationPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($":( Error: {ex.Message}");
                throw;
            }
        }

        internal static void ProcessAllProjects(string solutionFolder, string destinationPath)
        {
            if (!Directory.Exists(solutionFolder)) throw new DirectoryNotFoundException($"Directory {solutionFolder} does not exist");
            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);

            Console.WriteLine($"Processing eligible projects in {solutionFolder}");

            var allProjects = Directory.EnumerateFiles(solutionFolder, "*.csproj", SearchOption.AllDirectories);
            var projects = allProjects.Where(n =>
                  (n.Contains(@"Platform\Core\")
                || n.Contains(@"Platform\Text\")
                || n.Contains(@"Platform\Language\")
                //|| n.Contains(@"Platform\F5DeployPlatform\")        //Going to do this one manually
                //|| n.Contains(@"Platform\Consolidated\")
                //|| n.Contains(@"Platform\Applications\")
                //|| n.Contains(@"Platform\Tools\")
                //|| n.Contains(@"Platform\MiniBuild\")
                //|| n.Contains(@"Platform\SKUs\")
                ));

            foreach (var project in projects)
            {
                ProcessProject(project, destinationPath + project.Substring(solutionFolder.Length), destinationPath);
            }
        }

        /// <summary>
        /// Parse csproj and store the data in a ProjectInfo instance
        /// </summary>
        /// <param name="sourcePath">Target csproj</param>
        /// <param name="data">ProjectInfo to augment</param>
        /// <returns>Augmented ProjectInfo</returns>
        private static ProjectInfo GetDataFromCSProj(string sourcePath, ProjectInfo data)
        {
            var xe = XElement.Load(sourcePath);
            var elements = xe.Elements().ToList();
            foreach (var group in elements.Where(e => e.Name.LocalName == "ItemGroup"))
            {
                foreach (var none in group.Elements().Where(e => e.Name.LocalName == "None"))
                {
                    var include = none.Attribute(XName.Get("Include"))?.Value;
                    if (include == null) continue;
                    if (include == "packages.config") continue; // ignore packages.config
                    // else, add this include
                    data.OtherFiles.Add(include);
                }
                foreach (var resource in group.Elements().Where(e => e.Name.LocalName == "EmbeddedResource"))
                {
                    var include = resource.GetAttribute("Include");
                    if (include == null) continue;
                    var generator = resource.GetValue("Generator");
                    var lastGenOutput = resource.GetValue("LastGenOutput");
                    var logicalName = resource.GetValue("LogicalName");
                    var manifestResourceName = resource.GetValue("ManifestResourceName");
                    var subType = resource.GetValue("SubType");
                    data.ResourceFiles.Add(new ProjectInfo.EmbeddedResource
                    {
                        ResX = include,
                        Generator = generator,
                        LastGenOutput = lastGenOutput,
                        LogicalName = logicalName,
                        ManifestResourceName = manifestResourceName,
                        SubType = subType
                    });
                }
                foreach (var projectReference in group.Elements().Where(e => e.Name.LocalName == "ProjectReference"))
                {
                    data.ProjectReferences.Add(projectReference.GetAttribute("Include"));
                }
                foreach (var reference in group.Elements().Where(e => e.Name.LocalName == "Reference"))
                {
                    var hintPath = reference.Elements().FirstOrDefault(e => e.Name.LocalName == "HintPath")?.Value;
                    if (String.IsNullOrEmpty(hintPath))
                    {
                        // this might be a .NET SDK reference
                        data.SdkReferences.Add(reference.GetAttribute("Include"));
                    }
                    else
                    {
                        var name = reference.GetAttribute("Include").Split(',')[0];
                        var match = ExtractVersionFromHintPath.Match(hintPath);
                        if (!match.Success)
                        {
                            // We were unable to find the version number.
                            // Proceed adding the reference without specifying the version.
                            data.NuGetReferences.Add(new ProjectInfo.ExternalReference
                            {
                                Name = name,
                            });
                        }
                        else
                        {
                            var version = match.Groups[VersionGroupNumber].Value;
                            if (!string.IsNullOrEmpty(version))
                            {
                                string[] versionParts = version.Split('.');
                                version = string.Join(".", versionParts.Skip(Math.Max(0, versionParts.Length - 3)));
                            }

                            data.NuGetReferences.Add(new ProjectInfo.ExternalReference
                            {
                                Name = name,
                                Version = version,
                            });
                        }
                    }
                }
            }
            foreach (var group in elements.Where(e => e.Name.LocalName == "PropertyGroup"))
            {
                var assemblyName = group.GetValue("AssemblyName");
                var rootNamespace = group.GetValue("RootNamespace");
                var shippingAssembly = group.GetValue("ShippingAssembly");
                var noWarn = group.GetValue("NoWarn");
                var AssemblyAttributeClsCompliant = group.GetValue("AssemblyAttributeClsCompliant");

                if (assemblyName != null) data.AssemblyName = assemblyName;
                if (rootNamespace != null) data.RootNamespace = rootNamespace;
                if (shippingAssembly != null) data.ShippingAssembly = shippingAssembly;
                if (noWarn != null) data.NoWarn = noWarn;
                if (AssemblyAttributeClsCompliant != null) data.AssemblyAttributeClsCompliant = AssemblyAttributeClsCompliant;
            }

            return data;
        }

        private static object WriteProject(ProjectInfo projectData, string destinationPath, string solutionFolder)
        {
            if (string.IsNullOrEmpty(solutionFolder))
            {
                solutionFolder = destinationPath;
            }

            solutionFolder += solutionFolder.EndsWith(@"\")? "": @"\";
            string pathToRoot = new Uri(destinationPath).MakeRelativeUri(new Uri(solutionFolder)).ToString().Replace('/', Path.DirectorySeparatorChar);

            if (!Directory.Exists(Path.GetDirectoryName(destinationPath))) throw new DirectoryNotFoundException($"Directory {Path.GetDirectoryName(destinationPath)} does not exist");

            if (String.IsNullOrEmpty(projectData.AssemblyName)) throw new InvalidOperationException($"Cannot create {destinationPath}: Project has no AssemblyName");

            var hostDirectory = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(hostDirectory)) Directory.CreateDirectory(hostDirectory);

            var sb = new StringBuilder();
            sb.AppendLine($@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <AssemblyName>{projectData.AssemblyName}</AssemblyName>
    <TargetFramework>net46</TargetFramework>");

            sb.AppendPropertyIfSet(projectData.RootNamespace, nameof(projectData.RootNamespace));
            sb.AppendPropertyIfSet(projectData.ShippingAssembly, nameof(projectData.ShippingAssembly));
            sb.AppendPropertyIfSet(projectData.NoWarn, nameof(projectData.NoWarn));
            sb.AppendPropertyIfSet(projectData.AssemblyAttributeClsCompliant, nameof(projectData.AssemblyAttributeClsCompliant));

            sb.AppendLine("  </PropertyGroup>");

            // ------------------------------------------- SdkReferences
            if (projectData.SdkReferences?.Any() == true)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var sdkReference in projectData.SdkReferences)
                {
                    sb.AppendLine($@"    <Reference Include=""{sdkReference}"" />");
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // ------------------------------------------- NuGetReferences
            if (projectData.NuGetReferences?.Any() == true)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var packageReference in projectData.NuGetReferences)
                {
                    if (packageReference.Name.Contains("Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions"))
                    {
                        //ignore it
                    }
                    else if (packageReference.Name.Contains("Microsoft.VisualStudio.QualityTools.UnitTestFramework") ||
                        packageReference.Name.Contains("Microsoft.VisualStudio.TestPlatform.TestFramework"))
                    {
                        sb.AppendLine(@"    <PackageReference Include=""MSTest.TestAdapter"" Version=""1.1.18"" />
    <PackageReference Include=""MSTest.TestFramework"" Version=""1.1.18"" /> ");
                    }
                    else if (packageReference.Name.Contains("Microsoft.VisualStudio.QualityTools.MockObjectFramework"))
                    {
                        sb.AppendLine($@"    <Reference Include=""Microsoft.VisualStudio.QualityTools.MockObjectFramework"">
      <HintPath>{pathToRoot}..\lib\MOF\Microsoft.VisualStudio.QualityTools.MockObjectFramework.dll</HintPath>
    </Reference> ");
                    }
                    else
                    {
                        sb.AppendLine($@"    <PackageReference Include=""{packageReference.Name}"" Version=""{packageReference.Version}"" />");
                    }
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // ------------------------------------------- ProjectReferences
            if (projectData.ProjectReferences?.Any() == true)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var projectReference in projectData.ProjectReferences)
                {
                    sb.AppendLine($@"    <ProjectReference Include=""{projectReference.Replace(@"$(PlatformPath)\", pathToRoot)}"" />");
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // ------------------------------------------- None
            if (projectData.OtherFiles?.Any() == true)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var otherFiles in projectData.OtherFiles)
                {
                    sb.AppendLine($@"    <None Include=""{otherFiles}"" />");
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // ------------------------------------------- EmbeddedResource
            if (projectData.ResourceFiles?.Any() == true)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var resource in projectData.ResourceFiles)
                {
                    sb.AppendLine($@"    <EmbeddedResource Update=""{resource.ResX}"">");
                    //if (!string.IsNullOrEmpty(resource.SubType))
                    //{
                    //    sb.AppendLine($@"      <SubType>{resource.SubType}</SubType>");
                    //}
                    if (!string.IsNullOrEmpty(resource.Generator))
                    {
                        sb.AppendLine($@"      <Generator>{resource.Generator}</Generator>");
                    }
                    if (!string.IsNullOrEmpty(resource.LastGenOutput))
                    {
                        sb.AppendLine($@"      <LastGenOutput>{resource.LastGenOutput}</LastGenOutput>");
                    }
                    if (!string.IsNullOrEmpty(resource.LogicalName))
                    {
                        sb.AppendLine($@"      <LogicalName>{resource.LogicalName}</LogicalName>");
                    }
                    if (!string.IsNullOrEmpty(resource.ManifestResourceName))
                    {
                        sb.AppendLine($@"      <ManifestResourceName>{resource.ManifestResourceName}</ManifestResourceName>");
                    }
                    sb.AppendLine($@"    </EmbeddedResource>");
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // ------------------------------------------- Files generated by EmbeddedResource
            if (projectData.ResourceFiles?.Any() == true)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var resource in projectData.ResourceFiles)
                {
                    // not all embedded resources are resx and generate files (e.g. cur files)
                    if (resource.ResX.EndsWith(".resx"))
                    {
                        sb.AppendLine($@"    <Compile Update=""{resource.LastGenOutput}"">");
                        sb.AppendLine($@"      <DesignTime>true</DesignTime>");
                        sb.AppendLine($@"      <AutoGen>true</AutoGen>");
                        sb.AppendLine($@"      <DependentUpon>{resource.ResX}</DependentUpon>");
                        sb.AppendLine($@"    </Compile>");
                    }
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // ------------------------------------------- Finish
            sb.AppendLine("</Project>");
            File.WriteAllText(destinationPath, sb.ToString());
            return destinationPath;
        }


        private static object getVersionProperty(string packageReference)
        {
            if (packageReference.StartsWith("Microsoft.VisualStudio."))
            {
                return "$(MicrosoftVSSDKVersion)";
            }
            else
            {
                return "$(" + packageReference.Replace(".", String.Empty) + ")";
            }
        }
    }
}