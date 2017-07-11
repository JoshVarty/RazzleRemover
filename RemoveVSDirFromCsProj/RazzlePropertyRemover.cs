using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RazzleRemover
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
        private static List<string> StringToRemove { get; } =
            new List<string>()
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>",
                @"<Import Project=""$(BuildPropsFile)"" Condition=""'$(BuildProps_Imported)'!='True' AND Exists('$(BuildPropsFile)')"" />",
                @"<Import Project=""..\Platform.Settings.targets"" />",
                @"<Import Project=""$(PlatformPath)\Tools\Targets\Platform.Settings.Selector.targets"" />",
                "<!--Import the targets-->",
                @"<Import Project=""$(PlatformPath)\Tools\Targets\Platform.Imports.targets"" />",
                @"<BuildPropsFile>$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), Build.props))\Build.props</BuildPropsFile>",
                @"<Import Project=""$(PartitionExports)"" />",
                " KeepDuplicates=\"false\"",
            };

        private static List<string> PropertiesToRemove { get; } = new List<string>()
        {
            "GeneratedModuleId",
            "GeneratedModuleVersion",
            "GenerateAssemblyRefs",
            "CopyToSuiteBin",
            "UseVsVersion",
            "Nonshipping",
            "SignAssemblyAttribute",
            "OutputPath",
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
                    "<TargetFramework>net45</TargetFramework>\r\n    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\r\n    <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>"
                },
                {
                    "<TargetType>Library</TargetType>",
                    "<TargetFramework>net45</TargetFramework>\r\n    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\r\n    <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>"
                },
                {
                    "<HintPath>$(PkgTestPlatform_MSTest)\\v1\\lib\\net20\\Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll</HintPath>",
                    "<HintPath>$(RepoRoot)\\lib\\Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll</HintPath>"
                },
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
                || n.Contains(@"Platform\WER\"))).ToList().Take(5).ToList();
        }

        public void RemoveRazzleProperties()
        {
            foreach (var lineToRemove in StringToRemove)
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

            foreach(var propertyToRemove in PropertiesToRemove)
            {
                foreach (var projFile in ProjectPaths)
                {
                    var fileContents = File.ReadAllText(projFile);
                    if (fileContents.Contains(propertyToRemove))
                    {
                        //Replace contents
                        fileContents = removePropertyAndContents(fileContents, propertyToRemove);
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
        }

        /// <summary>
        /// Removes everything (including the property) between the start and end tag for a given property.
        /// For example, it will remove anything like: 
        ///         <GeneratedModuleId>Microsoft.VisualStudio.Language.Intellisense</GeneratedModuleId>
        /// as well as:
        ///         <GeneratedModuleId>Microsoft.VisualStudio.Text.Data</GeneratedModuleId>
        /// </summary>
        private string removePropertyAndContents(string fileContents, string property)
        {
            var startProperty = "<" + property + ">";
            var endProperty = "</" + property + ">";
            var start = fileContents.IndexOf(startProperty);
            var end = fileContents.IndexOf(endProperty) + endProperty.Length;

            while (start > -1 && end > -1 && end > start)
            {
                fileContents = fileContents.Remove(start, (end - start));
                start = fileContents.IndexOf(startProperty);
                end = fileContents.IndexOf(endProperty) + endProperty.Length;
            }

            return fileContents;
        }
    }
}
