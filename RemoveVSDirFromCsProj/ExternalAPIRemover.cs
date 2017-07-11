using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RazzleRemover
{
    /// <summary>
    /// This class is meant to remove ExternalAPI references like: 
    ///         <Reference Include="$(ExternalAPIsPath)\Microsoft.VisualStudio.Threading\lib\net45\Microsoft.VisualStudio.Threading.dll" />
    ///         
    /// It's then supposed to output a NuGet command that lets us install these packages as proper NuGet packages
    /// </summary>
    class ExternalAPIRemover
    {
        private string PlatformPath { get; }

        private List<string> ProjectPaths { get; }

        /// <summary>
        /// Map from ExternalAPI reference to NuGet package
        /// </summary>
        private static List<Tuple<string, string>> ReferencesToReplaceWithNuGetPackages { get; } =
            new List<Tuple<string, string>>()
            {
                (new Tuple<string, string>("<Reference Include=\"$(ExternalAPIsPath)\\Microsoft.VisualStudio.Threading\\lib\\net45\\Microsoft.VisualStudio.Threading.dll\" />", "Microsoft.VisualStudio.Threading")),
                (new Tuple<string, string>("<Reference Include=\"$(ExternalAPIsPath)\\Microsoft.VisualStudio.Telemetry\\lib\\net45\\Microsoft.VisualStudio.Telemetry.dll\" />", "Microsoft.VisualStudio.Telemetry -Pre")),
                (new Tuple<string, string>("<Reference Include=\"$(ExternalAPIsPath)\\System.ValueTuple\\lib\\netstandard1.0\\System.ValueTuple.dll\" />", "System.ValueTuple")),
                (new Tuple<string, string>("<Reference Include=\"..\\..\\..\\..\\ExternalApis\\System.Collections.Immutable\\lib\\netstandard1.0\\System.Collections.Immutable.dll\" />", "System.ValueTuple")),
                (new Tuple<string, string>("<Reference Include=\"$(ExternalApisPath)\\System.Collections.Immutable\\lib\\netstandard1.0\\System.Collections.Immutable.dll\" />", "System.Collections.Immutable")),
            };

        public ExternalAPIRemover(string root = @"C:\git\VS")
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

        public void RemoveExternalAPIsAndPrintNuGets()
        {
            var nugetPackagesToProjects = RemoveReferencesAndFindNugets();
            PrintNugetInstallInstructions(nugetPackagesToProjects);
        }

        private Dictionary<string, HashSet<string>> RemoveReferencesAndFindNugets()
        {
            var lookup = new Dictionary<string, HashSet<string>>();

            foreach (var item in ReferencesToReplaceWithNuGetPackages)
            {
                var @using = item.Item1;
                var nugetPackage = item.Item2;
                if (!lookup.ContainsKey(nugetPackage))
                {
                    lookup[nugetPackage] = new HashSet<string>();
                }

                foreach (var projFile in ProjectPaths)
                {
                    var fileContents = File.ReadAllText(projFile);
                    if (fileContents.Contains(@using))
                    {
                        //Replace contents
                        fileContents = fileContents.Replace(@using, "");
                        File.WriteAllText(projFile, fileContents);
                        //Now remember that we have to install the NuGet package for this project
                        string projectName = Path.GetFileNameWithoutExtension(projFile);
                        lookup[nugetPackage].Add(projectName);
                        Console.WriteLine(projFile + " " + nugetPackage);
                    }
                }
            }

            return lookup;
        }

        /// <summary>
        /// Now we're going to create a bunch of install instructions for NuGet that look like:
        /// Get-Project <PROJECT-NAMES-WITH-COMMAS> | Install-Package <PACKAGENAME>
        /// </summary>
        /// <param name="nugetPackagesToProjects"></param>
        private void PrintNugetInstallInstructions(Dictionary<string, HashSet<string>> nugetPackagesToProjects)
        {
            Console.WriteLine();
            Console.WriteLine();

            foreach (var mapping in nugetPackagesToProjects)
            {
                var nugetPackage = mapping.Key;
                var projects = mapping.Value;

                string flatProjects = String.Join(", ", projects);

                //Only show NuGet script if there are any to install
                if (flatProjects.Any())
                {
                    string installScript = $"Get-Project { flatProjects }  | Install-Package { nugetPackage }";
                    Console.WriteLine(installScript);
                    Console.WriteLine();
                }
            }

            Console.WriteLine();
            Console.WriteLine("Install the NuGet packages to their correct projects using the above scripts.");
            Console.WriteLine("Press <ENTER> to continue.");
            Console.ReadLine();
        }
    }
}
