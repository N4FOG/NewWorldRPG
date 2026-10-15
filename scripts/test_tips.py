import urllib.request
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
headers = {'User-Agent': 'Mozilla/5.0'}
test_ids = [9018, 9017, 9002, 9001, 7906, 9059, 7001, 7313]

for item_id in test_ids:
    url = f'https://bdocodex.com/tip.php?id=item--{item_id}&l=pt'
    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=5) as r:
            html = r.read().decode('utf-8')
            m = re.search(r'class="item_title[^"]*"[^>]*><b>([^<]+)</b>', html)
            name = m.group(1) if m else 'Not found'
            print(f"{item_id} -> {name}")
    except Exception as e:
        print(f"{item_id} -> Error: {e}")
