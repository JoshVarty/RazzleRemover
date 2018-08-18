using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTransformer
{
    class Program
    {
        // Alive: new[] {@"C:\git\vsc\src\Platform\Text\Def\TextData\TextData.csproj", @"C:\Users\amwieczo\Desktop\sample.csproj"}
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine(@"Usage:
first argument  - Directory with solution to derazzle
second argument - Output directory
Or:
first argument  - Project to derazzle
second argument - Destination path for converted project");
            }

            var sourcePath = args[0].Trim().TrimEnd('\\');
            var destinationPath = args[1].Trim().TrimEnd('\\');

            if (sourcePath.EndsWith(".csproj"))
            {
                ProjectWorker.ProcessProject(sourcePath, destinationPath);
            }
            else
            {
                ProjectWorker.ProcessAllProjects(sourcePath, destinationPath);
            }
        }
    }
}
