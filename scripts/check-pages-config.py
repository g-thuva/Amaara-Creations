"""Reject a Pages artifact configured against localhost or an unspecified API.

This checks public configuration syntax only; it never authenticates or deploys.
"""
import argparse
import ipaddress
import os
from pathlib import Path
import re
from urllib.parse import urlsplit

PUBLIC_URLS = ('VITE_API_BASE_URL', 'VITE_MEDIA_BASE_URL', 'VITE_PUBLIC_SITE_URL', 'VITE_MANAGEMENT_LOGIN_URL')


def validate(values):
    base = values.get('VITE_PUBLIC_BASE_PATH', '')
    if not re.fullmatch(r'/(?:[A-Za-z0-9_-]+/)*', base) or values.get('VITE_ROUTER_MODE') != 'hash':
        raise ValueError('Pages requires an explicit safe base path and hash routing.')
    urls = {}
    for key in PUBLIC_URLS:
        value = values.get(key, '')
        url = urlsplit(value)
        host = (url.hostname or '').lower()
        if url.scheme != 'https' or not re.fullmatch(r'[a-z0-9.-]+', host) or url.username or url.password or url.query or url.fragment:
            raise ValueError(key + ' requires an explicit approved public HTTPS URL.')
        if host == 'localhost' or host.endswith(('.localhost', '.local', '.test', '.invalid', '.example', '.example.com', '.example.org', '.example.net')) or host in ('example.com', 'example.org', 'example.net'):
            raise ValueError(key + ' cannot use a local or example endpoint.')
        try:
            address = ipaddress.ip_address(host)
        except ValueError:
            address = None
        if address is not None and not address.is_global:
            raise ValueError(key + ' cannot use a private or loopback address.')
        if any(character in value for character in ('\\', '<', '>', '{', '}')) or any(ord(character) <= 32 for character in value):
            raise ValueError(key + ' contains a placeholder or unsafe character.')
        urls[key] = url
    if urls['VITE_API_BASE_URL'].hostname.endswith('github.io') or not urls['VITE_API_BASE_URL'].path.rstrip('/').endswith('/api/v1'):
        raise ValueError('Pages must use a real API host with the existing /api/v1 prefix.')
    if urls['VITE_PUBLIC_SITE_URL'].path.rstrip('/') != base.rstrip('/'):
        raise ValueError('Public site URL and Pages base path must agree.')
    management = urls['VITE_MANAGEMENT_LOGIN_URL']
    storefront = urls['VITE_PUBLIC_SITE_URL']
    if (management.hostname, management.port or 443) == (storefront.hostname, storefront.port or 443):
        raise ValueError('Management sign-in must use a separate approved origin.')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--github-preflight', action='store_true', help='Report whether public deployment is configured; skip publishing when URLs are missing.')
    args = parser.parse_args(argv)
    try:
        if args.github_preflight:
            missing = [key for key in PUBLIC_URLS if not os.environ.get(key, '').strip()]
            if not missing:
                validate(os.environ)
            ready = not missing
            message = ('Pages deployment skipped: public API, media and management hosting must be configured. Missing variables: ' + ', '.join(missing) + '.') if missing else 'Pages deployment enabled: all public URL settings passed validation.'
            if os.environ.get('GITHUB_OUTPUT'):
                with Path(os.environ['GITHUB_OUTPUT']).open('a', encoding='utf-8') as output:
                    output.write(f'ready={str(ready).lower()}\n')
            if os.environ.get('GITHUB_STEP_SUMMARY'):
                with Path(os.environ['GITHUB_STEP_SUMMARY']).open('a', encoding='utf-8') as summary:
                    summary.write('## GitHub Pages deployment\n\n' + message + '\n')
            print(message)
            return 0
        validate(os.environ)
        print('Pages public configuration: PASS (syntax only; verify real API/CORS/cookies separately).')
        return 0
    except ValueError as error:
        print('Pages public configuration: FAIL; ' + str(error))
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
