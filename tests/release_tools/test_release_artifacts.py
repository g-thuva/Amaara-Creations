import json
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts'))
from release_artifacts import artifact_files, digest, public_config, verify


class ReleaseArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='amaara-release-tests-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.public = json.loads((ROOT / 'deploy/frontend.rehearsal.json').read_text(encoding='utf-8'))

    def package(self):
        for name, text in {'api/be.dll': 'binary fixture', 'api/appsettings.json': '{}', 'web/index.html': '<html></html>', 'migrations.sql': 'SELECT 1;'}.items():
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding='utf-8')
        manifest = {'format': 1, 'mode': 'rehearsal', 'sourceCommit': 'fixture', 'publicConfiguration': self.public,
                    'files': {path.relative_to(self.root).as_posix(): digest(path) for path in artifact_files(self.root) if path != self.root / 'manifest.json'}}
        (self.root / 'manifest.json').write_text(json.dumps(manifest), encoding='utf-8')

    def test_complete_package_verifies(self):
        self.package()
        self.assertEqual('fixture', verify(self.root)['sourceCommit'])

    def test_tampering_missing_payload_and_extra_file_fail(self):
        self.package()
        payload = self.root / 'web/index.html'
        payload.write_text('changed', encoding='utf-8')
        with self.assertRaises(ValueError): verify(self.root)
        payload.unlink()
        with self.assertRaises(ValueError): verify(self.root)
        self.package()
        (self.root / 'unexpected.txt').write_text('extra', encoding='utf-8')
        with self.assertRaises(ValueError): verify(self.root)

    def test_private_content_and_runtime_credentials_fail(self):
        self.package()
        for name in ['api/App_Data/private-media/test.png', 'api/appsettings.Production.Local.json', 'web/.env', 'api/be.Tests.dll', 'api/be.ReleaseRehearsal.dll', 'api/secret.pfx', 'api/database.bak']:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('fixture', encoding='utf-8')
            with self.assertRaises(ValueError): artifact_files(self.root)
            path.unlink()
        settings = self.root / 'api/appsettings.json'
        settings.write_text(json.dumps({'Jwt': {'Key': 'not-an-allowed-runtime-secret'}}), encoding='utf-8')
        with self.assertRaises(ValueError): artifact_files(self.root)

    def test_loopback_public_endpoint_fails(self):
        self.package()
        (self.root / 'web/index.html').write_text('<script src="http://localhost:5192/api/v1"></script>', encoding='utf-8')
        with self.assertRaises(ValueError): artifact_files(self.root)

    def test_nested_manifest_cannot_bypass_membership_verification(self):
        self.package()
        (self.root / 'web/manifest.json').write_text('{}', encoding='utf-8')
        with self.assertRaises(ValueError): verify(self.root)

    def test_empty_or_missing_directory_fails(self):
        with self.assertRaises(ValueError): artifact_files(self.root)
        with self.assertRaises(ValueError): artifact_files(self.root / 'missing')

    def test_production_rejects_example_hosts_and_unknown_public_settings(self):
        with self.assertRaises(ValueError): public_config(self.public, 'production')
        with self.assertRaises(ValueError): public_config({**self.public, 'VITE_PRIVATE_API_KEY': 'not-allowed'}, 'rehearsal')

    def test_safe_paths_and_canonical_prefix_must_match(self):
        for change in [{'VITE_API_BASE_URL': '//foreign.invalid'}, {'VITE_API_BASE_URL': '/api bad'}, {'VITE_PUBLIC_BASE_PATH': '/../'}, {'VITE_PUBLIC_SITE_URL': 'https://user:pass@store.example.test'}, {'VITE_PUBLIC_BASE_PATH': '/shop/'}, {'VITE_PUBLIC_SITE_URL': 'https://{{APP_HOST}}'}]:
            with self.assertRaises(ValueError): public_config({**self.public, **change}, 'rehearsal')


if __name__ == '__main__': unittest.main()
