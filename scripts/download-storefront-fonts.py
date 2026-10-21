"""Refresh licensed Latin font subsets from the official Google Fonts distribution.

The committed files keep production rendering independent of third-party CSS.
"""
from pathlib import Path
import re
import urllib.request

destination = Path(__file__).resolve().parents[1] / 'client/public/fonts'
destination.mkdir(parents=True, exist_ok=True)
agent = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36'
request = urllib.request.Request('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=Poppins:wght@600&display=swap', headers={'User-Agent': agent})
css = urllib.request.urlopen(request, timeout=30).read().decode()
saved = set()
for block in re.findall(r'/\* latin \*/\s*(@font-face\s*\{.*?\})', css, re.S):
    family = re.search(r"font-family:\s*'([^']+)'", block).group(1)
    url = re.search(r'url\((https://fonts.gstatic.com/[^)]+\.woff2)\)', block).group(1)
    name = 'inter-latin.woff2' if family == 'Inter' else 'poppins-semibold-latin.woff2'
    if name in saved:
        continue
    data = urllib.request.urlopen(url, timeout=30).read()
    if data[:4] != b'wOF2':
        raise ValueError('Expected a WOFF2 font')
    (destination / name).write_bytes(data)
    saved.add(name)
    print(f'{name}: {len(data)} bytes')
for family in ['inter', 'poppins']:
    url = f'https://raw.githubusercontent.com/google/fonts/main/ofl/{family}/OFL.txt'
    license_text = urllib.request.urlopen(url, timeout=30).read().decode('utf-8')
    (destination / f'{family}-OFL.txt').write_text('\n'.join(line.rstrip() for line in license_text.splitlines()) + '\n', encoding='utf-8')
if len(saved) != 2:
    raise ValueError('Both required font families must be downloaded')
