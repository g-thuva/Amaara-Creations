from pathlib import Path
import sys

root = Path(sys.argv[1]).resolve()
forbidden = [p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and
             ('.Local.json' in p.name or p.name == 'appsettings.Development.json' or 'App_Data' in p.parts or 'private-media' in p.parts or 'development-email' in p.parts)]
for path in forbidden:
    print('Forbidden release artifact: ' + path)
print(f'Release artifact privacy check: {"FAIL" if forbidden else "PASS"}')
sys.exit(1 if forbidden else 0)
