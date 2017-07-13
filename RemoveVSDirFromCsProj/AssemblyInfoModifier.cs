using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveVSDirFromCsProj
{
    class AssemblyInfoModifier
    {

        internal static bool AddClsCompliant(string path)
        {
            var modified = false;
            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(path), "*AssemblyInfo*.cs"))
            {
                var fileContents = File.ReadAllText(file);
                if (fileContents.Contains(@"[assembly: AssemblyCulture ("""")]"))
                {
                    File.AppendAllText(file, @"[assembly: System.CLSCompliant(true)]
");
                    Console.WriteLine("Added to: " + file);
                    modified = true;
                }
            }
            return modified;
        }
    }
}
