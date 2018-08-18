using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GoodieProvider
{
    public class AppConfigRemover
    {
        public void ProcessAllProjects(string sourcePath)
        {
            if (!Directory.Exists(sourcePath)) throw new DirectoryNotFoundException($"Directory {sourcePath} does not exist");
            var allProjects = Directory.EnumerateFiles(sourcePath, "*.csproj", SearchOption.AllDirectories);
            foreach (var project in allProjects)
            {
                RemoveAppConfig(project);
            }
        }

        private void RemoveAppConfig(string project)
        {
            var xe = XElement.Load(project);
            bool projectChanged = false;

            var elements = xe.Elements().Descendants().Where(n => n.Attributes().SingleOrDefault(a => a.Name.LocalName == "Include" && a.Value == "app.config") != null).ToList();
            foreach (var element in elements)
            {
                var parentGroup = element.Parent;
                if (parentGroup.Elements().Count() > 1)
                {
                    element.Remove();
                }
                else
                {
                    parentGroup.Remove();
                }
                projectChanged = true;
            }

            if (projectChanged)
            {
                Console.WriteLine("Removed app.config include from " + Path.GetFileName(project));
                xe.Save(project);
            }
            else
            {
                Console.WriteLine("Skipping " + Path.GetFileName(project));
            }
        }
    }
}