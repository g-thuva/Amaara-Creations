"""Read-only HTTPS smoke probes. Never place orders, send email or call a PSP."""
import argparse
import json
import os
import ssl
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import Request, build_opener, HTTPRedirectHandler, HTTPSHandler


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None  # Never forward a privileged smoke token to a redirect destination.

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--api-origin', required=True)
parser.add_argument('--site-origin', required=True)
parser.add_argument('--product-id', type=int)
parser.add_argument('--content-slug')
parser.add_argument('--authenticated', action='store_true')
parser.add_argument('--admin', action='store_true')
args = parser.parse_args()
for value in [args.api_origin, args.site_origin]:
    uri = urlsplit(value)
    if uri.scheme != 'https' or not uri.hostname or uri.username or uri.password or uri.query or uri.fragment:
        parser.error('Use explicit HTTPS origins without credentials, query or fragment.')
token = os.environ.get('AMAARA_SMOKE_TOKEN')
if (args.authenticated or args.admin) and not token:
    parser.error('Provide the short-lived authorized token through AMAARA_SMOKE_TOKEN; never a command argument.')
checks = [(args.api_origin, '/health/live', 200, False), (args.api_origin, '/health/ready', 200, False),
          (args.api_origin, '/api/v1/products?pageSize=1', 200, False),
          (args.api_origin, '/api/v1/products?search=release-smoke&pageSize=1', 200, False),
          (args.site_origin, '/', 200, False), (args.site_origin, '/products', 200, False), (args.site_origin, '/custom', 200, False)]
if args.product_id: checks.append((args.api_origin, '/api/v1/products/' + str(args.product_id), 200, False))
if args.content_slug:
    from urllib.parse import quote
    checks.append((args.api_origin, '/api/v1/content/pages/' + quote(args.content_slug, safe=''), 200, False))
if args.authenticated:
    checks.extend((args.api_origin, path, 200, True) for path in ['/api/v1/auth/me', '/api/v1/users/profile', '/api/v1/cart', '/api/v1/orders'])
if args.admin:
    checks.extend((args.api_origin, '/api/v1/admin/' + path, 200, True) for path in ['dashboard', 'orders', 'customers', 'inventory', 'custom-designs', 'reports', 'operational-health'])
failed = False
opener = build_opener(NoRedirect(), HTTPSHandler(context=ssl.create_default_context()))
for origin, path, expected, authorized in checks:
    headers = {'Authorization': 'Bearer ' + token} if authorized else {}
    try:
        with opener.open(Request(origin.rstrip('/') + path, headers=headers), timeout=15) as response:
            status = response.status
    except HTTPError as error: status = error.code
    except (URLError, TimeoutError): status = 'unavailable'
    passed = status == expected
    print(json.dumps({'check': path.split('?')[0], 'status': status, 'result': 'PASS' if passed else 'FAIL'}))
    failed |= not passed
print('Read-only smoke finished; checkout quotes, login/password submissions, writes, email and provider transactions require the separate authorized acceptance procedure.')
raise SystemExit(1 if failed else 0)
