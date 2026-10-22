from pathlib import Path
import sys

root = Path(sys.argv[1]).resolve()
if not root.is_dir() or not any(path.is_file() for path in root.rglob('*')):
    print('Release artifact privacy check: FAIL; directory is missing or empty.')
    sys.exit(1)
forbidden = [p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and
             ('.Local.json' in p.name or p.name == 'appsettings.Development.json' or 'App_Data' in p.parts or 'private-media' in p.parts or 'development-email' in p.parts or any(name in p.name.lower() for name in ['be.tests', 'acceptancehost', 'paymentsimulator', 'payment-simulator']))]
for path in root.rglob('*'):
    if path.is_file() and path.suffix.lower() in {'.js', '.mjs', '.html'}:
        content = path.read_text(encoding='utf-8-sig')
        if any(marker in content for marker in ['payments.simulator.invalid', 'amaara_local_payment_lab', 'SIMULATED PAYMENTS']):
            forbidden.append(path.relative_to(root).as_posix())
for path in forbidden:
    print('Forbidden release artifact: ' + path)
print(f'Release artifact privacy check: {"FAIL" if forbidden else "PASS"}')
sys.exit(1 if forbidden else 0)
