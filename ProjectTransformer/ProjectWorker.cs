using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace ProjectTransformer
{
    internal class ProjectWorker
    {
        const string MSBuildPath = @"C:\Program Files (x86)\Microsoft Visual Studio\gotoval\MSBuild\15.0\Bin\MSBuild.exe";

        internal static object ProcessProject(string sourcePath, string destinationPath)
        {
            var msbuildStream = GetRawData(sourcePath);
            var projectData = ProcessData(msbuildStream);
            var newProjectPath = WriteProject(projectData, destinationPath);
            return projectData;
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

        private static ProjectInfo ProcessData(StreamReader sr)
        {
            var data = new ProjectInfo();
            string line;
            int lineNumber = 0;
            while (true)
            {
                line = sr.ReadLine()?.Trim();
                if (line == null) break;
                if (lineNumber++ < 3) continue; // Skip the first three lines
                if (line.StartsWith("AssemblyName:"))
                {
                    var value = line.Substring("AssemblyName:".Length);
                    data.AssemblyName = value;
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
                else if (line.StartsWith("References:"))
                {
                    var value = line.Substring("References:".Length);
                    data.NuGetReferences = value.Split(';');
                }
                else if (line.StartsWith("None:"))
                {
                    var value = line.Substring("None:".Length);
                    data.OtherFiles = value.Split(';');
                }
                else if (line.StartsWith("Resources:"))
                {
                    var value = line.Substring("Resources:".Length);
                    data.ResourceFiles = value.Split(';');
                }

            }
            return data;
        }

        private static object WriteProject(ProjectInfo projectData, string destinationPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(destinationPath))) throw new DirectoryNotFoundException($"Directory {Path.GetDirectoryName(destinationPath)} does not exist");

            var sb = new StringBuilder();
            sb.AppendLine($@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <AssemblyName>{projectData.AssemblyName}</AssemblyName>
    <TargetFramework>net46</TargetFramework>
    <NoWarn>{projectData.NoWarn}</NoWarn>
  </PropertyGroup>");

            sb.AppendLine("  <ItemGroup>");
            foreach (var packageReference in projectData.NuGetReferences)
            {
                sb.AppendLine($@"      <PackageReference Include=""{packageReference}"" Version=""{getVersionProperty(packageReference)}"" />");
            }
            sb.AppendLine("  </ItemGroup>");

            sb.AppendLine("  <ItemGroup>");
            foreach (var projectReference in projectData.ProjectReferences)
            {
                sb.AppendLine($@"      <Reference Include=""{projectReference}"" />");
            }
            sb.AppendLine("  </ItemGroup>");

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
                return packageReference.Replace(".", String.Empty);
            }
        }
    }
}