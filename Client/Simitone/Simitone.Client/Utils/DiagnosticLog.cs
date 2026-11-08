using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Simitone.Client.Utils
{
    // Best-effort diagnostics: no game resources, commands, or save data are serialized here.
    public static class DiagnosticLog
    {
        public const int FileLimit = 2 * 1024 * 1024;
        private static readonly object Gate = new object();
        private static readonly List<KeyValuePair<string, string>> PrivatePaths = new List<KeyValuePair<string, string>>();
        private static string Session;
        public static string SessionDirectory { get { lock (Gate) return Session; } }

        public static void Start(string profile, string build)
        {
            lock (Gate)
            {
                Session = null;
                PrivatePaths.Clear();
                AddPrivatePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "<user>");
                AddPrivatePath(profile, "<profile>");
                try
                {
                    var root = Path.Combine(profile, "Diagnostics");
                    Directory.CreateDirectory(root);
                    // Retain only directories created by this logger, with an explicit ownership marker.
                    foreach (var old in Directory.GetDirectories(root, "session-*")
                        .Where(x => (File.GetAttributes(x) & FileAttributes.ReparsePoint) == 0 && File.Exists(Path.Combine(x, ".simitone-diagnostics")))
                        .OrderByDescending(x => Path.GetFileName(x), StringComparer.Ordinal).Skip(4))
                    {
                        try { Directory.Delete(old, true); } catch (Exception) { }
                    }
                    Session = Path.Combine(root, "session-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(Session);
                    File.WriteAllText(Path.Combine(Session, ".simitone-diagnostics"), "1");
                    Event("session-start", "build=" + build + "; client=" + typeof(DiagnosticLog).Assembly.GetName().Version +
                        "; runtime=" + RuntimeInformation.FrameworkDescription + "; os=" + RuntimeInformation.OSDescription +
                        "; architecture=" + RuntimeInformation.ProcessArchitecture);
                }
                catch (Exception) { Session = null; }
            }
        }

        public static string ReadBuild(string directory)
        {
            try { return File.ReadAllText(Path.Combine(directory, "preview-build.txt")).Trim(); }
            catch (Exception) { return "local/unidentified"; }
        }

        public static void AddPrivatePath(string path, string replacement)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.TrimEnd('\\', '/');
            if (path.Length == 0) return;
            lock (Gate) PrivatePaths.Add(new KeyValuePair<string, string>(path, replacement));
        }

        private static string Clean(string text)
        {
            text = text ?? "";
            foreach (var pair in PrivatePaths.OrderByDescending(x => x.Key.Length))
            {
                text = text.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
                text = text.Replace(pair.Key.Replace('\\', '/'), pair.Value, StringComparison.OrdinalIgnoreCase);
                text = text.Replace(pair.Key.Replace('/', '\\'), pair.Value, StringComparison.OrdinalIgnoreCase);
            }
            return text.Length <= 16000 ? text : text.Substring(0, 16000) + " [truncated]";
        }

        private static void Append(string filename, string text)
        {
            try
            {
                if (Session == null) return;
                var path = Path.Combine(Session, filename);
                var line = DateTime.UtcNow.ToString("o") + " " + Clean(text) + Environment.NewLine;
                if (File.Exists(path) && new FileInfo(path).Length + System.Text.Encoding.UTF8.GetByteCount(line) > FileLimit) return;
                File.AppendAllText(path, line);
            }
            catch (Exception) { } // A logging failure must not replace the gameplay error.
        }

        public static void Event(string area, string detail = null)
        {
            lock (Gate) Append("session.log", area + (detail == null ? "" : " " + detail));
        }

        public static void Error(string area, Exception error, bool fatal = false)
        {
            lock (Gate)
            {
                try
                {
                    Append("errors.log", area + " " + error);
                    Append("session.log", (fatal ? "fatal " : "error ") + area + " " + error?.GetType().Name);
                    if (fatal && Session != null)
                        File.WriteAllText(Path.Combine(Session, "crash.txt"), Clean(DateTime.UtcNow.ToString("o") + " " + area + " " + error));
                }
                catch (Exception) { }
            }
        }

        public static void SimAntics(string title, string message)
        {
            if (title != "SimAntics Exception!" || message == null ||
                !message.StartsWith("A SimAntics Exception has occurred", StringComparison.Ordinal)) return;
            lock (Gate)
            {
                Append("errors.log", "SimAntics " + message);
                Append("session.log", "simantics-error (see errors.log)");
            }
        }
    }
}
