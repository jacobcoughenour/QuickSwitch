using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickSwitch
{
    public class SwitchLogic
    {
        // todo make this configurable in the options
        public static string[] SpecialSuffixes = new string[] { "Model" };

        /// <summary>
        /// Removes the extensions and special suffixes from the file name.
        /// This is pretty much the main logic of the whole extension.
        /// </summary>
        public static string NormalizeFileName(string fileName)
        {
            var withoutExtensions = fileName.Substring(0, fileName.IndexOf('.'));
            foreach (var suffix in SpecialSuffixes)
                if (withoutExtensions.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return withoutExtensions.Substring(0, withoutExtensions.Length - suffix.Length);
            return withoutExtensions;
        }

        /// <summary>
        /// Gets all the files that match the same normalized file name as the current file.
        /// </summary>
        public static IEnumerable<string> GetMatchingFiles(string currentFilePath)
        {
            var parentDirectory = Path.GetDirectoryName(currentFilePath);
            var currentFileName = Path.GetFileName(currentFilePath);
            var normalizedFileName = NormalizeFileName(currentFileName);
            return Directory.GetFiles(parentDirectory)
                .Where(file => NormalizeFileName(Path.GetFileName(file)) == normalizedFileName);
        }
    }
}
