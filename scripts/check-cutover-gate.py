"""Evaluate approval evidence only. Never deploy, migrate or enable payments."""
from pathlib import Path
import json
import sys

root = Path(__file__).resolve().parent.parent
required = json.loads((root / 'deploy/cutover-gate.example.json').read_text())
gate = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
missing = []
for key in required:
    approved = gate.get(key) is True
    print(f'{key}: {"YES" if approved else "NO"}')
    if not approved:
        missing.append(key)
if missing:
    print('PRODUCTION CUTOVER BLOCKED - external evidence and explicit owner approvals required.')
    sys.exit(2)
print('Checklist evidence is complete. This command does not deploy or enable payments.')
