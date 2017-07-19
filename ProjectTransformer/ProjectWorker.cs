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

        static readonly Regex ExtractVersionFromHintPath = new Regex(@"[\\]([a-zA-Z]|[.])+((\d+[.]*)+)[\\]");

        internal static void ProcessProject(string sourcePath, string destinationPath)
        {
            Console.WriteLine($"Processing {sourcePath}");
            try
            {
                var data = new ProjectInfo();
                data = GetDataFromMSBuild(sourcePath, data);
                data = GetDataFromCSProj(sourcePath, data);

                var newProjectPath = WriteProject(data, destinationPath);
                Console.WriteLine($":) New project saved at {destinationPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($":( Error: {ex.Message}");
                throw;
            }
        }

        internal static void ProcessAllProjects(string solutionFolder)
        {
            if (!Directory.Exists(solutionFolder)) throw new DirectoryNotFoundException($"Directory {solutionFolder} does not exist");
            Console.WriteLine($"Processing all projects in {solutionFolder}");

            foreach (var project in Directory.EnumerateFiles(solutionFolder, "*.csproj", SearchOption.AllDirectories))
            {
                ProcessProject(project, project.Substring(0, project.Length - ".csproj".Length) + ".new.csproj");
            }
        }

        /// <summary>
        /// Analyze MSBuild output and store the data in a ProjectInfo instance
        /// </summary>
        /// <param name="sourcePath">Target csproj</param>
        /// <param name="data">ProjectInfo to augment</param>
        /// <returns>Augmented ProjectInfo</returns>
        private static ProjectInfo GetDataFromMSBuild(string sourcePath, ProjectInfo data)
        {
            var msbuildStream = GetRawData(sourcePath);
            data = ProcessStream(msbuildStream, data);
            return data;
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
                    var include = resource.Attribute(XName.Get("Include"))?.Value;
                    if (include == null) continue;
                    var generator = resource.GetValue("Generator");
                    var lastGenOutput = resource.GetValue("LastGenOutput");
                    data.ResourceFiles.Add(new ProjectInfo.EmbeddedResource
                    {
                        ResX = include,
                        Generator = generator,
                        LastGenOutput = lastGenOutput,
                    });
                }
            }/*
            foreach (var group in elements.Where(e => e.Name.LocalName == "PropertyGroup"))
            {
                var assemblyName = group.GetValue("AssemblyName");
                var rootNamespace = group.GetValue("RootNamespace");
                var noWarn = group.GetValue("NoWarn");

                if (assemblyName != null) data.AssemblyName = assemblyName;
                if (rootNamespace != null) data.RootNamespace = rootNamespace;
                if (noWarn != null) data.NoWarn = noWarn;
            }*/

            return data;
        }

        private static StreamReader GetRawData(string sourcePath)
        {
            if (!File.Exists(sourcePath)) throw new FileNotFoundException($"Project {sourcePath} does not exist");
            if (!File.Exists(MSBuildPath)) throw new FileNotFoundException($"Unable to find MSBuild. Please set a correct path in {nameof(ProjectWorker)}.cs");

            var startInfo = new ProcessStartInfo()
            {
                Arguments = $"{sourcePath} /t:Tool /v:m",
                FileName = MSBuildPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var msbuild = Process.Start(startInfo);
            return msbuild.StandardOutput;
        }

        private static ProjectInfo ProcessStream(StreamReader sr, ProjectInfo data)
        {
            string line;
            int lineNumber = 0;
            while (true)
            {
                line = sr.ReadLine()?.Trim();
                if (line == null) break;
                if (lineNumber++ < 3) continue; // Skip the first three lines
                if (line.Contains("error MSB4057")) throw new InvalidOperationException("Please run first pass of the tool on the target project and put Directory.Build.props in the target project's root or any parent directory");
                if (line.StartsWith("AssemblyName:"))
                {
                    var value = line.Substring("AssemblyName:".Length);
                    data.AssemblyName = value;
                }
                if (line.StartsWith("RootNamespace:"))
                {
                    var value = line.Substring("RootNamespace:".Length);
                    data.RootNamespace = value;
                }
                else if (line.StartsWith("NoWarn:"))
                {
                    var value = line.Substring("NoWarn:".Length);
                    data.NoWarn = value;
                }
                else if (line.StartsWith("ProjectReferences:"))
                {
                    var value = line.Substring("ProjectReferences:".Length);
                    data.ProjectReferences = value.Split(';');
                }
                else if (line.StartsWith("Reference:"))
                {
                    var value = line.Substring("Reference:".Length);
                    if (value.TrimEnd().EndsWith(", HintPath="))
                    {   // e.g: Reference:System;System.Core, HintPath=
                        // These are system references
                        value = value.Substring(0, value.Length-", HintPath=".Length);
                        data.SdkReferences = value.Split(';');
                    }
                    else
                    {   // e.g: Reference:Microsoft.VisualStudio.Validation, Version=15.3.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture = MSIL, HintPath =..\..\..\..\Packages\Microsoft.VisualStudio.Validation.15.3.15\lib\net45\Microsoft.VisualStudio.Validation.dll
                        var parts = value.Split(',');
                        var name = parts[0];
                        if (String.IsNullOrWhiteSpace(name)) throw new NotSupportedException("Reference has no name");
                        var hintPath = parts.Where(part => part.TrimStart().StartsWith("HintPath=")).FirstOrDefault();
                        if (hintPath == null) throw new NotSupportedException("Reference has no HintPath");
                        var match = ExtractVersionFromHintPath.Match(hintPath);
                        if (!match.Success) throw new NotSupportedException("Unable to get version from HintPath");
                        var version = match.Groups[2].Value;
                        data.NuGetReferences.Add(new ProjectInfo.ProjectReference
                        {
                            Name = name,
                            Version = version,
                        });
                    }
                }
            }
            return data;
        }

        private static object WriteProject(ProjectInfo projectData, string destinationPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(destinationPath))) throw new DirectoryNotFoundException($"Directory {Path.GetDirectoryName(destinationPath)} does not exist");
            if (String.IsNullOrEmpty(projectData.AssemblyName)) throw new InvalidOperationException($"Cannot create {destinationPath}: Project has no AssemblyName");

            var sb = new StringBuilder();
            sb.AppendLine($@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <AssemblyName>{projectData.AssemblyName}</AssemblyName>");
            if (!String.IsNullOrWhiteSpace(projectData.RootNamespace))
            {
                sb.AppendLine($"    <RootNamespace>{projectData.RootNamespace}</RootNamespace>");
            }
            sb.AppendLine($@"    <TargetFramework>net46</TargetFramework>
    <NoWarn>{projectData.NoWarn}</NoWarn>
  </PropertyGroup>");

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
                    sb.AppendLine($@"    <PackageReference Include=""{packageReference.Name}"" Version=""{packageReference.Version}"" />");
                }
                sb.AppendLine("  </ItemGroup>");
            }

            // ------------------------------------------- ProjectReferences
            if (projectData.ProjectReferences?.Any() == true)
            {
                sb.AppendLine("  <ItemGroup>");
                foreach (var projectReference in projectData.ProjectReferences)
                {
                    sb.AppendLine($@"    <Reference Include=""{projectReference}"" />");
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
                    sb.AppendLine($@"      <Generator>{resource.Generator}""</Generator>");
                    sb.AppendLine($@"      <LastGenOutput>{resource.LastGenOutput}""</LastGenOutput>");
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
                    sb.AppendLine($@"    <Compile Update=""{resource.LastGenOutput}"">");
                    sb.AppendLine($@"      <DesignTime>true</DesignTime>");
                    sb.AppendLine($@"      <AutoGen>true</AutoGen>");
                    sb.AppendLine($@"      <DependentUpon>{resource.ResX}""</DependentUpon>");
                    sb.AppendLine($@"    </EmbeddedResource>");
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