"""Check the production lot-label fallback and the packaged classic marker."""
import gzip
from pathlib import Path
import struct
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
client = root / "Client/Simitone/Simitone.Client"
source = (client / "UI/Panels/UIHouseSelectPanel.cs").read_text(encoding="utf-8-sig")
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
    subprocess.run(["dotnet", "run", "--project", str(fixture), "-c", "Release"], check=True)

assets = client / "Content/3D"
data = gzip.decompress((assets / "classic-plumbob.fsom").read_bytes())
assert struct.unpack_from("<4sii", data) == (b"FSOm", 2, 0)
offset = 13 + data[12]
assert struct.unpack_from("<i", data, offset) == (2,)
offset += 4
for _ in range(2):
    assert struct.unpack_from("<iHHi", data, offset) == (1, 2, 65535, 12)
    offset += 12 + 12 * 32
    assert struct.unpack_from("<13i", data, offset) == (12, *range(12))
    offset += 13 * 4
assert len(data) - offset == 24
assert (assets / "TEX_2.png").read_bytes().startswith(b"\x89PNG\r\n\x1a\n")
print("PASS: lot title fallback and classic plumbob mesh and texture.")
