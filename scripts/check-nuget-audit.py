"""Fail CI for vulnerable direct/transitive dependencies or an unavailable audit."""
import json
import subprocess
import sys

result = subprocess.run(['dotnet', 'list', 'be/be.csproj', 'package', '--vulnerable', '--include-transitive', '--format', 'json'], capture_output=True, text=True)
if result.returncode:
    print('NuGet audit: unavailable (FAIL). Check feed connectivity without publishing private configuration.')
    sys.exit(1)
try:
    report = json.loads(result.stdout)
    if not report.get('projects'):
        raise ValueError('Missing project results')
    findings = []
    for project in report['projects']:
        for framework in project.get('frameworks', []):
            for package in framework.get('topLevelPackages', []) + framework.get('transitivePackages', []):
                if package.get('vulnerabilities'):
                    findings.append(package['id'])
except (ValueError, KeyError, TypeError):
    print('NuGet audit: invalid report (FAIL).')
    sys.exit(1)
for package in findings:
    print('Vulnerable dependency: ' + package)
print(f'NuGet audit: {"FAIL" if findings else "PASS"}; {len(findings)} known vulnerable packages.')
sys.exit(1 if findings else 0)
