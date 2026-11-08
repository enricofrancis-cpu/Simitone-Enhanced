"""Exercise the production CAS head setter and appearance validation with headless doubles."""
from pathlib import Path
import tempfile, subprocess, argparse
parser=argparse.ArgumentParser();parser.add_argument('--dotnet',default='dotnet');args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
source=(root/'Client/Simitone/Simitone.Client/UI/Screens/TS1CASScreen.cs').read_text()
method=source[source.index('        private void SetHead('):source.index('        private void PopulateReal(')].replace('private void SetHead','public void SetHead',1)
validation=source[source.index('        private static bool CanRenderAppearance('):source.index('        private void PopulateSimType(')].replace('private static bool','public static bool',1)
with tempfile.TemporaryDirectory(prefix='simitone-cas-empty-') as temporary:
 fixture=Path(temporary)
 (fixture/'CasProbe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>')
 (fixture/'Program.cs').write_text(r'''using System; using System.Collections.Generic;
var screen=new Probe();var head=new VMAvatar();
screen.SetHead(head,0);screen.ActiveHeads=new();screen.SetHead(head,-100);
if(head.HeadOutfit!=null) throw new Exception("Empty selection wrote outfit");
screen.ActiveHeads=new(){"c001","c002"};screen.ActiveHeadTex=new(){"tex1","tex2"};
screen.SetHead(head,-1);
if(head.HeadOutfit.Outfit.TS1AppearanceID!="c002.apr" || head.HeadOutfit.Outfit.TS1TextureID!="tex2") throw new Exception("Valid wrapped selection failed");
Content.Get().AvatarAppearances.Items["outfit.apr"] = new Appearance { Bindings = new[] {
    new AppearanceBinding { RealBinding = new Binding { MeshName = "present.mesh" } },
    new AppearanceBinding { RealBinding = new Binding { MeshName = "missing.mesh" } }
} };
Content.Get().AvatarMeshes.Items["present.mesh"] = new Mesh { TextureName = "x" };
if (Probe.CanRenderAppearance("outfit")) throw new Exception("Missing second mesh was accepted");
if (!DiagnosticLog.Last.Contains("outfit; mesh=missing.mesh")) throw new Exception("Missing mesh diagnostic was not recorded");
Content.Get().AvatarMeshes.Items["missing.mesh"] = new Mesh();
if (Probe.CanRenderAppearance("outfit")) throw new Exception("Missing mesh texture name was accepted");
Content.Get().AvatarMeshes.Items["missing.mesh"].TextureName = "custom";
if (!Probe.CanRenderAppearance("outfit")) throw new Exception("Complete outfit was rejected");
if (Probe.CanRenderAppearance("absent")) throw new Exception("Missing appearance was accepted");
Console.WriteLine("PASS: extracted production CAS head setter rejects null/empty selections and preserves valid wrapping, and appearance validation rejects missing meshes (headless doubles).");
class Probe { public List<string> ActiveHeads;public List<string> ActiveHeadTex;
'''+method+validation+r'''}
class Content
{
    private static readonly Content Instance = new();
    public static Content Get() => Instance;
    public Provider<Appearance> AvatarAppearances = new();
    public Provider<Mesh> AvatarMeshes = new();
}
class Provider<T> where T : class
{
    public Dictionary<string, T> Items = new();
    public T Get(string name) => Items.GetValueOrDefault(name);
}
class Appearance { public AppearanceBinding[] Bindings; }
class AppearanceBinding { public Binding RealBinding; }
class Binding { public string MeshName; }
class Mesh { public string TextureName; }
static class DiagnosticLog
{
    public static string Last;
    public static void Event(string area, string details) => Last = area + " " + details;
}
class VMAvatar { public FSO.SimAntics.Model.VMOutfitReference HeadOutfit; }
class Outfit { public string TS1AppearanceID;public string TS1TextureID; }
static class DirectionUtils { public static double PosMod(double value,double mod)=>(value%mod+mod)%mod; }
namespace FSO.SimAntics.Model { class VMOutfitReference { public Outfit Outfit;public VMOutfitReference(Outfit value){Outfit=value;} } }
''')
 subprocess.run([args.dotnet,'run','--project',str(fixture/'CasProbe.csproj'),'-c','Release'],check=True)
