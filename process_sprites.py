import os
from PIL import Image, ImageOps

source_files = {
    "player_woodsman.png": r"C:\Users\n4fog\.gemini\antigravity\brain\2713a7dc-db9c-4108-b770-bb63611cdafe\player_woodsman_1792023681586.jpg",
    "tree_birch.png": r"C:\Users\n4fog\.gemini\antigravity\brain\2713a7dc-db9c-4108-b770-bb63611cdafe\tree_birch_1792023700022.jpg",
    "rock_boulder.png": r"C:\Users\n4fog\.gemini\antigravity\brain\2713a7dc-db9c-4108-b770-bb63611cdafe\rock_boulder_1792023719306.jpg",
    "tree_pine.png": r"C:\Users\n4fog\.gemini\antigravity\brain\2713a7dc-db9c-4108-b770-bb63611cdafe\tree_pine_1792023742886.jpg"
}

dest_dir = r"k:\UnRealWorld\urw_reborn\assets\sprites"
os.makedirs(dest_dir, exist_ok=True)

def remove_background(img, tolerance=25):
    # Converte para RGBA
    img = img.convert("RGBA")
    w, h = img.size
    
    # Obter a cor de fundo média dos 4 cantos
    corners = [img.getpixel((0, 0)), img.getpixel((w-1, 0)), img.getpixel((0, h-1)), img.getpixel((w-1, h-1))]
    bg_r = sum(c[0] for c in corners) // 4
    bg_g = sum(c[1] for c in corners) // 4
    bg_b = sum(c[2] for c in corners) // 4
    
    # Criar mascara de transparencia baseada em proximidade com branco/fundo
    datas = img.getdata()
    new_data = []
    
    for item in datas:
        r, g, b, a = item
        # Se for muito próximo de branco ou da cor dos cantos
        is_bg = (r >= 235 and g >= 235 and b >= 235) or (
            abs(r - bg_r) < tolerance and abs(g - bg_g) < tolerance and abs(b - bg_b) < tolerance
        )
        if is_bg:
            new_data.append((255, 255, 255, 0)) # Transparente
        else:
            new_data.append((r, g, b, 255))
            
    img.putdata(new_data)
    
    # Auto-crop do espaco transparente excedente
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
        
    return img

for name, src in source_files.items():
    if os.path.exists(src):
        print(f"Processando {name} de {src}...")
        orig = Image.open(src)
        processed = remove_background(orig, tolerance=20)
        out_path = os.path.join(dest_dir, name)
        processed.save(out_path, "PNG")
        print(f"Salvo com sucesso: {out_path} (Tamanho: {processed.size})")
    else:
        print(f"Arquivo fonte nao encontrado: {src}")

print("Concluído!")
