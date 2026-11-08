using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model;
using Simitone.Client.Utils;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static VMAvatarMarshal Sim(uint guid, short objectID) => new VMAvatarMarshal
{
    GUID = guid, ObjectID = objectID,
    PersonData = Enumerable.Range(0, 101).Select(i => (short)i).ToArray()
};
static KeyValuePair<uint, FameProgress> Fame(uint guid, short score, short stars, short high) =>
    new KeyValuePair<uint, FameProgress>(guid, new FameProgress(score, stars, high));

var actor = Sim(101, 7);
var stayAtHome = Sim(102, 8);
var visitor = Sim(999, 9);
var before = (short[])actor.PersonData.Clone();
var home = new VMMarshal { Entities = new VMEntityMarshal[] { actor, stayAtHome, visitor, new VMGameObjectMarshal { GUID = 101 } } };
var progress = FameProgressTransfer.CaptureFamily(new uint[] { 101, 102 }, new[]
{
    Fame(101, 78, 5, 90), Fame(999, 900, 10, 1000)
});
Check(progress.Count == 1 && progress.ContainsKey(101), "Non-family visitor captured or absent family member invented");
FameProgressTransfer.ApplyToHome(home, progress);
Check(actor.PersonData[80] == 78 && actor.PersonData[81] == 5 && actor.PersonData[82] == 90,
    "Returning home restored the stale fame fields");
for (int i = 0; i < before.Length; i++)
    if (i < 80 || i > 82) Check(actor.PersonData[i] == before[i], "Unrelated home person data changed");
Check(stayAtHome.PersonData[80] == 80 && visitor.PersonData[80] == 80, "Absent family member or visitor changed");

// A later destination must replace earlier fame values; demotion/reset are real changes too.
progress = FameProgressTransfer.CaptureFamily(new uint[] { 101 }, new[] { Fame(101, 156, 6, 160) });
FameProgressTransfer.ApplyToHome(home, progress);
Check(actor.PersonData[80] == 156 && actor.PersonData[81] == 6, "Second destination lost its updates");
progress = FameProgressTransfer.CaptureFamily(new uint[] { 101 }, new[] { Fame(101, 2, 1, 160) });
FameProgressTransfer.ApplyToHome(home, progress);
Check(actor.PersonData[80] == 2 && actor.PersonData[81] == 1 && actor.PersonData[82] == 160, "Demotion was incorrectly maximized away");
progress = FameProgressTransfer.CaptureFamily(new uint[] { 101 }, new[] { Fame(101, 0, 0, 0) });
FameProgressTransfer.ApplyToHome(home, progress);
Check(actor.PersonData[80] == 0 && actor.PersonData[81] == 0 && actor.PersonData[82] == 0, "Fame reset was lost");

// GUID, not a transient lot object ID, identifies the same Sim across worlds.
var differentGUID = Sim(103, actor.ObjectID);
FameProgressTransfer.ApplyToHome(new VMMarshal { Entities = new VMEntityMarshal[] { differentGUID } }, progress);
Check(differentGUID.PersonData[80] == 80, "Reused object ID changed a different Sim");
FameProgressTransfer.ApplyToHome(null, progress);
FameProgressTransfer.ApplyToHome(new VMMarshal(), progress);
new FameProgress(1, 2, 3).ApplyTo(null);
var legacy = new short[80];
new FameProgress(1, 2, 3).ApplyTo(legacy);
Check(legacy.All(x => x == 0), "Short legacy person data partially changed");

// Exercise the real NBRS writer/reader used by SaveNeighbourhood, using synthetic data.
var neighborhood = new NBRS();
var neighbor = new Neighbour { Name = "Synthetic actor", NeighbourID = 1, GUID = 101,
    PersonMode = 9, PersonData = new short[88], Relationships = new Dictionary<int, List<short>>() };
neighborhood.AddNeighbor(neighbor);
new FameProgress(1, 0, 0).ApplyTo(neighbor.PersonData);
Check(neighbor.PersonData[80] == 1 && neighbor.PersonData[81] == 0, "Agency enrollment state was not synchronized");
new FameProgress(550, 9, 600).ApplyTo(neighbor.PersonData);
using (var stream = new MemoryStream())
{
    neighborhood.Write(null, stream);
    stream.Position = 0;
    var reloaded = new NBRS();
    reloaded.Read(null, stream);
    var data = reloaded.NeighbourByID[1].PersonData;
    Check(data[80] == 550 && data[81] == 9 && data[82] == 600, "Neighbourhood save/reload lost fame fields");
}
Console.WriteLine("PASS: family-only capture, stale home repair, multi-lot updates, demotion/reset, GUID identity, unrelated-field/visitor isolation, short data, and real NBRS save/reload.");
SuperstarChecks.Run(args);
