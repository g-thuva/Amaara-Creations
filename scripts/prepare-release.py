"""Build a versioned, checksum-verified package. Never deploy or migrate a database."""
from pathlib import Path
import argparse
import json
import os
import subprocess
import sys
from release_artifacts import artifact_files, digest, public_config, verify

ROOT = Path(__file__).resolve().parent.parent


def run(command, environment, log):
    result = subprocess.run(command, cwd=ROOT, env=environment, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding='utf-8', errors='replace')
    log.write_text(result.stdout, encoding='utf-8')
    if result.returncode:
        raise ValueError(f'Release command failed; inspect the private local build log {log.name}.')


def prepare(args):
    mode = args.mode
    values = public_config(json.loads(args.public_config.read_text(encoding='utf-8-sig')), mode)
    branch = subprocess.check_output(['git', 'branch', '--show-current'], cwd=ROOT, text=True).strip()
    branch = branch or os.environ.get('GITHUB_HEAD_REF') or os.environ.get('GITHUB_REF_NAME', '')
    if mode == 'production' and branch != 'develop': raise ValueError('Production release preparation must run on develop.')
    if not branch: raise ValueError('Release source branch must be identifiable.')
    dirty = bool(subprocess.check_output(['git', 'status', '--porcelain'], cwd=ROOT).strip())
    if dirty and not (mode == 'rehearsal' and args.allow_dirty_rehearsal):
        raise ValueError('Use a clean checkout, or explicitly label an uncommitted local rehearsal.')
    if mode == 'production':
        gate = json.loads(args.approvals.read_text(encoding='utf-8-sig'))
        required = json.loads((ROOT / 'deploy/cutover-gate.example.json').read_text(encoding='utf-8'))
        if set(gate) != set(required) or any(gate.get(key) is not True for key in required):
            raise ValueError('Production approval evidence is incomplete; no production artifact was prepared.')
    output = args.output.resolve()
    if not output.is_relative_to((ROOT / '.runtime/releases').resolve()):
        raise ValueError('Release output must be a new directory below .runtime/releases.')
    if output.exists(): raise ValueError('Release output already exists; use a new versioned directory.')
    node = subprocess.check_output(['node', '--version'], text=True).strip()
    if int(node.lstrip('v').split('.')[0]) < 22: raise ValueError('Release builds require Node 22 or newer.')
    output.mkdir(parents=True)
    logs = ROOT / '.runtime' / ('release-build-logs-' + output.name)
    logs.mkdir(exist_ok=False)
    environment = {key: value for key, value in os.environ.items() if not key.upper().startswith('VITE_')}
    environment.update(values)
    environment.update(DOTNET_ENVIRONMENT='Production', ASPNETCORE_ENVIRONMENT='Production')
    npm = 'npm.cmd' if os.name == 'nt' else 'npm'
    commands = [
        ['dotnet', 'tool', 'restore'],
        ['dotnet', 'restore', 'be/be.csproj'],
        ['dotnet', 'publish', 'be/be.csproj', '--configuration', 'Release', '--no-restore', '--output', str(output / 'api'), '-p:ContinuousIntegrationBuild=true', '-p:Deterministic=true', '-p:PathMap=' + str(ROOT) + '=/src'],
        ['dotnet', 'ef', 'migrations', 'has-pending-model-changes', '--project', 'be', '--configuration', 'Release', '--no-build'],
        ['dotnet', 'ef', 'migrations', 'script', '--idempotent', '--project', 'be', '--configuration', 'Release', '--no-build', '--output', str(output / 'migrations.sql')],
        [npm, '--prefix', 'client', 'ci'],
        ['node', 'client/scripts/build-release.mjs', str(output / 'web')],
    ]
    for index, command in enumerate(commands):
        run(command, environment, logs / f'{index + 1:02d}.log')
        print(f'PASS release step {index + 1}/{len(commands)}')
    # Published defaults are intentionally unable to impersonate a configured production site.
    settings = json.loads((ROOT / 'be/appsettings.json').read_text(encoding='utf-8-sig'))
    settings['ConnectionStrings'] = {'DefaultConnection': ''}
    settings['Jwt'].update(Key='', Issuer='', Audience='')
    settings['Authentication']['FrontendBaseUrl'] = ''
    settings['AllowedHosts'] = ''
    settings['Cors'] = {'AllowedOrigins': []}
    settings['DataProtection'] = {'KeysPath': ''}
    settings['PublicSite'] = {'BaseUrl': ''}
    settings['Email'] = {'Enabled': False, 'Provider': 'SMTP'}
    settings['AdminBootstrap'] = {'Enabled': False}
    settings['ReleaseControls'] = {'AcceptNewPayments': False, 'AcceptUploads': False, 'AllowAdminChanges': False}
    for provider in ['Zahls', 'Datatrans']:
        settings[provider] = {'Enabled': False}
    for path in (output / 'api').glob('appsettings.*.json'):
        if not path.resolve().is_relative_to((output / 'api').resolve()) or path.is_symlink():
            raise ValueError('Unexpected published settings location.')
        path.unlink()  # Only copies inside this newly created, verified output directory.
    (output / 'api/appsettings.json').write_text(json.dumps(settings, indent=2) + '\n', encoding='utf-8')
    manifest = {
        'format': 1, 'mode': mode, 'sourceBranch': branch,
        'sourceCommit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
        'dirtySource': dirty, 'nodeVersion': node,
        'dotnetSdk': subprocess.check_output(['dotnet', '--version'], cwd=ROOT, text=True).strip(),
        'packageLockSha256': digest(ROOT / 'client/package-lock.json'),
        'publicConfiguration': values, 'runtimeSecretsIncluded': False,
        'paymentsEnabledByPackage': False,
        'migrations': sorted(path.stem for path in (ROOT / 'be/Infrastructure/Persistence/Migrations').glob('[0-9]*.cs') if not path.name.endswith('.Designer.cs')),
        'files': {path.relative_to(output).as_posix(): digest(path) for path in artifact_files(output)},
    }
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2, sort_keys=True) + '\n', encoding='utf-8')
    verify(output)
    print(f'Release preparation: PASS; mode={mode}; source={manifest["sourceCommit"]}; files={len(manifest["files"])}')
    print('Runtime secrets, approved deployment target and integration activation remain external inputs.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--public-config', required=True, type=Path)
    parser.add_argument('--mode', choices=['rehearsal', 'production'], default='rehearsal')
    parser.add_argument('--approvals', type=Path, default=ROOT / 'deploy/cutover-gate.example.json')
    parser.add_argument('--allow-dirty-rehearsal', action='store_true')
    try:
        prepare(parser.parse_args())
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        print('Release preparation: FAIL; ' + str(error))
        return 1
    return 0


if __name__ == '__main__': raise SystemExit(main())
