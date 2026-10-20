"""Verify a prepared release's exact membership, integrity and privacy."""
from pathlib import Path
import argparse
from release_artifacts import verify

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('directory', type=Path)
args = parser.parse_args()
try:
    manifest = verify(args.directory)
    print(f'Release verification: PASS; mode={manifest["mode"]}; source={manifest["sourceCommit"]}; files={len(manifest["files"])}')
except (ValueError, OSError, KeyError) as error:
    print('Release verification: FAIL; ' + str(error))
    raise SystemExit(1)
