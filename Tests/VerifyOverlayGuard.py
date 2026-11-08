"""Exercise all real MSBuild overlay source guards without editing FreeSO."""
import argparse
from pathlib import Path
import subprocess
import tempfile
from xml.sax.saxutils import quoteattr
parser=argparse.ArgumentParser(); parser.add_argument('--dotnet',default='dotnet'); args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
groups={
 'FSO.Content': ('tso.content',['TS1/TS1NeighbourProvider.cs','TS1/TS1BCFProvider.cs','Framework/TS1Provider.cs']),
 'FSO.SimAntics': ('tso.simantics',['Utils/VMTS1PurchasableOutfitHelper.cs','Primitives/VMTS1InventoryOperations.cs','Primitives/VMGenericTS1Call.cs','Engine/VMThread.cs']),
 'FSO.LotView': ('tso.world',['Components/ObjectComponent.cs']),
 'FSO.Vitaboy': ('tso.vitaboy.model',['CFP.cs'])
}
for project_name,(directory,names) in groups.items():
 with tempfile.TemporaryDirectory(prefix='simitone-overlay-guard-') as temporary:
  fixture=Path(temporary); project=fixture/(project_name+'.proj')
  project.write_text('<Project><Import Project='+quoteattr(str(root/'Directory.Build.targets'))+'/></Project>')
  def check():
   return subprocess.run([args.dotnet,'msbuild',str(project),'-t:VerifyEnhancedProviderBase','-nologo'],text=True,capture_output=True)
  for ending in (b'\n',b'\r\n'):
   for name in names:
    original=(root/'FreeSO/TSOClient'/directory/name).read_bytes().replace(b'\r\n',b'\n')
    target=fixture/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(original.replace(b'\n',ending))
   result=check()
   if result.returncode: raise RuntimeError('Original rejected: '+result.stdout+result.stderr)
  for name in names:
   target=fixture/name;original=target.read_bytes();target.write_bytes(original+b'\n// synthetic changed source\n')
   result=check()
   if not result.returncode or 'Review EngineOverrides' not in result.stdout+result.stderr:
    raise RuntimeError('Changed source accepted: '+name+' '+result.stdout+result.stderr)
   target.write_bytes(original)
print('PASS: all four project guards accept LF/CRLF originals and reject changes to all nine overlaid sources; submodule untouched.')
