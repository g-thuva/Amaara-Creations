from pathlib import Path
import sys

root = Path(sys.argv[1]).resolve()
if not root.is_dir() or not any(path.is_file() for path in root.rglob('*')):
    print('Release artifact privacy check: FAIL; directory is missing or empty.')
    sys.exit(1)
forbidden = [p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and
             ('.Local.json' in p.name or p.name == 'appsettings.Development.json' or 'App_Data' in p.parts or 'private-media' in p.parts or 'development-email' in p.parts)]
for path in forbidden:
    print('Forbidden release artifact: ' + path)
print(f'Release artifact privacy check: {"FAIL" if forbidden else "PASS"}')
sys.exit(1 if forbidden else 0)
