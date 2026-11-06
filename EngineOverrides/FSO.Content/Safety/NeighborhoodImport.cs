// SPDX-License-Identifier: MPL-2.0
using System;
using System.IO;

namespace FSO.Content.TS1.EnhancedSafety
{
    public static class NeighborhoodImport
    {
        private static readonly string[] RequiredFiles = {
            "Neighborhood.iff", "LotLocations.iff", "LotZoning.iff", "StreetNames.iff",
            "Houses/NeighborhoodDesc.iff", "Houses/STDesc.iff", "Houses/MTDesc.iff"
        };

        private static void Validate(string directory)
        {
            foreach (var relative in RequiredFiles)
            {
                var path = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new InvalidDataException("The copied neighbourhood is incomplete: " + relative +
                        ". Existing files have been preserved; use a fresh disposable profile or restore its backup.");
            }
            if (!Directory.Exists(Path.Combine(directory, "Characters")))
                throw new InvalidDataException("The copied neighbourhood is missing its Characters directory. Existing files have been preserved.");
        }

        public static void EnsureCopied(string source, string destination, Action<string, string> copyFile = null)
        {
            source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
            destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination));
            var separator = Path.DirectorySeparatorChar.ToString();
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase) ||
                destination.StartsWith(source + separator, StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith(destination + separator, StringComparison.OrdinalIgnoreCase))
                throw new IOException("The original and copied neighbourhood folders must be separate.");

            if (Directory.Exists(destination)) { Validate(destination); return; }
            if (File.Exists(destination)) throw new IOException("A file blocks the copied neighbourhood folder.");
            Validate(source); // Reject incomplete source data before creating any import files.
            var parent = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(parent);
            var staging = Path.Combine(parent, ".simitone-import-" + Path.GetFileName(destination) + "-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(Path.Combine(staging, Path.GetRelativePath(source, directory)));
                foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
                {
                    var target = Path.Combine(staging, Path.GetRelativePath(source, file));
                    if (copyFile != null) copyFile(file, target);
                    else File.Copy(file, target, false);
                    File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
                }
                Validate(staging);
                Directory.Move(staging, destination); // Publish only a structurally complete copy; never merge into existing saves.
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }
    }
}
