"""Shared release validation. No deployment, database access or secret discovery."""
from pathlib import Path
import hashlib
import json
import re
from urllib.parse import urlsplit

PUBLIC_KEYS = {'VITE_PUBLIC_BASE_PATH', 'VITE_API_BASE_URL', 'VITE_MEDIA_BASE_URL', 'VITE_PUBLIC_SITE_URL', 'VITE_ROUTER_MODE'}
LOOPBACK = re.compile(r'https?://(?:localhost|127(?:\.\d{1,3}){3}|\[::1\])(?=[:/\s\"\']|$)', re.I)
SECRET_NAMES = {'key', 'apikey', 'apipassword', 'username', 'password', 'hmackey', 'webhooksigningkey', 'clientsecret', 'secretaccesskey', 'defaultconnection'}


def public_config(values, mode):
    if not isinstance(values, dict) or set(values) != PUBLIC_KEYS or any(not isinstance(value, str) or not value for value in values.values()):
        raise ValueError('Public configuration must contain exactly the five documented nonempty VITE settings.')
    if values['VITE_ROUTER_MODE'] != 'browser' or not re.fullmatch(r'/(?:[A-Za-z0-9_-]+/)*', values['VITE_PUBLIC_BASE_PATH']):
        raise ValueError('Use browser routing and an explicit safe public base path.')
    for key in ['VITE_PUBLIC_SITE_URL', 'VITE_API_BASE_URL', 'VITE_MEDIA_BASE_URL']:
        value = values[key]
        if key != 'VITE_PUBLIC_SITE_URL' and re.fullmatch(r'/[A-Za-z0-9_./~-]*', value) and not value.startswith('//') and '..' not in value:
            continue
        url = urlsplit(value)
        if url.scheme != 'https' or not url.hostname or url.username or url.password or url.query or url.fragment or LOOPBACK.search(value):
            raise ValueError(f'{key} requires an approved HTTPS URL or a safe same-origin resource path.')
        if not re.fullmatch(r'[A-Za-z0-9.-]+', url.hostname) or any(char in value for char in ['{', '}', '<', '>', '\\', ' ', '\n', '\r']):
            raise ValueError('Public URLs must contain resolved, valid hostnames without placeholder or control characters.')
        if mode == 'production' and (url.hostname.endswith(('.test', '.invalid', '.example', '.localhost')) or url.hostname in ['example.com', 'example.org', 'example.net']):
            raise ValueError('Production cannot use a reserved example host.')
    expected = values['VITE_PUBLIC_BASE_PATH'].rstrip('/')
    if urlsplit(values['VITE_PUBLIC_SITE_URL']).path.rstrip('/') != expected:
        raise ValueError('Canonical URL and public base path must describe the same deployment prefix.')
    return values


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def artifact_files(root):
    root = root.resolve()
    if not root.is_dir():
        raise ValueError('Release directory does not exist.')
    files = []
    for path in root.rglob('*'):
        if path.is_symlink():
            raise ValueError('Release artifacts cannot contain symbolic links.')
        if not path.is_file():
            continue
        relative = path.relative_to(root).as_posix()
        parts = [part.lower() for part in path.relative_to(root).parts]
        name = path.name.lower()
        if any(part in {'app_data', 'private-media', 'development-email', 'node_modules', 'uploads', 'test-results', '.git'} for part in parts) or '.local.json' in name or name.startswith('.env') or name == 'appsettings.development.json' or '.example.json' in name or 'be.tests' in name or 'be.releaserehearsal' in name or 'acceptancehost' in name or path.suffix.lower() in {'.pfx', '.p12', '.pem', '.key', '.bak', '.mdf', '.ldf'}:
            raise ValueError('Private/development content found in artifact: ' + relative)
        if path.suffix.lower() in {'.json', '.js', '.mjs', '.css', '.html', '.map', '.sql'}:
            text = path.read_text(encoding='utf-8-sig')
            if LOOPBACK.search(text) or re.search(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|\bghp_[A-Za-z0-9]{36}\b|\bAKIA[0-9A-Z]{16}\b', text):
                raise ValueError('Loopback endpoint or credential material found in artifact: ' + relative)
            if re.search(r'/@vite/client|/@react-refresh|import\.meta\.hot', text):
                raise ValueError('Development runtime found in artifact: ' + relative)
            if name.startswith('appsettings'):
                def check(value):
                    if isinstance(value, dict):
                        for key, item in value.items():
                            if key.lower() in SECRET_NAMES and isinstance(item, str) and item.strip():
                                raise ValueError('Runtime secrets must be supplied outside the release artifact: ' + relative)
                            check(item)
                    elif isinstance(value, list):
                        for item in value: check(item)
                check(json.loads(text))
        files.append(path)
    if not files:
        raise ValueError('Release directory is empty.')
    return sorted(files)


def verify(root):
    root = root.resolve()
    manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
    if not isinstance(manifest, dict) or manifest.get('format') != 1 or manifest.get('mode') not in {'rehearsal', 'production'}:
        raise ValueError('Unsupported release manifest.')
    public_config(manifest['publicConfiguration'], manifest['mode'])
    expected = manifest['files']
    actual = {path.relative_to(root).as_posix(): digest(path) for path in artifact_files(root) if path != root / 'manifest.json'}
    if expected != actual:
        raise ValueError('Artifact membership or SHA-256 integrity does not match the manifest.')
    for required in ['api/be.dll', 'api/appsettings.json', 'web/index.html', 'migrations.sql']:
        if required not in actual or (root / required).stat().st_size == 0:
            raise ValueError('Missing required release payload: ' + required)
    return manifest
