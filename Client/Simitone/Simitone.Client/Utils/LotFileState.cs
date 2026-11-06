using System;
using System.Buffers.Binary;
using System.IO;

namespace Simitone.Client.Utils
{
    public static class LotFileState
    {
        public const short ClearWeather = 1 << 8;

        public static string WeatherPath(string lotPath)
        {
            if (string.IsNullOrWhiteSpace(lotPath)) throw new ArgumentException("A lot path is required.", nameof(lotPath));
            return Path.ChangeExtension(Path.GetFullPath(lotPath), ".weather");
        }

        public static short ReadWeather(string lotPath)
        {
            var path = WeatherPath(lotPath);
            try
            {
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (file.Length != sizeof(short)) throw new InvalidDataException("Expected a two-byte weather state.");
                    Span<byte> bytes = stackalloc byte[sizeof(short)];
                    file.ReadExactly(bytes);
                    return BinaryPrimitives.ReadInt16LittleEndian(bytes);
                }
            }
            catch (FileNotFoundException) { return ClearWeather; }
            catch (DirectoryNotFoundException) { return ClearWeather; }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException)
            {
                // Weather is optional; retain damaged files for inspection and open the lot clear.
                Console.Error.WriteLine("Lot weather unavailable at " + path + ": " + error.Message);
                return ClearWeather;
            }
        }

        public static void WriteWeather(string lotPath, short weather)
        {
            var path = WeatherPath(lotPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Span<byte> bytes = stackalloc byte[sizeof(short)];
                    BinaryPrimitives.WriteInt16LittleEndian(bytes, weather);
                    file.Write(bytes);
                    file.Flush(true);
                }
                File.Move(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public static string PrepareBlueprintPath(string profilePath)
        {
            if (string.IsNullOrWhiteSpace(profilePath)) throw new ArgumentException("A profile path is required.", nameof(profilePath));
            var directory = Path.Combine(Path.GetFullPath(profilePath), "Blueprints");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "cas.xml");
            // Older builds created this exact directory in place of the output file.
            // Only an empty directory is removed; user contents are never deleted recursively.
            if (Directory.Exists(path)) Directory.Delete(path, false);
            return path;
        }
    }
}
