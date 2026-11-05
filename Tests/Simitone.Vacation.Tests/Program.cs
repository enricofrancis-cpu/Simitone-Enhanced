using System.Security.Cryptography;
using Simitone.Client.Utils;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void Reject(Action action, string message)
{
    try { action(); }
    catch (InvalidDataException) { return; }
    catch (IOException) { return; }
    throw new Exception(message);
}
static void Resign(byte[] bytes)
{
    SHA256.HashData(bytes.AsSpan(0, bytes.Length - 32)).CopyTo(bytes, bytes.Length - 32);
}
static VacationCheckpoint Sample() => new VacationCheckpoint
{
    HomeLot = 5, DestinationLot = 41, FamilyID = 7, FamilyNumber = 23,
    ControllingSim = 0x12345678, TransitInfo = 1, HomeWeather = 256, DestinationWeather = 513,
    HomeState = new byte[] { 1, 2, 3 }, DestinationState = new byte[] { 9, 8, 7, 6 }
};

var source = Sample();
var bytes = source.Serialize();
var read = VacationCheckpoint.Deserialize(bytes);
Check(read.HomeLot == 5 && read.DestinationLot == 41 && read.FamilyID == 7 && read.FamilyNumber == 23 &&
    read.ControllingSim == 0x12345678 && read.TransitInfo == 1 && read.HomeWeather == 256 &&
    read.DestinationWeather == 513, "Travel context did not round-trip");
Check(read.HomeState.SequenceEqual(source.HomeState) && read.DestinationState.SequenceEqual(source.DestinationState),
    "Both independent lot snapshots must survive a round-trip");
Check(read.MatchesHousehold(5, 7, 23, new uint[] { 0x12345678 }), "Correct household rejected");
Check(!read.MatchesHousehold(6, 7, 23, new uint[] { 0x12345678 }), "Wrong home accepted");
Check(!read.MatchesHousehold(5, 8, 23, new uint[] { 0x12345678 }), "Wrong family accepted");
Check(!read.MatchesHousehold(5, 7, 24, new uint[] { 0x12345678 }), "Reused family ID accepted");
Check(!read.MatchesHousehold(5, 7, 23, new uint[] { 42 }), "Unrelated controlling Sim accepted");
for (int length = 0; length < bytes.Length; length++)
    Reject(() => VacationCheckpoint.Deserialize(bytes[..length]), "Truncated checkpoint accepted");
var damaged = (byte[])bytes.Clone();
damaged[45] ^= 1;
Reject(() => VacationCheckpoint.Deserialize(damaged), "Corrupted snapshot accepted");
var future = (byte[])bytes.Clone();
BitConverter.GetBytes(2).CopyTo(future, 6);
Resign(future);
Reject(() => VacationCheckpoint.Deserialize(future), "Future format accepted");
var badLength = (byte[])bytes.Clone();
BitConverter.GetBytes(int.MaxValue).CopyTo(badLength, 34);
Resign(badLength);
Reject(() => VacationCheckpoint.Deserialize(badLength), "Unbounded snapshot length accepted");
foreach (var invalidLot in new[] { 0, 39, 50, 81 })
{
    var invalid = Sample(); invalid.DestinationLot = invalidLot;
    Reject(() => invalid.Serialize(), "Non-vacation destination accepted");
}
Check(VacationCheckpoint.IsVacationLot(40) && VacationCheckpoint.IsVacationLot(49), "Vacation boundary rejected");

var root = Path.Combine(Path.GetTempPath(), "simitone-vacation-tests-" + Guid.NewGuid().ToString("N"));
try
{
    var path = VacationCheckpoint.GetPath(Path.Combine(root, "UserData"), 5);
    source.Save(path);
    var previous = File.ReadAllBytes(path);
    var replacement = Sample(); replacement.DestinationLot = 49;
    Reject(() => replacement.Save(path, () => throw new IOException("Simulated neighbourhood save failure")),
        "Failed neighbourhood save should abort checkpoint replacement");
    Check(File.ReadAllBytes(path).SequenceEqual(previous), "Failed save destroyed previous checkpoint");
    Check(!Directory.EnumerateFiles(Path.GetDirectoryName(path), "*.tmp").Any(), "Temporary file leaked");
    var invalid = Sample(); invalid.HomeState = Array.Empty<byte>();
    bool callbackRan = false;
    Reject(() => invalid.Save(path, () => callbackRan = true), "Invalid save accepted");
    Check(!callbackRan && File.ReadAllBytes(path).SequenceEqual(previous), "Invalid save changed persisted state");
    replacement.Save(path);
    Check(VacationCheckpoint.Read(path).DestinationLot == 49, "Replacement checkpoint not committed");
    Check(path != VacationCheckpoint.GetPath(Path.Combine(root, "UserData2"), 5), "Neighbourhood paths collide");
    Check(path != VacationCheckpoint.GetPath(Path.Combine(root, "UserData"), 6), "Household paths collide");
}
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
Console.WriteLine("PASS: travel context and both snapshots, household ownership, corruption/truncation/version/length rejection, failed-save preservation, atomic replacement, path isolation.");
LotSnapshotChecks.Run();
