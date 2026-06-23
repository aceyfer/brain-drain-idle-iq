using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using UnityEditor;

namespace Plankton.Editor
{
    public abstract class PlatformFiles
    {
        protected List<File> files;

        protected virtual void LoadFiles(BuildTarget platform)
        {
            var currPath = GetMyFilePath();
            var currDir = Path.GetDirectoryName(currPath).Replace("\\", "/") + $"/../{platform}/";
            foreach (var file in files)
            {
                if (string.IsNullOrEmpty(file.sourceFilename)) continue;
                var filename = currDir + file.sourceFilename;
                file.content = System.IO.File.ReadAllText(filename);
            }
        }

        public virtual void SaveFiles(bool generateAar)
        {
            foreach (var file in files)
            {
                UpdatePlaceholder(file, "[PLACEHOLDERS]", file.placeholders.ToList(), file.placeholderSeparator, file.placeholderFinalFunc);
                var filedir = Application.dataPath + Path.GetDirectoryName(file.destinationFilename);
                if (Directory.Exists(filedir) == false)
                    Directory.CreateDirectory(filedir);
                var filename = Application.dataPath + file.destinationFilename;
                System.IO.File.WriteAllText(filename, file.content);
                Debug.Log($"{file.destinationFilename} generated successfully!");
            }
        }

        public string GetMyFilePath([CallerFilePath] string callerFilePath = null)
        {
            return callerFilePath;
        }


        //////////////////////////////////////////////////////
        /// STATIC MEMBERS
        //////////////////////////////////////////////////////
        static protected void UpdatePlaceholder(File file, string placeholder, List<string> contents, string separator, Func<string, string> finalModification = null)
        {
            if (contents.Count > 0)
            {
                var items = string.Join(separator, contents.Distinct().ToList());
                var final = finalModification != null ? finalModification?.Invoke(items) : items;
                file.content = file.content.Replace(placeholder, final);
            }
            else file.content = file.content.Replace(placeholder, string.Empty);
        }


        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        public class File
        {
            public string sourceFilename = string.Empty;
            public string destinationFilename = string.Empty;
            public string content = string.Empty;
            public string placeholderSeparator = "\n";
            public List<string> placeholders = new List<string>();
            public Func<string, string> placeholderFinalFunc = item => item;
        }
    }
}