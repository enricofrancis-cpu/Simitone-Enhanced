"""Compile the current production CAS head setter with headless rendering doubles."""
from pathlib import Path
import tempfile, subprocess, argparse
parser=argparse.ArgumentParser();parser.add_argument('--dotnet',default='dotnet');args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
source=(root/'Client/Simitone/Simitone.Client/UI/Screens/TS1CASScreen.cs').read_text()
method=source[source.index('        private void SetHead('):source.index('        private void PopulateReal(')].replace('private void SetHead','public void SetHead',1)
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
Console.WriteLine("PASS: extracted production CAS head setter rejects null/empty selections and preserves valid wrapping (headless doubles).");
class Probe { public List<string> ActiveHeads;public List<string> ActiveHeadTex;
'''+method+r'''}
class VMAvatar { public FSO.SimAntics.Model.VMOutfitReference HeadOutfit; }
class Outfit { public string TS1AppearanceID;public string TS1TextureID; }
static class DirectionUtils { public static double PosMod(double value,double mod)=>(value%mod+mod)%mod; }
namespace FSO.SimAntics.Model { class VMOutfitReference { public Outfit Outfit;public VMOutfitReference(Outfit value){Outfit=value;} } }
''')
 subprocess.run([args.dotnet,'run','--project',str(fixture/'CasProbe.csproj'),'-c','Release'],check=True)
