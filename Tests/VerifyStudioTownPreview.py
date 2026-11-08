"""Check the production lot-label fallback and the packaged classic marker."""
import gzip
from pathlib import Path
import struct
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
client = root / "Client/Simitone/Simitone.Client"
source = (client / "UI/Panels/UIHouseSelectPanel.cs").read_text(encoding="utf-8-sig")
thread = (root / "EngineOverrides/FSO.SimAntics/Engine/VMThread.cs").read_text(encoding="utf-8-sig")
permissions = thread[thread.index("        internal static bool IsUninvitedTS1Visitor"):
                     thread.index("        public List<VMPieMenuInteraction> CheckAction")]
title_method = source[source.index("        internal static string GetLotTitle"):
                      source.index("        public UIHouseSelectPanel(int houseID)")]
with tempfile.TemporaryDirectory(prefix="simitone-lot-title-") as temporary:
    fixture = Path(temporary)
    (fixture / "TitleProbe.csproj").write_text("""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
""")
    (fixture / "Program.cs").write_text("""
using System;
MenuPermissions.Verify();
Check(Probe.GetLotTitle("Studio", "Main Street", 83) == "Studio");
Check(Probe.GetLotTitle("", "Main Street", 83) == "Main Street");
Check(Probe.GetLotTitle("", null, 83) == "Lot 83");
Check(Probe.GetLotTitle(null, "  ", 90) == "Lot 90");
static void Check(bool condition)
{
    if (!condition) throw new Exception("Incorrect lot title fallback.");
}
class Probe {
""" + title_method + "}")
    (fixture / "Permissions.cs").write_text("""
using System;
using System.Collections.Generic;
using System.Linq;
[Flags]
enum TTABFlags { AllowVisitors=1, TS1NoChild=16, TS1NoAdult=64, Debug=128,
    TS1AllowCats=512, TS1AllowDogs=1024, FSOSkipPermissions=2048 }
enum VMPersonDataVariable { PersonType, GreetStatus, PersonsAge }
enum VMPrimitiveExitCode { RETURN_TRUE, RETURN_FALSE }
class VMPieMenuInteraction {}
class VMQueuedAction {
    public TTABFlags Flags;
    public object CheckRoutine, Callee, CodeOwner, StackObject;
}
class VMStackFrame { public object Caller, Callee, CodeOwner, StackObject, Routine; public short[] Args; }
class FakeContext { public FakeVM VM = new(); }
class FakeVM { public Lot TS1State = new(); }
class Lot { public Family CurrentFamily = new(); }
class Family { public uint[] RuntimeSubset = Array.Empty<uint>(); }
class FakeObject { public FakeOBJD OBJ = new(); }
class FakeOBJD { public uint GUID = 7; }
class VMAvatar {
    public bool IsPet, IsCat, IsDog;
    public short PersonType=1, GreetStatus=0, Age=25;
    public FakeObject Object = new();
    public short GetPersonData(VMPersonDataVariable v) =>
        v == VMPersonDataVariable.PersonType ? PersonType :
        v == VMPersonDataVariable.GreetStatus ? GreetStatus : Age;
}
class MenuPermissions {
    private object Entity = new VMAvatar();
    private FakeContext Context = new();
    private bool CheckPasses = true;
    private VMPrimitiveExitCode EvaluateCheck(FakeContext c, object entity, VMStackFrame f,
        object unused, List<VMPieMenuInteraction> result) =>
        CheckPasses ? VMPrimitiveExitCode.RETURN_TRUE : VMPrimitiveExitCode.RETURN_FALSE;
    public static void Verify() {
        var engine = new MenuPermissions();
        var sim = (VMAvatar)engine.Entity;
        var stage = new VMQueuedAction { Flags = (TTABFlags)0x32, CheckRoutine = new object() };
        void Expect(bool allowed) {
            if ((engine.CheckTS1Action(stage, false) != null) != allowed)
                throw new Exception("Unexpected stage menu permission.");
        }
        Expect(false); // Ungreeted NPC visitor.
        engine.Context.VM.TS1State.CurrentFamily.RuntimeSubset = new uint[] { 7 };
        Expect(true); // Active traveling household member still has PersonType 1.
        sim.Age = 10; Expect(false);
        sim.Age = 25;
        engine.CheckPasses = false; Expect(false);
        engine.CheckPasses = true;
        sim.IsPet = true; Expect(false);
        sim.IsPet = false;
        engine.Context.VM.TS1State.CurrentFamily = null;
        Expect(false);
        sim.GreetStatus = 2;
        Expect(true); // Preserve greeted visitor behavior.
    }
""" + permissions + "}")
    subprocess.run(["dotnet", "run", "--project", str(fixture), "-c", "Release"], check=True)

assets = client / "Content/3D"
data = gzip.decompress((assets / "classic-plumbob.fsom").read_bytes())
assert struct.unpack_from("<4sii", data) == (b"FSOm", 2, 0)
offset = 13 + data[12]
assert struct.unpack_from("<i", data, offset) == (2,)
offset += 4
points = set()
for _ in range(2):
    assert struct.unpack_from("<iHHi", data, offset) == (1, 2, 65535, 24)
    offset += 12
    for index in range(24):
        position = struct.unpack_from("<3f", data, offset + index * 32)
        points.add(tuple(round(value * 1.33, 4) for value in position))
    offset += 24 * 32
    assert struct.unpack_from("<25i", data, offset) == (24, *range(24))
    offset += 25 * 4
assert len(points) == 10 and sum(point[1] == 0.1612 for point in points) == 8
for axis, extent in enumerate((0.675, 2.1, 0.675)):
    values = [point[axis] for point in points]
    assert abs(max(values) - min(values) - extent * 0.94) < 2e-4
assert len(data) - offset == 24
assert (assets / "TEX_2.png").read_bytes().startswith(b"\x89PNG\r\n\x1a\n")
print("PASS: lot title fallback and classic plumbob mesh and texture.")

root = Path(__file__).resolve().parents[1]
travel = (root / "EngineOverrides/FSO.SimAntics/Primitives/VMGenericTS1Call.cs").read_text()
for context in ("crossDataDT", "crossData2"):
    activate = f"ActivateFamily(context.VM, {context}.ActiveFamily)"
    spawn = f"CreateObjectInstance({context}.DowntownSimGUID"
    assert travel.rindex(activate, 0, travel.index(spawn)) < travel.index(spawn)
print("PASS: single-Sim travel activates the selected family before constructing the avatar.")
