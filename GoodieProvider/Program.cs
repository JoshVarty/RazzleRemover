using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoodieProvider
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(@"Please provide the path to the repository in first argument.");
            }

            var sourcePath = args[0].Trim().TrimEnd('\\');

            //new VersionProvider().ProcessAllProjects(sourcePath);
            new AppConfigRemover().ProcessAllProjects(sourcePath);

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
