using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RemoveVSDirFromCsProj
{
    /// <summary>
    /// This class is meant to remove <CopyToSuiteBin>true</CopyToSuiteBin> MSBuild properties like:
    ///      <PropertyGroup>
    ///          <CopyToSuiteBin>true</CopyToSuiteBin>
    ///      </PropertyGroup>
    /// </summary>
    internal class RazzlePropertyRemover
    {
        private string PlatformPath { get; }

        private List<string> ProjectPaths { get; }

        /// <summary>
        /// These are a bunch of Razzle related properties and attributes I want to remove entirely.
        /// </summary>
        private static List<string> PropertiesAndAttributesToRemove { get; } =
            new List<string>()
            {
                "<CopyToSuiteBin>true</CopyToSuiteBin>",
                "<CopyToSuiteBin>false</CopyToSuiteBin>",
                "<UseVsVersion>true</UseVsVersion>",
                " KeepDuplicates=\"false\"",
                "<Nonshipping>true</Nonshipping>",
                "<Nonshipping>false</Nonshipping>",
                "<GenerateAssemblyRefs>true</GenerateAssemblyRefs>",
                "<SignAssemblyAttribute>true</SignAssemblyAttribute>",
                @"<?xml version=""1.0"" encoding=""utf-8""?>",
                @"<Import Project=""$(BuildPropsFile)"" Condition=""'$(BuildProps_Imported)'!='True' AND Exists('$(BuildPropsFile)')"" />",
                @"<Import Project=""..\Platform.Settings.targets"" />",
                @"<Import Project=""$(PlatformPath)\Tools\Targets\Platform.Settings.Selector.targets"" />",
                "<!--Import the targets-->",
                @"<Import Project=""$(PlatformPath)\Tools\Targets\Platform.Imports.targets"" />",
                @"<BuildPropsFile>$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), Build.props))\Build.props</BuildPropsFile>",
                @"<OutputPath>$(BinariesDirectory)\bin\$(BuildArchitecture)</OutputPath>",
                @"<Import Project=""$(PartitionExports)"" />",
                @"<GeneratedModuleVersion>15.0.0</GeneratedModuleVersion>",
                @"<AssemblyAttributeClsCompliant>false</AssemblyAttributeClsCompliant>",
            };

        private static Dictionary<string, Func<string, bool>> PropertiesToRemoveAndAct { get; } =
            new Dictionary<string, Func<string, bool>>()
            {
                {
                    @"<AssemblyAttributeClsCompliant>true</AssemblyAttributeClsCompliant>",
                    (path) => AssemblyInfoModifier.AddClsCompliant(path)
                }
            };

        private static Dictionary<string, string> StringsToReplace { get; } =
            new Dictionary<string, string>()
            {
                {
                    @"<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">",
                    @"<Project Sdk=""Microsoft.NET.Sdk"">"
                },
                {
                    "<OutputType>Library</OutputType>",
                    "<TargetFramework>net45</TargetFramework>"
                },
                {
                    "<TargetType>Library</TargetType>",
                    "<TargetFramework>net45</TargetFramework>"
                }
            };

        public RazzlePropertyRemover(string root = @"C:\git\VS")
        {
            PlatformPath = Path.Combine(root, @"src\Platform");

            //Only find the .csproj's we're interested in.
            var csprojs = Directory.GetFiles(PlatformPath, "*.csproj", SearchOption.AllDirectories);
            ProjectPaths = csprojs.Where(n =>
                !(n.Contains(@"Platform\Applications\")
                || n.Contains(@"Consolidated\CFEditor\")
                || n.Contains(@"Platform\ExtensibilityHosting\")
                || n.Contains(@"Platform\F5DeployPlatform\")        //Going to do this one manually
                || n.Contains(@"Platform\Imaging\")
                || n.Contains(@"Platform\Tools\")
                || n.Contains(@"Platform\UserNotifications\")
                || n.Contains(@"Platform\Utilities\")
                || n.Contains(@"Platform\WER\"))).ToList();
        }

        public void RemoveRazzleProperties()
        {
            foreach (var lineToRemove in PropertiesAndAttributesToRemove)
            {
                foreach (var projFile in ProjectPaths)
                {
                    var fileContents = File.ReadAllText(projFile);
                    if (fileContents.Contains(lineToRemove))
                    {
                        //Replace contents
                        fileContents = fileContents.Replace(lineToRemove, "");
                        File.WriteAllText(projFile, fileContents);
                        Console.WriteLine("Removed from: " + projFile);
                    }
                }
            }

            foreach (var lineToReplace in StringsToReplace)
            {
                foreach (var projFile in ProjectPaths)
                {
                    var fileContents = File.ReadAllText(projFile);
                    if (fileContents.Contains(lineToReplace.Key))
                    {
                        //Replace contents
                        fileContents = fileContents.Replace(lineToReplace.Key, lineToReplace.Value);
                        File.WriteAllText(projFile, fileContents);
                        Console.WriteLine("Replaced in: " + projFile);
                    }
                }
            }

            foreach (var lineToRemoveAndAct in PropertiesToRemoveAndAct)
            {
                foreach (var projFile in ProjectPaths)
                {
                    var fileContents = File.ReadAllText(projFile);
                    if (fileContents.Contains(lineToRemoveAndAct.Key))
                    {
                        //Replace contents
                        fileContents = fileContents.Replace(lineToRemoveAndAct.Key, "");
                        File.WriteAllText(projFile, fileContents);
                        var modified = lineToRemoveAndAct.Value(projFile);
                        Console.WriteLine("Removed from and " + (modified ? "modified: " : "not modified: ") + projFile);
                    }
                }
            }
        }
    }
}
