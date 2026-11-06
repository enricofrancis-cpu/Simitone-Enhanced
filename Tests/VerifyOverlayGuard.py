"""Check the real MSBuild source guard without editing the FreeSO checkout."""
import argparse
from pathlib import Path
import subprocess
import tempfile
from xml.sax.saxutils import quoteattr

parser = argparse.ArgumentParser()
parser.add_argument('--dotnet', default='dotnet')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='simitone-overlay-guard-') as temporary:
    fixture = Path(temporary)
    (fixture / 'TS1').mkdir()
    project = fixture / 'FSO.Content.proj'
    project.write_text('<Project><Import Project=' + quoteattr(str(root / 'Directory.Build.targets')) + '/></Project>')
    for ending in (b'\n', b'\r\n'):
        for name in ('TS1NeighbourProvider.cs', 'TS1BCFProvider.cs'):
            original = (root / 'FreeSO/TSOClient/tso.content/TS1' / name).read_bytes()
            (fixture / 'TS1' / name).write_bytes(original.replace(b'\r\n', b'\n').replace(b'\n', ending))
        result = subprocess.run([args.dotnet, 'msbuild', str(project), '-t:VerifyEnhancedProviderBase', '-nologo'], text=True, capture_output=True)
        if result.returncode:
            raise RuntimeError('Unchanged provider rejected: ' + result.stdout + result.stderr)
    source = fixture / 'TS1/TS1BCFProvider.cs'
    source.write_bytes(source.read_bytes() + b'\n// synthetic changed source\n')
    result = subprocess.run([args.dotnet, 'msbuild', str(project), '-t:VerifyEnhancedProviderBase', '-nologo'], text=True, capture_output=True)
    if not result.returncode or 'Review EngineOverrides' not in result.stdout + result.stderr:
        raise RuntimeError('Changed provider source was not rejected by the guard: ' + result.stdout + result.stderr)
print('PASS: actual MSBuild guard accepts LF/CRLF originals and rejects changed provider source; submodule untouched.')
