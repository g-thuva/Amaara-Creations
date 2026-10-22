import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import unicodedata

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('build_password_blocklist', ROOT / 'scripts/build-password-blocklist.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PasswordBlocklistPreparationTests(unittest.TestCase):
    def test_hashes_normalized_whole_passwords_without_copying_plaintext(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'approved-test-corpus.txt'
            password = 'a public example cafe\u0301 passphrase'
            source.write_text(password + '\n' + password + '\nshort\n', encoding='utf-8')
            output = root / 'private-corpus-v1'
            self.assertEqual(module.build(source, output), 1)
            manifest = json.loads((output / 'manifest.json').read_text())
            expected = hashlib.sha256(unicodedata.normalize('NFC', password).encode()).digest()
            prefix = expected[:2].hex()
            self.assertEqual((output / prefix[:2] / (prefix[2:] + '.bin')).read_bytes(), expected)
            self.assertEqual(manifest['buckets'][prefix]['count'], 1)
            self.assertFalse((output / '.building').exists())
            with self.assertRaises(ValueError):
                module.build(source, output)

    def test_empty_eligible_corpus_does_not_create_an_active_manifest(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'test-corpus.txt'
            source.write_text('short\n', encoding='utf-8')
            output = root / 'private-corpus-v1'
            with self.assertRaises(ValueError):
                module.build(source, output)
            self.assertFalse((output / 'manifest.json').exists())


if __name__ == '__main__':
    unittest.main()
