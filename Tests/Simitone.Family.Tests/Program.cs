using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Simitone.Client.Utils;
static void Check(bool test, string message) { if (!test) throw new Exception(message); }
static Neighbour Sim(short id, uint guid, short family) {
    var data=new short[88]; data[61]=family; data[80]=7; data[15]=600;
    return new Neighbour { NeighbourID=id, GUID=guid, Name="Sim"+id, PersonData=data, PersonMode=9,
        Relationships=new() {{42, new() {75,80}}} };
}
var resource=new IffFile();
var family=new FAMI { ChunkProcessed=true, ChunkID=7, FamilyGUIDs=new uint[] {11,12,13,14,15,16}, RuntimeSubset=new uint[] {11,12} };
var other=new FAMI { ChunkProcessed=true, ChunkID=8, FamilyGUIDs=new uint[] {12,20} };
var names=new FAMs { ChunkProcessed=true, ChunkID=7 };
var neighbors=new NBRS { ChunkProcessed=true, ChunkID=1 };
var first=Sim(1,11,7); var moved=Sim(2,12,8); var unrelated=Sim(3,20,8);
var shortData=Sim(4,13,7); shortData.PersonData=new short[10];
var noData=Sim(5,14,7); noData.PersonData=null; noData.PersonMode=0;
foreach (var sim in new[]{first,moved,unrelated,shortData,noData}) neighbors.AddNeighbor(sim);
foreach (var chunk in new IffChunk[]{family,other,names,neighbors}) { chunk.ChunkProcessed=true; resource.AddChunk(chunk); }
Check(FamilyDeletion.DeleteUnplaced(resource,family), "valid CAS deletion rejected");
Check(first.PersonData[61]==0 && moved.PersonData[61]==8 && unrelated.PersonData[61]==8, "membership isolation failed");
Check(first.PersonData[80]==7 && first.PersonData[15]==600 && first.Relationships[42][0]==75, "unrelated Sim state changed");
Check(resource.Get<FAMI>(7)==null && resource.Get<FAMs>(7)==null && resource.Get<FAMI>(8)==other, "wrong family chunks removed");
Check(family.FamilyGUIDs.Length==0 && family.RuntimeSubset.Length==0, "runtime family retained members");
Check(!FamilyDeletion.DeleteUnplaced(resource,family), "stale repeated deletion accepted");
resource.AddChunk(new FAMI { ChunkProcessed=true, ChunkID=7, FamilyGUIDs=new uint[]{99} });
Check(first.PersonData[61]==0, "old member attached to reused family ID");
// Use the pinned production serializer, with valid-sized records.
neighbors.Entries.Remove(shortData); neighbors.NeighbourByID.Remove(shortData.NeighbourID);
using var stream=new MemoryStream();
neighbors.Write(resource,stream); stream.Position=0;
var loaded=new NBRS(); loaded.Read(new IffFile(),stream);
Check(loaded.NeighbourByID[1].PersonData[61]==0 && loaded.NeighbourByID[2].PersonData[61]==8, "membership did not survive NBRS save/reload");
Check(loaded.NeighbourByID[1].PersonData[80]==7 && loaded.NeighbourByID[1].Relationships[42][1]==80, "roundtrip lost unrelated state");
// SaveNeighbourhood writes the entire IFF, not just NBRS. Verify that path too.
foreach (var chunk in resource.ListAll()) chunk.ChunkLabel="";
using var wholeFile=new MemoryStream();
resource.Write(wholeFile); wholeFile.Position=0;
var reloaded=new IffFile(); reloaded.Read(wholeFile);
Check(reloaded.Get<FAMs>(7)==null && reloaded.Get<FAMI>(7).FamilyGUIDs.SequenceEqual(new uint[]{99}), "whole IFF retained deleted family data");
Check(reloaded.Get<NBRS>(1).NeighbourByID[1].PersonData[61]==0 && reloaded.Get<FAMI>(8).FamilyGUIDs.Contains(20u), "whole IFF lost membership cleanup or unrelated family");
foreach (var id in new ushort[]{0,1000,2000}) {
    var protectedFamily=new FAMI { ChunkProcessed=true, ChunkID=id, FamilyGUIDs=new uint[]{11} }; resource.AddChunk(protectedFamily);
    Check(!FamilyDeletion.DeleteUnplaced(resource,protectedFamily), "protected family deleted");
}
var housed=new FAMI { ChunkProcessed=true,ChunkID=9, HouseNumber=1}; resource.AddChunk(housed);
Check(!FamilyDeletion.DeleteUnplaced(resource,housed), "housed family deletion should use eviction flow");
var absent=new FAMI { ChunkProcessed=true,ChunkID=10};
Check(!FamilyDeletion.DeleteUnplaced(resource,absent), "unregistered family accepted");
var noNBR=new IffFile(); var empty=new FAMI { ChunkProcessed=true,ChunkID=2}; noNBR.AddChunk(empty);
Check(FamilyDeletion.DeleteUnplaced(noNBR,empty), "empty family without NBRS should be removable");
Console.WriteLine("PASS: family cleanup, reused IDs, moved/missing/malformed member isolation, protected families, real NBRS save/reload.");
