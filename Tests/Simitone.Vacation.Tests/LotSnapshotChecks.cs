using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Model;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Marshals.Threads;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TS1Platform;
using Simitone.Client.Utils;

internal static class LotSnapshotChecks
{
    private static VMMarshal Lot(short number, int hour) => new VMMarshal
    {
        TS1 = true,
        Context = new VMContextMarshal
        {
            Clock = new VMClockMarshal { Hours = hour, Minutes = 17, TicksPerMinute = 30 },
            Architecture = new VMArchitectureMarshal
            {
                Width = 1, Height = 1, Stories = 1,
                Terrain = new VMArchitectureTerrain(1, 1),
                Walls = new[] { new WallTile[1] }, Floors = new[] { new FloorTile[1] }
            },
            Ambience = new VMAmbientSoundMarshal(), RandomSeed = 12345
        },
        Entities = Array.Empty<VMEntityMarshal>(), Threads = Array.Empty<VMThreadMarshal>(),
        MultitileGroups = Array.Empty<VMMultitileGroupMarshal>(),
        GlobalState = Enumerable.Range(0, 38).Select(i => (short)(i == 10 ? number : 0)).ToArray(),
        PlatformState = new VMTS1LotState
        {
            CurrentFamily = new FAMI { ChunkID = 7, HouseNumber = 5, FamilyNumber = 23,
                Budget = 1234, FamilyGUIDs = new uint[] { 0x12345678 } }
        }
    };

    public static void Run()
    {
        var checkpoint = new VacationCheckpoint
        {
            HomeLot = 5, DestinationLot = 41, FamilyID = 7, FamilyNumber = 23,
            ControllingSim = 0x12345678, TransitInfo = 1,
            HomeState = VacationLotSnapshot.Write(Lot(5, 8)),
            DestinationState = VacationLotSnapshot.Write(Lot(41, 19))
        };
        var loaded = VacationCheckpoint.Deserialize(checkpoint.Serialize());
        var home = VacationLotSnapshot.Read(loaded.HomeState, 5);
        var destination = VacationLotSnapshot.Read(loaded.DestinationState, 41);
        if (home.Context.Clock.Hours != 8 || destination.Context.Clock.Hours != 19 ||
            home.Context.RandomSeed != 12345 || ((VMTS1LotState)destination.PlatformState).CurrentFamily.Budget != 1234)
            throw new Exception("Real FreeSO marshals lost independent lot clocks, RNG or family state");
        try
        {
            VacationLotSnapshot.Read(loaded.HomeState, 41);
            throw new Exception("Swapped home/destination snapshot accepted");
        }
        catch (InvalidDataException) { }
        try
        {
            VacationLotSnapshot.Read(new byte[] { 1, 2, 3, 4 }, 5);
            throw new Exception("Non-VM snapshot accepted");
        }
        catch (InvalidDataException) { }
        Console.WriteLine("PASS: production FreeSO compressed VMMarshal round-trip preserves both lot clocks, RNG and family data; wrong-lot/non-VM snapshots rejected (synthetic lots, no game assets).");
    }
}
