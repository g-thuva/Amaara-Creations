"""Build a private offline SHA-256 blocklist from an approved UTF-8 wordlist.

Never use customer passwords as input. Output must be a new versioned directory.
The application only reads hashed, checksummed buckets, not this source list.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import unicodedata


def build(source: Path, output: Path):
    output = output.resolve()
    if output.exists():
        raise ValueError('Choose a new output directory; existing corpora are immutable.')
    output.mkdir(parents=True)
    work = output / '.building'
    work.mkdir()
    streams = {}
    secondary_streams = []
    try:
        with source.open(encoding='utf-8-sig', errors='strict') as words:
            for line in words:
                password = line.rstrip('\r\n')
                if not 15 <= len(password) <= 128:
                    continue
                digest = hashlib.sha256(unicodedata.normalize('NFC', password).encode('utf-8')).digest()
                prefix = f'{digest[0]:02x}'
                if prefix not in streams:
                    streams[prefix] = (work / prefix).open('wb')
                streams[prefix].write(digest)
        for stream in streams.values():
            stream.close()
        manifest = {'format': 1, 'algorithm': 'SHA256', 'normalization': 'NFC', 'buckets': {}}
        for first in sorted(work.iterdir()):
            shards = {}
            with first.open('rb') as hashes:
                while digest := hashes.read(32):
                    second = f'{digest[1]:02x}'
                    if second not in shards:
                        shards[second] = (work / f'{first.name}_{second}').open('wb')
                        secondary_streams.append(shards[second])
                    shards[second].write(digest)
            for stream in shards.values():
                stream.close()
            (output / first.name).mkdir()
            for second in sorted(shards):
                shard = work / f'{first.name}_{second}'
                if shard.stat().st_size > 32_000_000:
                    raise ValueError('A corpus bucket exceeds its supported bound.')
                raw = shard.read_bytes()
                values = {raw[index:index + 32] for index in range(0, len(raw), 32)}
                data = b''.join(sorted(values))
                (output / first.name / (second + '.bin')).write_bytes(data)
                manifest['buckets'][first.name + second] = {'count': len(values), 'sha256': hashlib.sha256(data).hexdigest()}
        if not manifest['buckets']:
            raise ValueError('The source has no eligible passwords; no active manifest was created.')
        (output / 'manifest.json').write_text(json.dumps(manifest, sort_keys=True) + '\n', encoding='utf-8')
    finally:
        for stream in [*streams.values(), *secondary_streams]:
            stream.close()
        # This directory was created above, under the explicit new output root.
        if work.resolve().parent != output or work.name != '.building':
            raise ValueError('Refusing unexpected temporary cleanup path.')
        shutil.rmtree(work)
    return sum(bucket['count'] for bucket in manifest['buckets'].values())


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('approved_wordlist', type=Path)
    parser.add_argument('new_private_output', type=Path)
    args = parser.parse_args()
    count = build(args.approved_wordlist, args.new_private_output)
    print(f'Offline blocklist prepared: {count} distinct candidate passwords; no plaintext copied.')
