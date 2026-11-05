using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Simitone.Client.Utils
{
    // Simitone-only travel state. Destination house templates are never changed.
    internal sealed class VacationCheckpoint
    {
        private const int Version = 1;
        private const int MaxSnapshotBytes = 256 * 1024 * 1024;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("SIMVAC");

        public int HomeLot;
        public int DestinationLot;
        public ushort FamilyID;
        public int FamilyNumber;
        public uint ControllingSim;
        public short TransitInfo;
        public short HomeWeather;
        public short DestinationWeather;
        public byte[] HomeState;
        public byte[] DestinationState;

        public static bool IsVacationLot(int lot) => lot >= 40 && lot <= 49;

        public bool MatchesHousehold(int homeLot, ushort familyID, int familyNumber, IEnumerable<uint> members)
        {
            return HomeLot == homeLot && FamilyID == familyID && FamilyNumber == familyNumber &&
                members.Contains(ControllingSim);
        }

        public static string GetPath(string neighborhoodPath, int homeLot)
        {
            return Path.Combine(neighborhoodPath, "Simitone", "Vacations", $"House{homeLot:D2}.svc");
        }

        private void Validate()
        {
            if (HomeLot < 1 || HomeLot > 99 || IsVacationLot(HomeLot) ||
                !IsVacationLot(DestinationLot) || FamilyID == ushort.MaxValue ||
                ControllingSim == 0 || TransitInfo < 1 ||
                HomeState == null || HomeState.Length == 0 || HomeState.Length > MaxSnapshotBytes ||
                DestinationState == null || DestinationState.Length == 0 || DestinationState.Length > MaxSnapshotBytes)
                throw new InvalidDataException("Invalid vacation checkpoint metadata or lot snapshots.");
        }

        public byte[] Serialize()
        {
            Validate();
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(Magic);
                    writer.Write(Version);
                    writer.Write(HomeLot);
                    writer.Write(DestinationLot);
                    writer.Write(FamilyID);
                    writer.Write(FamilyNumber);
                    writer.Write(ControllingSim);
                    writer.Write(TransitInfo);
                    writer.Write(HomeWeather);
                    writer.Write(DestinationWeather);
                    writer.Write(HomeState.Length);
                    writer.Write(HomeState);
                    writer.Write(DestinationState.Length);
                    writer.Write(DestinationState);
                }
                var payload = stream.ToArray();
                stream.Write(SHA256.HashData(payload));
                return stream.ToArray();
            }
        }

        public static VacationCheckpoint Deserialize(byte[] bytes)
        {
            if (bytes.Length < 32 + 44 || bytes.LongLength > 2L * MaxSnapshotBytes + 1024)
                throw new InvalidDataException("Invalid vacation checkpoint length.");
            var payloadLength = bytes.Length - 32;
            if (!CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(bytes.AsSpan(0, payloadLength)), bytes.AsSpan(payloadLength)))
                throw new InvalidDataException("The vacation checkpoint is damaged (checksum mismatch).");

            using (var reader = new BinaryReader(new MemoryStream(bytes, 0, payloadLength, false)))
            {
                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic) || reader.ReadInt32() != Version)
                    throw new InvalidDataException("Unsupported vacation checkpoint format.");
                var result = new VacationCheckpoint
                {
                    HomeLot = reader.ReadInt32(),
                    DestinationLot = reader.ReadInt32(),
                    FamilyID = reader.ReadUInt16(),
                    FamilyNumber = reader.ReadInt32(),
                    ControllingSim = reader.ReadUInt32(),
                    TransitInfo = reader.ReadInt16(),
                    HomeWeather = reader.ReadInt16(),
                    DestinationWeather = reader.ReadInt16(),
                    HomeState = ReadSnapshot(reader),
                    DestinationState = ReadSnapshot(reader)
                };
                if (reader.BaseStream.Position != reader.BaseStream.Length)
                    throw new InvalidDataException("Unexpected vacation checkpoint data.");
                result.Validate();
                return result;
            }
        }

        private static byte[] ReadSnapshot(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length <= 0 || length > MaxSnapshotBytes || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid vacation lot snapshot length.");
            return reader.ReadBytes(length);
        }

        public static VacationCheckpoint Read(string path)
        {
            if (new FileInfo(path).Length > 2L * MaxSnapshotBytes + 1024)
                throw new InvalidDataException("Vacation checkpoint is too large.");
            return Deserialize(File.ReadAllBytes(path));
        }

        public void Save(string path, Action saveNeighborhood = null)
        {
            var bytes = Serialize(); // Validate before touching an existing checkpoint.
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    file.Write(bytes);
                    file.Flush(true);
                }
                saveNeighborhood?.Invoke();
                File.Move(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
