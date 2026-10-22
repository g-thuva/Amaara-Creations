"""Reject a Pages artifact configured against localhost or an unspecified API.

This checks public configuration syntax only; it never authenticates or deploys.
"""
import ipaddress
import os
import re
from urllib.parse import urlsplit


def validate(values):
    base = values.get('VITE_PUBLIC_BASE_PATH', '')
    if not re.fullmatch(r'/(?:[A-Za-z0-9_-]+/)*', base) or values.get('VITE_ROUTER_MODE') != 'hash':
        raise ValueError('Pages requires an explicit safe base path and hash routing.')
    urls = {}
    for key in ('VITE_API_BASE_URL', 'VITE_MEDIA_BASE_URL', 'VITE_PUBLIC_SITE_URL'):
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


if __name__ == '__main__':
    try:
        validate(os.environ)
        print('Pages public configuration: PASS (syntax only; verify real API/CORS/cookies separately).')
    except ValueError as error:
        print('Pages public configuration: FAIL; ' + str(error))
        raise SystemExit(2)
