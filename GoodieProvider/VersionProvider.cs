using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GoodieProvider
{
    public class VersionProvider
    {
        MultiValueDictionary<string, string> PackageVersions = new MultiValueDictionary<string, string>();

        public void ProcessAllProjects(string sourcePath)
        {
            if (!Directory.Exists(sourcePath)) throw new DirectoryNotFoundException($"Directory {sourcePath} does not exist");
            var allProjects = Directory.EnumerateFiles(sourcePath, "*.csproj", SearchOption.AllDirectories);
            foreach (var project in allProjects)
            {
                GetVersionsAndModifyProject(project);
            }
            UpdateAndSaveVersionProps(sourcePath);
        }

        private void GetVersionsAndModifyProject(string project)
        {
            var xe = XElement.Load(project);
            var references = xe.Elements().Descendants().Where(n => n.Name.LocalName == "PackageReference");
            foreach (var reference in references)
            {
                var name = reference.Attributes().Single(n => n.Name.LocalName == "Include").Value;
                var version = reference.Elements().SingleOrDefault(n => n.Name.LocalName == "Version")?.Value;
                if (version == null)
                {
                    Console.WriteLine($"Unable to get version for {name} in {Path.GetFileName(project)}");
                    continue;
                }
                if (version.StartsWith("$("))
                {
                    // This is a msbuild variable. This project was already converted.
                    continue;
                }
                PackageVersions.Add(name, version);
                var propertyName = getPropertyNameForPackage(name);
                reference.Elements().Single(n => n.Name.LocalName == "Version").SetValue(propertyName);
            }
            // TODO: Save xe
            var x = xe;
        }


        private void UpdateAndSaveVersionProps(string sourcePath)
        {
            // Load existing versions.props
            var existingVersions = LoadProps(Path.Combine(sourcePath, "build", "versions.props"));
            // Add newly discovered versions
            foreach (var discoveredVersion in PackageVersions)
            {
                var name = discoveredVersion.Key;
                var propertyName = getPropertyNameForPackage(name);
                foreach (var version in discoveredVersion.Value)
                {
                    existingVersions.Add(propertyName, version);
                }
            }
            // Save the combined versions.props

        }

        private MultiValueDictionary<string, string> LoadProps(string propsPath)
        {
            if (!File.Exists(propsPath))
            {
                return new MultiValueDictionary<string, string>();
            }
            return new MultiValueDictionary<string, string>(); // TODO. load props
        }


        /// <summary>
        /// Creates a MSBuild property for a given package name.
        /// Removes dots from the given package name.
        /// </summary>
        private string getPropertyNameForPackage(string name)
        {
            var processedName = name.Replace(".", "");
            return $"$({processedName})";
        }
    }
}