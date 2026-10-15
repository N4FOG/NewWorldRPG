import urllib.request
import json
import re
import os
import sys

# Ensure UTF-8 output
sys.stdout.reconfigure(encoding='utf-8')

HEADERS = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}

def fetch_bdo_data(recipe_type, lang='pt'):
    url = f'https://bdocodex.com/query.php?a=recipes&type={recipe_type}&id=1&l={lang}'
    req = urllib.request.Request(url, headers=HEADERS)
    with urllib.request.urlopen(req, timeout=20) as resp:
        raw = resp.read().decode('utf-8-sig')
        return json.loads(raw)['aaData']

def clean_html(raw_html):
    if not raw_html:
        return ''
    clean = re.sub(r'<.*?>', '', raw_html)
    return clean.strip()

def parse_items_from_html(html_str):
    if not html_str:
        return []
    # Pattern to find item links and their quantity badges
    pattern = re.compile(r'href="/[a-z]+/item/(\d+)/"[\s\S]*?<div class="quantity_small nowrap">([^<]+)</div>')
    items = []
    for match in pattern.finditer(html_str):
        item_id = int(match.group(1))
        qty = match.group(2).strip()
        items.append({
            'item_id': item_id,
            'qty': qty
        })
    return items

def main():
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    data_dir = os.path.join(base_dir, 'data')
    os.makedirs(data_dir, exist_ok=True)

    print("=== Baixando receitas de Culinária do Black Desert Online (BDO) ===")
    culinary_rows = fetch_bdo_data('culinary', 'pt')
    print(f"Total de receitas culinárias obtidas: {len(culinary_rows)}")

    recipes = []
    for r in culinary_rows:
        rec_id = r[0]
        title = clean_html(r[2])
        category = r[3]
        level_info = r[4].get('display', '') if isinstance(r[4], dict) else str(r[4])
        exp = r[5]
        weight = r[7]
        materials = parse_items_from_html(r[6])
        products = parse_items_from_html(r[8])
        
        # r[9] contains alternative/compatible ingredient IDs (groups de substituição)
        substitutions = []
        try:
            if len(r) > 9 and r[9]:
                substitutions = json.loads(r[9])
        except Exception:
            pass

        recipes.append({
            'id': rec_id,
            'name': title,
            'category': category,
            'level': level_info,
            'exp': exp,
            'weight': weight,
            'materials': materials,
            'products': products,
            'substitutions': substitutions
        })

    output_path = os.path.join(data_dir, 'bdo_culinary_recipes.json')
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(recipes, f, ensure_ascii=False, indent=2)

    print(f"Sucesso! {len(recipes)} receitas salvas em: {output_path}")

    print("\n=== Baixando receitas de Processamento (Moagem, Secagem, Agitação, etc.) ===")
    try:
        proc_rows = fetch_bdo_data('processing', 'pt')
        print(f"Total de receitas de processamento obtidas: {len(proc_rows)}")
        proc_recipes = []
        for r in proc_rows:
            rec_id = r[0]
            title = clean_html(r[2])
            category = r[3]
            level_info = r[4].get('display', '') if isinstance(r[4], dict) else str(r[4])
            exp = r[5]
            weight = r[7] if len(r) > 7 else '0'
            materials = parse_items_from_html(r[6])
            products = parse_items_from_html(r[8]) if len(r) > 8 else []
            proc_recipes.append({
                'id': rec_id,
                'name': title,
                'category': category,
                'level': level_info,
                'exp': exp,
                'materials': materials,
                'products': products
            })
        proc_output = os.path.join(data_dir, 'bdo_processing_recipes.json')
        with open(proc_output, 'w', encoding='utf-8') as f:
            json.dump(proc_recipes, f, ensure_ascii=False, indent=2)
        print(f"Sucesso! {len(proc_recipes)} receitas de processamento salvas em: {proc_output}")
    except Exception as e:
        print(f"Erro ao baixar processamento: {e}")

if __name__ == '__main__':
    main()
