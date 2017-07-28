using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            bool projectChanged = false;

            var references = xe.Elements().Descendants().Where(n => n.Name.LocalName == "PackageReference");
            foreach (var reference in references)
            {
                var name = reference.Attributes().Single(n => n.Name.LocalName == "Include").Value;
                var version = reference.Attributes().SingleOrDefault(n => n.Name.LocalName == "Version")?.Value;
                if (version == null)
                {
                    // There is no version attribute, perhaps there is a version element...
                    version = reference.Elements().SingleOrDefault(n => n.Name.LocalName == "Version")?.Value;
                    if (version == null)
                    {
                        Console.WriteLine($"Unable to get version for {name} in {Path.GetFileName(project)}");
                        continue;
                    } else
                    {
                        Console.WriteLine($"Fixing a malformed PackageReference for {name} in {Path.GetFileName(project)}");
                        projectChanged = true;
                        reference.Elements().Single(n => n.Name.LocalName == "Version").Remove();
                        reference.Add(new XAttribute("Version", version));
                    }
                }

                if (version.StartsWith("$("))
                {
                    // This is a msbuild variable. This project was already converted.
                    continue;
                }
                Console.WriteLine($"Converting PackageReference for {name} in {Path.GetFileName(project)}");

                if (PackageVersions.ContainsKey(name)
                    && PackageVersions[name].Contains(version))
                {
                    // Don't add duplicates
                }
                else
                {
                    PackageVersions.Add(name, version);
                }

                var propertyName = getPropertyNameForPackage(name, declaration: false);
                reference.Attributes().Single(n => n.Name.LocalName == "Version").SetValue(propertyName);
                projectChanged = true;
            }

            if (projectChanged)
            {
                xe.Save(project);
            }
        }


        private void UpdateAndSaveVersionProps(string sourcePath)
        {
            var versionsPropsPath = Path.Combine(sourcePath, "build", "versions.props");
            var directoryPath = Path.Combine(sourcePath, "build");

            // Load existing versions.props
            var existingVersions = LoadProps(versionsPropsPath);
            // Add newly discovered versions
            foreach (var discoveredVersion in PackageVersions)
            {
                var name = discoveredVersion.Key;
                var propertyName = getPropertyNameForPackage(name, declaration: true);
                foreach (var version in discoveredVersion.Value)
                {
                    existingVersions.Add(propertyName, version);
                }
            }
            // Save the combined versions.props
            var sb = new StringBuilder();
            sb.AppendLine($"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
            sb.AppendLine($"  <PropertyGroup>");
            foreach (var reference in existingVersions.OrderBy(n => n.Key))
            {
                var name = reference.Key;
                if (reference.Value.Count == 1)
                {
                    if (String.IsNullOrWhiteSpace(reference.Value.Single())) Console.WriteLine($"Warning: Package {name} doesn't specify a version.");
                    sb.AppendLine($"    <{name}>{reference.Value.Single()}</{name}>");
                }
                else
                {
                    foreach (var value in reference.Value)
                    {
                        Console.WriteLine($"Warning: Package {name} is referenced by multiple versions: {value}");
                        if (String.IsNullOrWhiteSpace(value)) Console.WriteLine($"Warning: Package {name} doesn't specify a version.");
                        sb.AppendLine($"    <{name}>{value}</{name}>");
                    }
                }
            }
            sb.AppendLine($"  </PropertyGroup>");
            sb.AppendLine($"</Project>");

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            File.WriteAllText(versionsPropsPath, sb.ToString());
        }

        private MultiValueDictionary<string, string> LoadProps(string propsPath)
        {
            var dictionary = new MultiValueDictionary<string, string>();
            if (!File.Exists(propsPath))
            {
                return dictionary;
            }
            var xe = XElement.Load(propsPath);
            var group = xe.Elements().Single(n => n.Name.LocalName == "PropertyGroup");
            var properties = group.Elements();
            foreach (var property in properties)
            {
                var name = property.Name.LocalName;
                var version = property.Value;
                dictionary.Add(name, version);
            }
            return dictionary;
        }


        /// <summary>
        /// Creates a MSBuild property for a given package name.
        /// Removes dots from the given package name.
        /// </summary>
        private string getPropertyNameForPackage(string name, bool declaration)
        {
            var processedName = name.Replace(".", "");
            return declaration ? processedName : $"$({processedName})";
        }
    }
}