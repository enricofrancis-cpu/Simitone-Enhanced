using System;
using System.IO;

namespace Simitone.Client.Utils
{
    public static class PreviewProfile
    {
        public static bool HasOverride(string[] args) => Array.Exists(args, x =>
            x.Equals("-profile", StringComparison.OrdinalIgnoreCase) ||
            x.StartsWith("-profile=", StringComparison.OrdinalIgnoreCase));

        public static string Resolve(string[] args, string defaultPath, string bundleDirectory = null)
        {
            if (bundleDirectory != null && File.Exists(Path.Combine(bundleDirectory, "superstar-preview.marker")))
                defaultPath = Path.GetFullPath(Path.Combine(bundleDirectory, "..", "TestProfile")).TrimEnd('\\', '/') + "/";
            string chosen = null;
            for (int i = 0; i < args.Length; i++)
            {
                string value = null;
                if (args[i].Equals("-profile", StringComparison.OrdinalIgnoreCase))
                {
                    if (++i >= args.Length) throw new ArgumentException("-profile needs an absolute directory.");
                    value = args[i];
                }
                else if (args[i].StartsWith("-profile=", StringComparison.OrdinalIgnoreCase))
                    value = args[i].Substring("-profile=".Length);
                if (value == null) continue;
                if (chosen != null) throw new ArgumentException("Specify -profile only once.");
                value = value.Trim('"');
                if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
                    throw new ArgumentException("-profile must be an absolute directory; refusing to use normal saves.");
                chosen = Path.GetFullPath(value);
            }
            return chosen == null ? defaultPath : chosen.TrimEnd('\\', '/') + "/";
        }
    }
}
