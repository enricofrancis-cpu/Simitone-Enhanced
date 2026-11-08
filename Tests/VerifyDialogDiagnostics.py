"""Exercise both production dialog logging callbacks with a headless event payload."""

from pathlib import Path
import re
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
client = root / "Client/Simitone/Simitone.Client"
callbacks = []
for screen in ("TS1GameScreen", "TS1CASScreen"):
    source = (client / "UI/Screens" / f"{screen}.cs").read_text(encoding="utf-8-sig")
    callback, = re.findall(r"vm\.OnDialog \+= ([^;]+);", source)
    callbacks.append(callback)

with tempfile.TemporaryDirectory(prefix="simitone-dialog-diagnostics-") as temporary:
    fixture = Path(temporary)
    shutil.copyfile(client / "Utils/DiagnosticLog.cs", fixture / "DiagnosticLog.cs")
    (fixture / "DialogProbe.csproj").write_text("""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""")
    (fixture / "Program.cs").write_text("""
using Simitone.Client.Utils;

Action<DialogInfo>[] callbacks = { CALLBACKS };
var root = Path.Combine(Path.GetTempPath(), "simitone-dialog-log-" + Guid.NewGuid());
try
{
    DiagnosticLog.Start(root, "dialog-probe");
    var errors = Path.Combine(DiagnosticLog.SessionDirectory, "errors.log");
    foreach (var callback in callbacks)
    {
        callback(null);
        callback(new DialogInfo("Ordinary dialog", "Do not log this"));
    }
    if (File.Exists(errors)) throw new Exception("Clear signals and ordinary dialogs must not log errors.");

    for (int i = 0; i < callbacks.Length; i++)
        callbacks[i](new DialogInfo("SimAntics Exception!", "A SimAntics Exception has occurred: probe " + i));

    var log = File.ReadAllText(errors);
    for (int i = 0; i < callbacks.Length; i++)
        if (!log.Contains("probe " + i)) throw new Exception("An error dialog was not logged.");

    Console.WriteLine("PASS: both production dialog callbacks handle clear signals and log VM errors.");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}
record DialogInfo(string Title, string Message);
""".replace("CALLBACKS", ",\n    ".join(callbacks)))
    subprocess.run(
        ["dotnet", "run", "--project", str(fixture / "DialogProbe.csproj"), "-c", "Release"],
        check=True,
    )
