"""Report configuration availability only; never print values or perform cutover."""
from pathlib import Path
import argparse
import json
import os
import re

ROOT = Path(__file__).resolve().parent.parent


def flatten(value, prefix=''):
    result = {}
    if isinstance(value, dict):
        for key, child in value.items():
            result.update(flatten(child, prefix + key + ':'))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            result.update(flatten(child, prefix + str(index) + ':'))
    else:
        result[prefix.rstrip(':')] = value
    return result


def configured(value):
    return isinstance(value, str) and bool(value.strip()) and not re.search(
        r'(?i)(replace|placeholder|example\.|\.example|localhost|127\.0\.0\.1|<|\{\{)', value)


def read(path):
    return flatten(json.loads(path.read_text(encoding='utf-8-sig'))) if path.is_file() else {}


def inspect(approvals_path):
    development = {}
    secrets_root = Path(os.environ['APPDATA']) / 'Microsoft' / 'UserSecrets' if os.environ.get('APPDATA') else Path.home() / '.microsoft' / 'usersecrets'
    secrets = secrets_root / 'amaara-creations-be-dev' / 'secrets.json'
    for path in [secrets, ROOT / 'be/appsettings.Development.Local.json']:
        development.update(read(path))
    production = {}
    production_files = [ROOT / 'be/appsettings.Production.json', ROOT / 'be/appsettings.Production.Local.json']
    for path in production_files:
        production.update(read(path))
    environment = {key.replace('__', ':'): value for key, value in os.environ.items() if '__' in key}
    environments = {os.environ[key] for key in ['ASPNETCORE_ENVIRONMENT', 'DOTNET_ENVIRONMENT'] if os.environ.get(key)}
    explicit_production = environments == {'Production'}
    if explicit_production:
        production.update(environment)
    development.update(environment)
    approvals = json.loads(approvals_path.read_text(encoding='utf-8-sig'))
    required = json.loads((ROOT / 'deploy/cutover-gate.example.json').read_text(encoding='utf-8'))
    approval_complete = set(approvals) == set(required) and all(approvals.get(key) is True for key in required)
    approved = lambda key: approvals.get(key) is True
    present = lambda key: configured(production.get(key))
    checks = {
        'productionDatabaseConfigured': present('ConnectionStrings:DefaultConnection') and approved('productionDatabaseApproved'),
        'productionHostingIdentified': approved('hostingApproved'),
        'productionDomainConfigured': present('PublicSite:BaseUrl') and approved('domainApproved'),
        'productionTlsVerified': approved('tlsVerified'),
        'zahlsLiveInstance': production.get('Zahls:Mode') == 'LIVE' and present('Zahls:InstanceName'),
        'zahlsLiveApiKey': production.get('Zahls:Mode') == 'LIVE' and present('Zahls:ApiKey'),
        'zahlsLiveSigningKey': production.get('Zahls:Mode') == 'LIVE' and present('Zahls:WebhookSigningKey'),
        'liveChfPspApproved': approved('zahlsProductionPspApproved') and approved('chfPaymentMethodsVerified'),
        'productionEmailConfigured': all(present('Email:' + key) for key in ['Host', 'Username', 'Password', 'FromEmail']) and approved('emailAccepted'),
        'productionPrivateStorageAccepted': approved('privateStorageAccepted'),
        'monitoringDestinationApproved': approved('monitoringDestinationApproved'),
        'backupDestinationApproved': approved('backupDestinationApproved'),
        'shippingApproved': approved('shippingApproved'),
        'taxApproved': approved('taxApproved'),
        'captureApproved': approved('captureApproved'),
        'refundReturnsApproved': approved('refundReturnPolicyApproved'),
        'zahlsTestCredentialsAvailable': all(configured(development.get('Zahls:' + key)) for key in ['InstanceName', 'ApiKey', 'WebhookSigningKey']),
    }
    # The current adapter deliberately implements TEST only. Inputs alone cannot authorize LIVE.
    return {'configurationValuesSuppressed': True, 'productionEnvironmentExplicit': explicit_production,
            'approvalSchemaComplete': approval_complete, 'liveAdapterImplemented': False,
            'availability': checks, 'productionCutoverAllowed': False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--approvals', type=Path, default=ROOT / 'deploy/cutover-gate.example.json')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    report = inspect(args.approvals)
    for key, value in report['availability'].items():
        print(f'{key}: {"YES" if value else "NO"}')
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('Availability inspection only; no deployment, migration, message or provider request was performed.')
    return 0 if report['productionCutoverAllowed'] else 2


if __name__ == '__main__':
    raise SystemExit(main())
