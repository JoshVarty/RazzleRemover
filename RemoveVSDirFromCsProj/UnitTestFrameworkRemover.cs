using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazzleRemover
{
    /// <summary>
    /// Replaces a unit test reference found in PkgTestPlatform_MSTest with a proper NuGet reference.
    /// It also adds a required reference to System.Runtime.
    /// </summary>
    public class UnitTestFrameworkRemover
    {
        private string PlatformPath { get; }

        private List<string> ProjectPaths { get; }

        private static string UnitTestReference { get; } =
@"<Reference Include=""Microsoft.VisualStudio.QualityTools.UnitTestFramework"">
      <Private>true</Private>
      <HintPath>$(PkgTestPlatform_MSTest)\v1\lib\net20\Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll</HintPath>
    </Reference>";
        private static string UnitTestNuGet = "MSTest.TestFramework";


        public UnitTestFrameworkRemover(string root = @"C:\git\VS")
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

        public void RemoveUnitTestFrameworkReferencesAndPrintNuGetScript()
        {
            var lookup = RemoveUnitTestUsingsAndFindNugets();
            PrintNugetInstallInstructions(lookup);
        }


        private Dictionary<string, HashSet<string>> RemoveUnitTestUsingsAndFindNugets()
        {
            var lookup = new Dictionary<string, HashSet<string>>();
            lookup[UnitTestNuGet] = new HashSet<string>();

            foreach (var projFile in ProjectPaths)
            {
                var fileContents = File.ReadAllText(projFile);
                if (fileContents.Contains(UnitTestReference))
                {
                    //Remove using
                    fileContents = fileContents.Replace(UnitTestReference, "");
                    //Add reference to System.Runtime
                    string searchString =
@"<Reference Include=""System"" />";
                    fileContents = fileContents.Replace(searchString,
@"<Reference Include=""System"" />
    <Reference Include=""System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" />");

                    File.WriteAllText(projFile, fileContents);
                    string projectName = Path.GetFileNameWithoutExtension(projFile);
                    lookup[UnitTestNuGet].Add(projectName);
                    Console.WriteLine(projFile);
                }
            }

            return lookup;
        }

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
