import urllib.request
import json
import re
import os
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed

sys.stdout.reconfigure(encoding='utf-8')

HEADERS = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}

def fetch_item_name(item_id):
    url = f'https://bdocodex.com/tip.php?id=item--{item_id}&l=pt'
    req = urllib.request.Request(url, headers=HEADERS)
    try:
        with urllib.request.urlopen(req, timeout=10) as r:
            html = r.read().decode('utf-8')
            m = re.search(r'class="item_title[^"]*"[^>]*><b>([^<]+)</b>', html)
            if m:
                return item_id, m.group(1).strip()
    except Exception:
        pass
    return item_id, f"Item #{item_id}"

def main():
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    data_dir = os.path.join(base_dir, 'data')
    rec_path = os.path.join(data_dir, 'bdo_culinary_recipes.json')
    dict_path = os.path.join(data_dir, 'bdo_items_dict.json')

    if not os.path.exists(rec_path):
        print("Arquivo de receitas não encontrado!")
        return

    with open(rec_path, 'r', encoding='utf-8') as f:
        recipes = json.load(f)

    # Collect all unique item IDs
    all_item_ids = set()
    for r in recipes:
        for m in r['materials']:
            all_item_ids.add(m['item_id'])
        for p in r['products']:
            all_item_ids.add(p['item_id'])

    print(f"Total de itens únicos a traduzir/identificar: {len(all_item_ids)}")

    # Load existing dict if available
    item_dict = {}
    if os.path.exists(dict_path):
        try:
            with open(dict_path, 'r', encoding='utf-8') as f:
                item_dict = {int(k): v for k, v in json.load(f).items()}
        except Exception:
            pass

    to_fetch = [i for i in all_item_ids if i not in item_dict or item_dict[i].startswith("Item #")]
    print(f"Itens pendentes para buscar: {len(to_fetch)}")

    if to_fetch:
        with ThreadPoolExecutor(max_workers=16) as executor:
            future_to_id = {executor.submit(fetch_item_name, item_id): item_id for item_id in to_fetch}
            completed = 0
            for future in as_completed(future_to_id):
                item_id, name = future.result()
                item_dict[item_id] = name
                completed += 1
                if completed % 50 == 0 or completed == len(to_fetch):
                    print(f"Progresso: {completed}/{len(to_fetch)} itens identificados...")

        with open(dict_path, 'w', encoding='utf-8') as f:
            json.dump({str(k): v for k, v in sorted(item_dict.items())}, f, ensure_ascii=False, indent=2)

    # Enrich recipes
    enriched_recipes = []
    for r in recipes:
        enriched_mats = []
        for m in r['materials']:
            item_name = item_dict.get(m['item_id'], f"Item #{m['item_id']}")
            enriched_mats.append({
                'item_id': m['item_id'],
                'name': item_name,
                'qty': m['qty']
            })

        enriched_prods = []
        for p in r['products']:
            item_name = item_dict.get(p['item_id'], f"Item #{p['item_id']}")
            enriched_prods.append({
                'item_id': p['item_id'],
                'name': item_name,
                'qty': p['qty']
            })

        enriched_recipes.append({
            'id': r['id'],
            'name': r['name'],
            'category': r['category'],
            'level': r['level'],
            'exp': r['exp'],
            'ingredients': enriched_mats,
            'products': enriched_prods,
            'substitutions': r.get('substitutions', [])
        })

    enriched_output = os.path.join(data_dir, 'bdo_cooking_recipes_enriched_pt.json')
    with open(enriched_output, 'w', encoding='utf-8') as f:
        json.dump(enriched_recipes, f, ensure_ascii=False, indent=2)

    print(f"\nConcluído com sucesso! Receitas totalmente enriquecidas salvas em:\n{enriched_output}")

if __name__ == '__main__':
    main()
