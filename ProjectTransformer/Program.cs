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
            if (args.Length != 2) throw new ArgumentException("Provide source and destination csproj as argumewnts");
            var sourcePath = args[0];
            var destinationPath = args[1];
            var projectData = ProjectWorker.ProcessProject(sourcePath, destinationPath);
        }
    }
}
