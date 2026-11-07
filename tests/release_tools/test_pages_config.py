import importlib.util
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('pages_config', Path(__file__).resolve().parents[2] / 'scripts/check-pages-config.py')
pages = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pages)


def public_values():
    # Syntactic fixtures only; these hosts are never contacted or deployed.
    return {'VITE_API_BASE_URL': 'https://api.github.com/api/v1', 'VITE_MEDIA_BASE_URL': 'https://api.github.com', 'VITE_PUBLIC_SITE_URL': 'https://g-thuva.github.io/Amaara-Creations', 'VITE_PUBLIC_BASE_PATH': '/Amaara-Creations/', 'VITE_ROUTER_MODE': 'hash', 'VITE_MANAGEMENT_LOGIN_URL': 'https://github.com/login'}


class PagesConfigurationTests(unittest.TestCase):
    def values(self):
        return public_values()

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


class PagesPreflightTests(unittest.TestCase):
    def run_check(self, values, preflight=True):
        with tempfile.TemporaryDirectory(prefix='amaara-pages-policy-') as directory:
            output = Path(directory) / 'output'
            summary = Path(directory) / 'summary'
            environment = {key: value for key, value in os.environ.items() if not key.startswith('VITE_') and key not in ('GITHUB_OUTPUT', 'GITHUB_STEP_SUMMARY')}
            environment.update(values, GITHUB_OUTPUT=str(output), GITHUB_STEP_SUMMARY=str(summary))
            command = [sys.executable, str(Path(pages.__file__))]
            if preflight:
                command.append('--github-preflight')
            result = subprocess.run(command, env=environment, capture_output=True, text=True, timeout=15)
            return result, output.read_text(encoding='utf-8') if output.exists() else '', summary.read_text(encoding='utf-8') if summary.exists() else ''

    def test_unconfigured_deployment_is_skipped_and_explains_each_missing_variable(self):
        result, output, summary = self.run_check({})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('ready=false\n', output)
        self.assertIn('Pages deployment skipped', result.stdout)
        for key in pages.PUBLIC_URLS:
            self.assertIn(key, summary)

    def test_storefront_alone_does_not_enable_deployment(self):
        result, output, summary = self.run_check({'VITE_PUBLIC_SITE_URL': public_values()['VITE_PUBLIC_SITE_URL']})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('ready=false\n', output)
        self.assertNotIn('VITE_PUBLIC_SITE_URL', summary)
        self.assertIn('VITE_API_BASE_URL', summary)

    def test_blank_variables_are_treated_as_unconfigured(self):
        result, output, summary = self.run_check({**public_values(), 'VITE_MEDIA_BASE_URL': '  '})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('ready=false\n', output)
        self.assertIn('VITE_MEDIA_BASE_URL', summary)

    def test_complete_valid_configuration_enables_deployment(self):
        result, output, summary = self.run_check(public_values())
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('ready=true\n', output)
        self.assertIn('all public URL settings passed validation', summary)

    def test_complete_invalid_configuration_fails_without_enabling_deployment(self):
        for update in ({'VITE_API_BASE_URL': 'http://localhost:5192/api/v1'}, {'VITE_MANAGEMENT_LOGIN_URL': 'https://g-thuva.github.io/login'}, {'VITE_ROUTER_MODE': 'browser'}):
            with self.subTest(update=update):
                result, output, summary = self.run_check({**public_values(), **update})
                self.assertEqual(2, result.returncode, result.stderr)
                self.assertNotIn('ready=true', output)
                self.assertIn('Pages public configuration: FAIL', result.stdout)
                self.assertNotIn('deployment enabled', summary)

    def test_actual_build_guard_still_rejects_missing_configuration(self):
        result, output, _ = self.run_check({}, preflight=False)
        self.assertEqual(2, result.returncode, result.stderr)
        self.assertIn('Pages public configuration: FAIL', result.stdout)
        self.assertEqual('', output)
