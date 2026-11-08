"""Run production resource-key, outfit-validation, and control-command regressions."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
OVERLAYS = ROOT / "EngineOverrides"

def section(path, start, end):
    text = (OVERLAYS / path).read_text(encoding="utf-8-sig")
    begin = text.index(start)
    return text[begin:text.index(end, begin)]

resource_key = section(
    "FSO.Content/TS1/TS1BCFProvider.cs", "        internal static string ResourceKey",
    "        public object Get(string name, Type expected)")
outfits = section(
    "FSO.SimAntics/Engine/VMSuitProvider.cs", "        private static VMOutfitReference ReadExpansionSuit",
    "        public static object GetSuit")
control = (OVERLAYS / "FSO.SimAntics/NetPlay/Model/Commands/VMNetChangeControlCmd.cs").read_text(encoding="utf-8-sig")

with tempfile.TemporaryDirectory(prefix="simitone-stability-") as path:
    fixture = Path(path)
    (fixture / "Stability.csproj").write_text("""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
</Project>
""")
    (fixture / "VMNetChangeControlCmd.cs").write_text(control, encoding="utf-8")
    (fixture / "Program.cs").write_text("""
using FSO.SimAntics.NetPlay.Model.Commands;

Probe.Verify();
var vm = new VM();
var player = new VMAvatar(10, 2) { PersistID = 55 };
var pet = new VMAvatar(11, 3);
var mascot = new VMAvatar(12, 4);
vm.Avatars.AddRange([mascot, player, pet]);
vm.Context.ObjectQueries.RegisterAvatarPersist(player, player.PersistID);
var select = new VMNetChangeControlCmd { ActorUID = 55, TargetID = 4 };
Check(!select.Execute(vm, player) && player.PersistID == 55 && mascot.PersistID == 0);
vm.TS1State.CurrentFamily = new Family { RuntimeSubset = [10, 11] };
Check(!select.Execute(vm, player) && vm.Context.ObjectQueries.Selected == player);
select.TargetID = 3;
pet.Dead = true;
Check(!select.Execute(vm, player) && vm.Context.ObjectQueries.Selected == player);
pet.Dead = false;
Check(select.Execute(vm, player) && vm.Context.ObjectQueries.Selected == pet);
Check(vm.SelectedObject == 3 && player.PersistID == 0 && pet.PersistID == 55);
Console.WriteLine("PASS: blank accessory names, costume entries, and household-only control.");

static void Check(bool condition)
{
    if (!condition) throw new Exception("Unexpected household control result.");
}

class Probe
{
    public static void Verify()
    {
        foreach (var input in new string[] { null, "", "  ", ".apr" })
            if (ResourceKey(input, ".apr") != "") throw new Exception("Blank appearance was accepted.");
        if (ResourceKey(" MyAccessory.APR ", ".apr") != "myaccessory" ||
            ResourceKey("MyAccessory", ".apr") != "myaccessory" ||
            ResourceKey("MyAnim.ANIM", ".anim") != "myanim" ||
            ResourceKey("Skeleton.SKEL", ".skel") != "skeleton")
            throw new Exception("A valid asset key was changed.");
        var strings = new STR();
        foreach (var input in new string[] { null, "", " ", " Added " })
        {
            strings.Entry = input;
            if (ReadExpansionSuit(strings, 27) != null) throw new Exception("Empty suit was accepted.");
        }
        strings.Entry = " Costume,BODY=custom ";
        if (ReadExpansionSuit(strings, 27)?.Name != "Costume,BODY=custom")
            throw new Exception("Custom costume was changed.");
    }
""" + resource_key + outfits + """
}

class STR { public string Entry; public string GetString(int index) => Entry; }
record VMOutfitReference(string Name, bool Head);

public abstract class VMNetCommandBodyAbstract
{
    public uint ActorUID;
    public abstract bool Execute(VM vm, VMAvatar caller);
    public abstract bool Verify(VM vm, VMAvatar caller);
    public virtual void SerializeInto(BinaryWriter writer) {}
    public virtual void Deserialize(BinaryReader reader) {}
}
public class VM
{
    public static bool UseWorld;
    public bool TS1 = true;
    public LotState TS1State = new();
    public Context Context = new();
    public List<VMAvatar> Avatars = new();
    public short SelectedObject;
    public VMAvatar GetObjectById(short id) => Avatars.FirstOrDefault(avatar => avatar.ObjectID == id);
    public void SetGlobalValue(int index, short value) => SelectedObject = value;
}
public class LotState { public Family CurrentFamily; }
public class Family { public uint[] RuntimeSubset; }
public class Context { public Queries ObjectQueries = new(); public World World = new(); }
public class Queries
{
    public VMAvatar Selected;
    public void RegisterAvatarPersist(VMAvatar avatar, uint id) => Selected = avatar;
    public void RemoveAvatarPersist(uint id) => Selected = null;
}
public class World { public void CenterTo(object component) {} }
public class VMAvatar(uint guid, short id)
{
    public bool Dead;
    public uint PersistID;
    public short ObjectID = id;
    public object WorldUI;
    public AvatarObject Object = new(new Definition(guid));
}
public record AvatarObject(Definition OBJ);
public record Definition(uint GUID);
""", encoding="utf-8")
    subprocess.run(["dotnet", "run", "--project", str(fixture / "Stability.csproj"), "-c", "Release"], check=True)
