import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('pages_config', Path(__file__).resolve().parents[2] / 'scripts/check-pages-config.py')
pages = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pages)


class PagesConfigurationTests(unittest.TestCase):
    def values(self):
        # Syntactic fixture only. No request or assumption that GitHub is our API.
        return {'VITE_API_BASE_URL': 'https://api.github.com/api/v1', 'VITE_MEDIA_BASE_URL': 'https://api.github.com', 'VITE_PUBLIC_SITE_URL': 'https://g-thuva.github.io/Amaara-Creations', 'VITE_PUBLIC_BASE_PATH': '/Amaara-Creations/', 'VITE_ROUTER_MODE': 'hash', 'VITE_MANAGEMENT_LOGIN_URL': 'https://github.com/login'}

    def test_explicit_public_https_configuration(self):
        pages.validate(self.values())

    def test_missing_and_local_api_are_rejected(self):
        for endpoint in ('', 'http://localhost:5192/api/v1', 'https://localhost/api/v1', 'https://127.0.0.1/api/v1', 'https://10.0.0.1/api/v1', '/api/v1'):
            with self.subTest(endpoint=endpoint), self.assertRaises(ValueError):
                pages.validate({**self.values(), 'VITE_API_BASE_URL': endpoint})

    def test_static_pages_cannot_be_used_as_the_api(self):
        with self.assertRaises(ValueError):
            pages.validate({**self.values(), 'VITE_API_BASE_URL': 'https://g-thuva.github.io/api/v1'})

    def test_credentials_and_unsafe_or_mismatched_routing_are_rejected(self):
        for update in ({'VITE_API_BASE_URL': 'https://user:password@api.github.com/api/v1'}, {'VITE_ROUTER_MODE': 'browser'}, {'VITE_PUBLIC_SITE_URL': 'https://g-thuva.github.io/wrong'}, {'VITE_API_BASE_URL': 'https://api.example.test/api/v1'}):
            with self.subTest(update=update), self.assertRaises(ValueError):
                pages.validate({**self.values(), **update})

    def test_management_entry_is_required_https_and_separate(self):
        for endpoint in ('', 'http://localhost:5179/login', 'http://staff.github.com/login', 'https://g-thuva.github.io/login', 'https://g-thuva.github.io:443/login', 'https://github.com/login?token=secret', 'https://github.com/login#token', 'https://user:password@github.com/login'):
            with self.subTest(endpoint=endpoint), self.assertRaises(ValueError):
                pages.validate({**self.values(), 'VITE_MANAGEMENT_LOGIN_URL': endpoint})
