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
            if (args.Length == 1)   // Process all csproj in this directory
            {
                var solutionFolder = args[0];
                ProjectWorker.ProcessAllProjects(solutionFolder);
            }
            else if (args.Length == 2)
            {
                var sourcePath = args[0];
                var destinationPath = args[1];
                ProjectWorker.ProcessProject(sourcePath, destinationPath);
            }
            else throw new ArgumentException("Provide either solution directory or source and destination csproj as arguments");
        }
    }
}
