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
            foreach (var suiteBin in PropertiesAndAttributesToRemove)
            {
                foreach (var projFile in ProjectPaths)
                {
                    var fileContents = File.ReadAllText(projFile);
                    if (fileContents.Contains(suiteBin))
                    {
                        //Replace contents
                        fileContents = fileContents.Replace(suiteBin, "");
                        File.WriteAllText(projFile, fileContents);
                        Console.WriteLine("Removed from: " + projFile);
                    }
                }
            }
        }
    }
}
