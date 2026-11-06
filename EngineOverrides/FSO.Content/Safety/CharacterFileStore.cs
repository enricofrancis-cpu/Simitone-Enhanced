// SPDX-License-Identifier: MPL-2.0
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FSO.Content.TS1.EnhancedSafety
{
    public static class CharacterFileStore
    {
        private const int MaxIndex = 99999;

        public static string[] CharacterPaths(string directory) => Directory.EnumerateFiles(directory)
            .Where(path => string.Equals(Path.GetExtension(path), ".iff", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();

        public static int NextIndex(IEnumerable<string> paths)
        {
            var occupied = new HashSet<int>();
            foreach (var path in paths)
            {
                var name = Path.GetFileName(path);
                if (name.Length == 13 && name.StartsWith("User", StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(".iff", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(name.AsSpan(4, 5), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
                    occupied.Add(id);
            }
            var next = occupied.Count == 0 ? 0 : occupied.Max() + 1;
            if (next <= MaxIndex) return next;
            for (next = 0; next <= MaxIndex; next++) if (!occupied.Contains(next)) return next;
            throw new IOException("No unused five-digit character filename is available.");
        }

        public static string WriteNew(string directory, ref int nextIndex, Action<Stream> serialize)
        {
            if (serialize == null) throw new ArgumentNullException(nameof(serialize));
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(directory, ".simitone-character-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    serialize(file);
                    file.Flush(true);
                }
                var occupied = new HashSet<string>(Directory.EnumerateFileSystemEntries(directory)
                    .Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);
                var candidate = nextIndex >= 0 && nextIndex <= MaxIndex ? nextIndex : NextIndex(Directory.EnumerateFileSystemEntries(directory));
                for (var attempts = 0; attempts <= MaxIndex; attempts++, candidate = (candidate + 1) % (MaxIndex + 1))
                {
                    var name = "User" + candidate.ToString("D5", CultureInfo.InvariantCulture) + ".iff";
                    if (occupied.Contains(name)) continue;
                    var path = Path.Combine(directory, name);
                    try
                    {
                        File.Move(temporary, path, false); // Never replace an existing character.
                        nextIndex = candidate + 1;
                        return path;
                    }
                    catch (IOException) when (File.Exists(path) || Directory.Exists(path))
                    {
                        occupied.Add(name); // Another writer won this name; retry without overwriting it.
                    }
                }
                throw new IOException("No unused five-digit character filename is available.");
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
