using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveVSDirFromCsProj
{

    /// <summary>
    /// This class is meant to remove the <Using____>true</Using____> references that were used in Razzle. 
    /// 
    /// It's then supposed to output a NuGet command that lets us install these references as proper NuGet packages
    /// </summary>
    public class PlatformUsingRemover
    {
        private string PlatformPath { get; }

        private List<string> ProjectPaths { get; }

        /// <summary>
        /// Map from <Using____> to NuGet package
        /// </summary>
        private static List<Tuple<string, string>> UsingsToReplaceWithNuGetPackages { get; } =
            new List<Tuple<string, string>>()
            {
                (new Tuple<string, string>("<UsingProjectPlatformImaging>true</UsingProjectPlatformImaging>", "Microsoft.VisualStudio.Imaging")),
                (new Tuple<string, string>("<UsingProjectPlatformUtilities>true</UsingProjectPlatformUtilities>", "Microsoft.VisualStudio.Utilities")),
                (new Tuple<string, string>("<UsingProjectVscommonVsImages>true</UsingProjectVscommonVsImages>", "Microsoft.VisualStudio.ImageCatalog")),
                //Note that we're installing -Pre for NuGet
                (new Tuple<string, string>("<UsingProjectVSCommonMsVsShellInterop140>true</UsingProjectVSCommonMsVsShellInterop140>", "Microsoft.VisualStudio.Shell.Interop.14.0.DesignTime -Pre")),
                //Note that we're installing -Pre for NuGet
                (new Tuple<string, string>("<UsingProjectVSCommonImageParametersInterop140_EmbedInteropTypes>true</UsingProjectVSCommonImageParametersInterop140_EmbedInteropTypes>", "Microsoft.VisualStudio.Imaging.Interop.14.0.DesignTime -Pre")),
            };

        /// <summary>
        /// The unit test using must be handled differently. We cannot just replace with  MSTest.TestFramework because we also have to include System.Runtime
        /// </summary>
        private static Tuple<string, string> UsingTestAndMSTestNuget { get; } = new Tuple<string, string>("<UsingProjectUnitTestUnittestframework>true</UsingProjectUnitTestUnittestframework>", "MSTest.TestFramework");

        private static List<string> UsingsToRemoveCompletely { get; } =
            //These usings have no replacements. In particular, the partion usings are just 
            //used to set context for other usings.
            new List<string>()
            {
                "<UsingPartitionPlatform>true</UsingPartitionPlatform>",
                "<UsingPartitionVscommon>true</UsingPartitionVscommon>",
                "<UsingPartitionUnittest>true</UsingPartitionUnittest>",
                "<UsingPartitionVsdata>true</UsingPartitionVsdata>",
                "<UsingPartitionUnitTest>true</UsingPartitionUnitTest>",
                //All of these are followed by the same reference with EmbedInteropTypes True
                //So we're going to remove all these, and only include the one to embed interop types
                "<UsingProjectVSCommonImageParametersInterop140>true</UsingProjectVSCommonImageParametersInterop140>",
            };

        public PlatformUsingRemover(string root = @"C:\git\VS")
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

        public void FixUsings()
        {
            //RemoveUsingProjectVSCommonImageParametersInterop140();
            //var nugetPackagesToProjects = RemoveUsingsAndFindNugets();
            //PrintNugetInstallInstructions(nugetPackagesToProjects);

            //nugetPackagesToProjects = RemoveUnitTestUsingsAndFindNugets();
            //GetNugetInstallInstructions(nugetPackagesToProjects);
            
            //ReplaceMockFramework();
            //AddCLSComplianceSuppressionToTestProjects();

            RemoveUsingPartions();
        }

        /// <summary>
        /// Some usings we just want to remove completely and not substitute anything for.
        /// For example UsingProjectVSCommonImageParametersInterop140 because it's always accompanied by UsingProjectVSCommonImageParametersInterop140_EmbedInterop
        /// </summary>
        private void RemoveUsingProjectVSCommonImageParametersInterop140()
        {
            string @using = "<UsingProjectVSCommonImageParametersInterop140>true</UsingProjectVSCommonImageParametersInterop140>";
            foreach (var projFile in ProjectPaths)
            {
                var fileContents = File.ReadAllText(projFile);
                if (fileContents.Contains(@using))
                {
                    //Replace contents
                    fileContents = fileContents.Replace(@using, "");
                    File.WriteAllText(projFile, fileContents);
                }
            }
        }

        private void RemoveUsingPartions()
        {
            var usings = new List<string>()
            {
                "<UsingPartitionUnittest>true</UsingPartitionUnittest>",
                "<UsingPartitionVsdata>true</UsingPartitionVsdata>",
                "<UsingPartitionUnitTest>true</UsingPartitionUnitTest>",
                "<UsingPartitionPlatform>true</UsingPartitionPlatform>",
                "<UsingPartitionVscommon>true</UsingPartitionVscommon>",
            };

            foreach(var @using in usings)
            {
                //We're gonna have to do these two projects manually.
                foreach (var projFile in ProjectPaths.Where(n => !n.EndsWith("VSEditor.csproj") && !n.EndsWith("VSEditorUnitTests.csproj")))
                {
                    var fileContents = File.ReadAllText(projFile);
                    if (fileContents.Contains(@using))
                    {
                        //Replace contents
                        fileContents = fileContents.Replace(@using, "");
                        File.WriteAllText(projFile, fileContents);
                    }
                }
            }
        }



        private Dictionary<string, HashSet<string>> RemoveUsingsAndFindNugets()
        {
            var lookup = new Dictionary<string, HashSet<string>>();

            foreach (var item in UsingsToReplaceWithNuGetPackages)
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

        private Dictionary<string, HashSet<string>> RemoveUnitTestUsingsAndFindNugets()
        {
            var lookup = new Dictionary<string, HashSet<string>>();
            var unitTestUsing = UsingTestAndMSTestNuget.Item1;
            var nugetPackage = UsingTestAndMSTestNuget.Item2;
            lookup[nugetPackage] = new HashSet<string>();

            foreach (var projFile in ProjectPaths)
            {
                var fileContents = File.ReadAllText(projFile);
                if (fileContents.Contains(unitTestUsing))
                {
                    //Remove using
                    fileContents = fileContents.Replace(unitTestUsing, "");
                    //Add reference to System.Runtime
                    string searchString =
@"<Reference Include=""System"" />";
                    fileContents = fileContents.Replace(searchString,
@"<Reference Include=""System"" />
    <Reference Include=""System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" />");

                    File.WriteAllText(projFile, fileContents);
                    string projectName = Path.GetFileNameWithoutExtension(projFile);
                    lookup[nugetPackage].Add(projectName);
                    Console.WriteLine(projFile);
                }
            }

            return lookup;
        }

        private void ReplaceMockFramework()
        {
            string mockLibraryUsing = "<UsingProjectVsdataMicrosoftVisualStudioQualityToolsMockObjectFramework>true</UsingProjectVsdataMicrosoftVisualStudioQualityToolsMockObjectFramework>";

            foreach (var projFile in ProjectPaths)
            {
                var fileContents = File.ReadAllText(projFile);
                if (fileContents.Contains(mockLibraryUsing))
                {
                    //Remove using
                    fileContents = fileContents.Replace(mockLibraryUsing, "");
                    //Add reference to Mock Library in /libs folder
                    string searchString =
@"<Reference Include=""System"" />";
                    fileContents = fileContents.Replace(searchString,
@"<Reference Include=""Microsoft.VisualStudio.QualityTools.MockObjectFramework, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\..\..\libs\Microsoft.VisualStudio.QualityTools.MockObjectFramework.dll</HintPath>
    </Reference>
    <Reference Include=""System"" />");

                    File.WriteAllText(projFile, fileContents);
                    Console.WriteLine(projFile);
                }
            }
        }

        private void AddCLSComplianceSuppressionToTestProjects()
        {
            string searchString =
@"$(NoWarn)</NoWarn>";

            var testProjects = ProjectPaths.Where(n => Path.GetFileNameWithoutExtension(n).Contains("Test"));
            foreach (var projFile in testProjects)
            {
                var fileContents = File.ReadAllText(projFile);

                if (fileContents.Contains(searchString))
                {
                    fileContents = fileContents.Replace(searchString,
@";3001;$(NoWarn)</NoWarn>");
                    File.WriteAllText(projFile, fileContents);
                    Console.WriteLine(projFile);
                }
            }
        }
    }
}
