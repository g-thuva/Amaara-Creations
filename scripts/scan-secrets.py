"""Scan non-ignored repository text without ever printing candidate values."""
from pathlib import Path
import re
import subprocess
import sys

root = Path(__file__).resolve().parent.parent
paths = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z'], cwd=root).decode().split('\0')
patterns = {
    'private-key': re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
    'aws-access-key': re.compile(r'\bAKIA[0-9A-Z]{16}\b'),
    'github-token': re.compile(r'\b(?:ghp_[A-Za-z0-9]{36}|github_pat_[A-Za-z0-9_]{60,})\b'),
    'assigned-secret': re.compile(r'''(?i)(?:["'](?:api_?key|webhook_?signing_?key|password|client_?secret)["']|(?<![-\w])(?:api_?key|webhook_?signing_?key|password|client_?secret))\s*[:=]\s*["']([^"'\r\n]{16,})["']'''),
}
findings = []
scanned = 0
for relative in sorted(set(paths)):
    path = root / relative
    if not relative or not path.is_file() or path.suffix.lower() not in {'.cs', '.js', '.jsx', '.ts', '.tsx', '.json', '.yml', '.yaml', '.ps1', '.env', '.md', '.py', '.mjs', '.template'}:
        continue
    if path.stat().st_size > 2_000_000:
        continue
    content = path.read_text(encoding='utf-8-sig', errors='replace')
    scanned += 1
    for kind, pattern in patterns.items():
        for match in pattern.finditer(content):
            candidate = match.group(1) if match.lastindex else match.group(0)
            # Test-only generated fixtures and documented placeholders are not credentials.
            if kind == 'assigned-secret' and (relative.startswith(('tests/', 'client/e2e/')) or any(word in candidate.lower() for word in ['replace', 'example', 'test', 'placeholder', 'set_in_user_secrets_only']) or any(char in candidate for char in ['(', ')', '{', '}', '$'])):
                continue
            if relative == 'scripts/scan-secrets.py':
                continue
            findings.append((relative, content.count('\n', 0, match.start()) + 1, kind))
for relative, line, kind in findings:
    print(f'{relative}:{line}: {kind} candidate (value suppressed)')
print(f'Secret scan: {"FAIL" if findings else "PASS"}; {scanned} text files; {len(findings)} candidates. Ignored private configuration was excluded.')
sys.exit(1 if findings else 0)
