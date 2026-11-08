using Simitone.Client.Utils;
static void Check(bool test, string message) { if (!test) throw new Exception(message); }
static void Reject(Action action) {
    try { action(); } catch (IOException) { return; } catch (UnauthorizedAccessException) { return; }
    throw new Exception("Expected a file operation failure.");
}
var root=Path.Combine(Path.GetTempPath(),"simitone-lot-files-"+Guid.NewGuid().ToString("N"));
try {
    var profile=Path.Combine(root,"Profile");
    var house=Path.Combine(profile,"UserData","Houses","House05.iff");
    var otherHouse=Path.Combine(profile,"UserData","Houses","House06.iff");
    var otherNeighborhood=Path.Combine(profile,"UserData2","Houses","House05.iff");
    var otherProfile=Path.Combine(root,"OtherProfile","UserData","Houses","House05.iff");
    Check(LotFileState.ReadWeather(house)==LotFileState.ClearWeather,"missing weather did not default clear");
    Directory.CreateDirectory(Path.Combine(profile,"LocalHouse"));
    File.WriteAllBytes(Path.Combine(profile,"LocalHouse","cas.weather"),new byte[]{1,3});
    Check(LotFileState.ReadWeather(house)==LotFileState.ClearWeather,"unrelated legacy CAS weather leaked into a lot");
    LotFileState.WriteWeather(house,513); LotFileState.WriteWeather(otherHouse,769);
    LotFileState.WriteWeather(otherNeighborhood,1025); LotFileState.WriteWeather(otherProfile,1281);
    Check(LotFileState.ReadWeather(house)==513 && LotFileState.ReadWeather(otherHouse)==769 &&
        LotFileState.ReadWeather(otherNeighborhood)==1025 && LotFileState.ReadWeather(otherProfile)==1281,"house/neighborhood/profile weather states crossed");
    Check(File.ReadAllBytes(LotFileState.WeatherPath(house)).SequenceEqual(new byte[]{1,2}),"weather format is not little-endian");
    foreach(var state in new short[]{0,256,short.MinValue,short.MaxValue}) {
        LotFileState.WriteWeather(house,state); Check(LotFileState.ReadWeather(house)==state,"weather bits did not roundtrip");
    }
    Check(!File.Exists(house),"weather save unexpectedly created or altered the house IFF");
    var weatherPath=LotFileState.WeatherPath(house);
    foreach(var bad in new[]{Array.Empty<byte>(),new byte[]{1},new byte[]{1,2,3},new byte[1024]}) {
        File.WriteAllBytes(weatherPath,bad);
        Check(LotFileState.ReadWeather(house)==LotFileState.ClearWeather && File.ReadAllBytes(weatherPath).SequenceEqual(bad),"corrupt optional weather was loaded or destroyed");
    }
    LotFileState.WriteWeather(house,513);
    Check(!Directory.EnumerateFiles(Path.GetDirectoryName(weatherPath),"*.tmp").Any(),"temporary weather files leaked");
    var blockedLot=Path.Combine(profile,"Blocked","House05.iff");
    Directory.CreateDirectory(LotFileState.WeatherPath(blockedLot));
    File.WriteAllText(Path.Combine(LotFileState.WeatherPath(blockedLot),"keep.txt"),"keep");
    Reject(()=>LotFileState.WriteWeather(blockedLot,769));
    Check(File.ReadAllText(Path.Combine(LotFileState.WeatherPath(blockedLot),"keep.txt"))=="keep" &&
        !Directory.EnumerateFiles(Path.GetDirectoryName(LotFileState.WeatherPath(blockedLot)),"*.tmp").Any(),"failed weather replacement destroyed contents or leaked staging files");
    // Reproduce the old export bug with real filesystem operations.
    var oldPath=Path.Combine(profile,"Blueprints","cas.xml"); Directory.CreateDirectory(oldPath);
    Reject(()=>File.WriteAllText(oldPath,"<house/>"));
    var export=LotFileState.PrepareBlueprintPath(profile);
    File.WriteAllText(export,"<house/>");
    Check(File.ReadAllText(export)=="<house/>" && !Directory.Exists(export),"export path is still a directory");
    LotFileState.WriteWeather(export,769);
    Check(LotFileState.ReadWeather(export)==769 && LotFileState.ReadWeather(house)==513,"export weather overwrote house weather");
    File.Delete(export); Directory.CreateDirectory(export); File.WriteAllText(Path.Combine(export,"keep.txt"),"keep");
    Reject(()=>LotFileState.PrepareBlueprintPath(profile));
    Check(File.ReadAllText(Path.Combine(export,"keep.txt"))=="keep","nonempty legacy export directory contents were deleted");
    Console.WriteLine("PASS: lot/neighborhood/profile weather isolation, binary roundtrips, damaged-file fallback, failed-write cleanup, legacy export-directory reproduction and safe repair.");
} finally { if(Directory.Exists(root)) Directory.Delete(root,true); }
