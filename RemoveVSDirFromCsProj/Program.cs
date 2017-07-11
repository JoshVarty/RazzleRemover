﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RemoveVSDirFromCsProj
{
    class Program
    {
        static void Main(string[] args)
        {
            string repoPath = @"C:\git\vsc";
            //var usingRemove = new PlatformUsingRemover();
            //usingRemove.FixUsings();
            //FindProjectUsing();
            
            //var externalReferencesRemover = new ExternalAPIRemover();
            //externalReferencesRemover.RemoveExternalAPIsAndPrintNuGets();

            var propertyRemover = new RazzlePropertyRemover(repoPath);
            propertyRemover.RemoveRazzleProperties();
        }
    }
}