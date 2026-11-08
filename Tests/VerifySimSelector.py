"""Exercise the production Sim-selector filter with a headless lot and VM."""
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
source = (root / "Client/Simitone/Simitone.Client/UI/Panels/UISwitchAvatarPanel.cs").read_text(encoding="utf-8-sig")
method = source[source.index("        internal static VMAvatar[] GetSelectableAvatars"):
                source.index("        public UISwitchAvatarPanel(")]
with tempfile.TemporaryDirectory(prefix="simitone-selector-") as temporary:
    fixture = Path(temporary)
    (fixture / "Selector.csproj").write_text("""
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>
<TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup></Project>
""")
    (fixture / "Program.cs").write_text("""
var traveller = new VMAvatar(1);
var relative = new VMAvatar(2);
var visitor = new VMAvatar(3);
var vm = new VM { Controlled = traveller };
vm.Context.ObjectQueries.Avatars.AddRange(new[] { traveller, relative, visitor });
Check(Probe.GetSelectableAvatars(null));
Check(Probe.GetSelectableAvatars(vm), traveller);
vm.TS1State.CurrentFamily = new Family();
Check(Probe.GetSelectableAvatars(vm), traveller);
vm.TS1State.CurrentFamily.RuntimeSubset = new uint[] { 1, 2, 4 };
Check(Probe.GetSelectableAvatars(vm), traveller, relative);
relative.Dead = true;
Check(Probe.GetSelectableAvatars(vm), traveller);
vm.TS1State.CurrentFamily = null;
vm.Controlled = null;
Check(Probe.GetSelectableAvatars(vm));
Console.WriteLine("PASS: no-family, no-controlled-Sim and active household selector cases.");

static void Check(VMAvatar[] actual, params VMAvatar[] expected)
{
    if (!actual.SequenceEqual(expected)) throw new Exception("Unexpected selectable Sims.");
}
class Probe {
""" + method + """
}
class VM
{
    public Context Context = new();
    public LotState TS1State = new();
    public uint MyUID;
    public VMAvatar Controlled;
    public VMAvatar GetAvatarByPersist(uint id) => Controlled;
}
class Context { public ObjectQueries ObjectQueries = new(); }
class ObjectQueries { public List<VMAvatar> Avatars = new(); }
class LotState { public Family CurrentFamily; }
class Family { public uint[] RuntimeSubset; }
class VMAvatar(uint guid)
{
    public bool Dead;
    public AvatarObject Object = new(new Definition(guid));
}
record AvatarObject(Definition OBJ);
record Definition(uint GUID);
""")
    subprocess.run(["dotnet", "run", "--project", str(fixture / "Selector.csproj"), "-c", "Release"], check=True)
