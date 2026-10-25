"""Publish a stamped, subpath-aware site without modifying source or hashed output."""
import argparse
import pathlib
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument('--base', default='/')
parser.add_argument('--output', default='artifacts/site')
parser.add_argument('--version', default='1.0.0-dev')
parser.add_argument('--commit', default='local')
parser.add_argument('--date', default='Local build')
args = parser.parse_args()
if not args.base.startswith('/') or not args.base.endswith('/') or any(c not in '/-_.abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789' for c in args.base):
    parser.error('base must be an absolute directory path ending in /')
root = pathlib.Path(__file__).resolve().parent.parent
output = (root / args.output).resolve()
if not output.is_relative_to(root / 'artifacts'):
    parser.error('output must be inside the repository artifacts directory')
with tempfile.TemporaryDirectory(prefix='social-tennis-publish-') as temporary:
    staging = pathlib.Path(temporary).resolve()
    for name in ['Directory.Build.props', 'stylecop.json', '.editorconfig']:
        shutil.copy2(root / name, staging / name)
    app = staging / 'app.codex.tennis'
    shutil.copytree(root / 'app.codex.tennis', app, ignore=shutil.ignore_patterns('bin', 'obj', 'docs'))
    index = app / 'wwwroot/index.html'
    index.write_text(index.read_text().replace('<base href="/"', f'<base href="{args.base}"'))
    publish_output = staging / 'published'
    subprocess.run(['dotnet', 'publish', str(app / 'app.codex.tennis.csproj'), '--disable-build-servers', '-m:1', '-c', 'Release', '-o', str(publish_output),
                    f'-p:DisplayVersion={args.version}', f'-p:BuildCommit={args.commit}', f'-p:ReleaseDate={args.date}'], check=True)
    # GitHub Pages serves 404.html for deep links. This copies index without altering any cached asset.
    shutil.copy2(publish_output / 'wwwroot/index.html', publish_output / 'wwwroot/404.html')
    (publish_output / 'wwwroot/.nojekyll').touch()
    # Replace only generated artifacts, after a successful publish, so stale assets cannot ship.
    if output.exists():
        shutil.rmtree(output)
    shutil.copytree(publish_output, output)
print(output / 'wwwroot')
