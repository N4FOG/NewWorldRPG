import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

def build_wiki():
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    data_dir = os.path.join(base_dir, 'data')
    nordic_path = os.path.join(data_dir, 'nordic_cooking_recipes.json')
    bdo_path = os.path.join(data_dir, 'bdo_cooking_recipes_enriched_pt.json')

    with open(nordic_path, 'r', encoding='utf-8') as f:
        nordic_recipes = json.load(f)

    with open(bdo_path, 'r', encoding='utf-8') as f:
        bdo_recipes = json.load(f)

    # All authentic skills from ini_skills.txt and the original game
    skills_data = [
        # Combate
        {"name": "Esquiva (Dodge)", "group": "Combate", "attribs": "Destreza, Agilidade, Visão", "desc": "Habilidade instintiva de desviar de golpes de machado, flechas de caçadores e investidas ferozes de ursos e lobos na taiga.", "key": "Passiva / Combate"},
        {"name": "Combate Desarmado (Unarmed)", "group": "Combate", "attribs": "Agilidade, Velocidade, Força", "desc": "Luta corpo a corpo com socos, agarramentos e imobilizações. Essencial quando desarmado ou em emboscadas na neve.", "key": "Automática"},
        {"name": "Facas (Knife)", "group": "Combate", "attribs": "Destreza, Agilidade, Tato", "desc": "Precisão letal com lâminas curtas de ferro ou pedra lascada. Também melhora o rendimento ao esfolar caças e fatiar alimentos.", "key": "Automática"},
        {"name": "Espadas (Sword)", "group": "Combate", "attribs": "Força, Destreza, Agilidade", "desc": "Uso de lâminas nórdicas forjadas. Arma de prestígio importada por comerciantes estrangeiros, devastadora contra inimigos com armadura.", "key": "Automática"},
        {"name": "Machados (Axe)", "group": "Combate", "attribs": "Força, Agilidade", "desc": "A alma do guerreiro e lenhador da floresta. Causa fraturas ósseas terríveis e sangramento profuso em feras e invasores.", "key": "Automática"},
        {"name": "Lanças (Spear)", "group": "Combate", "attribs": "Força, Agilidade, Agilidade", "desc": "Arma defensiva e de caça com alcance supremo. Permite empalar alces e javalis antes que possam se aproximar.", "key": "Automática"},
        {"name": "Clavas e Porretes (Club)", "group": "Combate", "attribs": "Força, Força, Agilidade", "desc": "Armas de impacto contundente feitas de madeira pesada ou pedras amarradas. Esmaga ossos e atordoa oponentes.", "key": "Automática"},
        {"name": "Arquearia (Bow)", "group": "Combate", "attribs": "Força, Destreza, Visão", "desc": "A arte máxima de caça finlandesa. Permite abater presas a grandes distâncias antes que sintam o cheiro do caçador.", "key": "Automática"},
        {"name": "Bestas (Crossbow)", "group": "Combate", "attribs": "Destreza, Visão, Tato", "desc": "Mecanismo de alta penetração. Disparo horizontal reto e potente, ideal para tiros emboscados.", "key": "Automática"},
        {"name": "Escudo (Shield)", "group": "Combate", "attribs": "Força, Destreza, Agilidade", "desc": "Aparar golpes frontais, flechas e mordidas com escudos redondos de bétula cobertos por couro cru.", "key": "Passiva"},

        # Sobrevivência e Tradição (Lore)
        {"name": "Culinária & Processamento (Cookery)", "group": "Sobrevivência", "attribs": "Inteligência, Tato, Olfato", "desc": "Preparo de sopas, ensopados, pães e queijos. Reduz desperdício de comida, extrai sal por evaporação e purifica toxinas mortais de fungos.", "key": "C"},
        {"name": "Botânica & Ervas (Herblore)", "group": "Sobrevivência", "attribs": "Inteligência, Visão, Olfato", "desc": "Identificação de bagas, fungos comestíveis, cascas medicinais e plantas venenosas da taiga.", "key": "Passiva / Menu"},
        {"name": "Agricultura (Agriculture)", "group": "Sobrevivência", "attribs": "Inteligência, Força, Paciência", "desc": "Preparo do solo, semeadura de centeio, cevada e nabos, colheita e armazenamento de sementes para o inverno.", "key": "Menu"},
        {"name": "Pesca (Fishing)", "group": "Sobrevivência", "attribs": "Destreza, Visão, Tato", "desc": "Pesca com linha, anzol de osso ou arpão em rios e lagos boreais congelados (pesca no gelo).", "key": "Menu"},
        {"name": "Confecção de Redes (Netmaking)", "group": "Sobrevivência", "attribs": "Destreza, Tato, Paciência", "desc": "Tecelagem de redes de pesca passivas a partir de cascas e fibras de cânhamo.", "key": "Menu"},
        {"name": "Rastreamento (Tracking)", "group": "Sobrevivência", "attribs": "Visão, Inteligência, Olfato", "desc": "Identificação da idade e direção de pegadas na neve fresca e no musgo, além do sangue de presas feridas.", "key": "Automática"},
        {"name": "Armadilhas (Trapping)", "group": "Sobrevivência", "attribs": "Inteligência, Destreza", "desc": "Construção de fossos com estacas, laços de tendão e alçapões para capturar lebres, alces e raposas passivamente.", "key": "Menu"},
        {"name": "Curtimento de Peles (Hideworking)", "group": "Sobrevivência", "attribs": "Força, Tato, Paciência", "desc": "Raspagem de gordura, curtimento com cinzas/sal e amaciamento de couro para confecção de roupas quentes.", "key": "Menu"},
        {"name": "Lenharia (Timbercraft)", "group": "Sobrevivência", "attribs": "Força, Destreza", "desc": "Derrubada eficiente de árvores com machado, evitando que o tronco caia sobre o lenhador e maximizando toretes de lenha.", "key": "Botão Direito"},
        {"name": "Construção (Building)", "group": "Sobrevivência", "attribs": "Força, Inteligência", "desc": "Edificação de cabanas de toras entalhadas (log cabins), saunas, telhados de bétula e abrigos simples.", "key": "Menu"},
        {"name": "Carpintaria (Carpentry)", "group": "Sobrevivência", "attribs": "Destreza, Tato", "desc": "Entalhe em madeira: colheres, tigelas, batedores de manteiga, pás e recipientes para alimentos.", "key": "Menu"},
        {"name": "Arquearia Artesanal (Bowyer)", "group": "Sobrevivência", "attribs": "Destreza, Tato, Força", "desc": "Criação e manutenção de arcos nórdicos recurvados, cordas de tendão e flechas emplumadas.", "key": "Menu"},
        {"name": "Medicina Tradicional (Physician)", "group": "Sobrevivência", "attribs": "Inteligência, Tato, Vontade", "desc": "Tratamento de cortes profundos, fixação de fraturas ósseas, ataduras com musgo esfagno e combate a infecções.", "key": "Menu"},
        {"name": "Previsão do Tempo (Weatherlore)", "group": "Sobrevivência", "attribs": "Visão, Olfato, Inteligência", "desc": "Leitura das nuvens, direção do vento e comportamento dos animais para prever nevascas rigorosas e degelo.", "key": "Passiva"},
        {"name": "Alfaiataria & Fibras (Clothcraft)", "group": "Sobrevivência", "attribs": "Destreza, Tato", "desc": "Costura de casacos de pele, botas forradas, gorros de lã e manoplas essenciais para evitar hipotermia.", "key": "Menu"},

        # Habilidades Físicas
        {"name": "Furtividade (Stealth)", "group": "Físico", "attribs": "Agilidade, Destreza, Visão", "desc": "Caminhar silencioso na vegetação rasteira e camuflagem contra o vento para aproximar-se de presas sem espantá-las.", "key": "Menu / Passiva"},
        {"name": "Escalada (Climbing)", "group": "Físico", "attribs": "Força, Agilidade, Tato", "desc": "Subir encostas rochosas, falésias e árvores para vigiar a taiga ou escapar de alcateias de lobos.", "key": "Automática"},
        {"name": "Esqui Nórdico (Skiing)", "group": "Físico", "attribs": "Agilidade, Equilíbrio, Velocidade", "desc": "Deslocamento veloz sobre neve fofa no inverno boreal com pranchas de madeira e bastão único.", "key": "Automática"},
        {"name": "Natação (Swimming)", "group": "Físico", "attribs": "Força, Resistência, Vontade", "desc": "Cruzar rios e lagos sem congelar ou se afogar pelo peso dos equipamentos e roupas encharcadas.", "key": "Automática"}
    ]

    html_content = f"""<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Wiki Oficial: UnReal World Reborn | Manual, Culinária, Skills & Mecânicas</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@500;700;900&family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {{
            --bg-base: #0c0f14;
            --bg-card: #151b24;
            --bg-card-hover: #1c2432;
            --bg-header: #10151f;
            --border-color: #2b3648;
            --border-gold: #c59f42;
            --text-main: #e2e7ef;
            --text-muted: #95a3b8;
            --gold: #f5c862;
            --gold-bright: #ffdf85;
            --orange: #ff7b38;
            --green: #48bb78;
            --red: #f56565;
            --blue: #4299e1;
            --cyan: #38bdf8;
            --font-display: 'Cinzel', serif;
            --font-body: 'Inter', sans-serif;
        }}

        * {{
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }}

        body {{
            background-color: var(--bg-base);
            color: var(--text-main);
            font-family: var(--font-body);
            line-height: 1.6;
            background-image: 
                radial-gradient(circle at 15% 15%, rgba(197, 159, 66, 0.05) 0%, transparent 40%),
                radial-gradient(circle at 85% 85%, rgba(66, 153, 225, 0.04) 0%, transparent 40%);
            background-attachment: fixed;
            display: flex;
            flex-direction: column;
            min-height: 100vh;
        }}

        /* Scrollbar */
        ::-webkit-scrollbar {{
            width: 8px;
            height: 8px;
        }}
        ::-webkit-scrollbar-track {{
            background: #090c10;
        }}
        ::-webkit-scrollbar-thumb {{
            background: #232e3f;
            border-radius: 4px;
            border: 1px solid #33435c;
        }}
        ::-webkit-scrollbar-thumb:hover {{
            background: var(--gold);
        }}

        /* Header Principal */
        header.top-header {{
            position: sticky;
            top: 0;
            z-index: 100;
            background: rgba(16, 21, 31, 0.95);
            backdrop-filter: blur(10px);
            border-bottom: 2px solid var(--border-gold);
            padding: 12px 30px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.6);
        }}

        .logo-area {{
            display: flex;
            align-items: center;
            gap: 15px;
        }}
        .logo-icon {{
            font-size: 2rem;
            filter: drop-shadow(0 0 10px rgba(245, 200, 98, 0.5));
        }}
        .logo-titles h1 {{
            font-family: var(--font-display);
            font-size: 1.45rem;
            font-weight: 700;
            color: var(--gold-bright);
            letter-spacing: 1px;
            line-height: 1.2;
        }}
        .logo-titles span {{
            font-size: 0.75rem;
            color: var(--text-muted);
            text-transform: uppercase;
            letter-spacing: 2px;
        }}

        .search-container {{
            position: relative;
            width: 380px;
        }}
        .search-container input {{
            width: 100%;
            background: #090d14;
            border: 1px solid var(--border-color);
            border-radius: 6px;
            padding: 10px 16px 10px 38px;
            color: var(--text-main);
            font-size: 0.9rem;
            outline: none;
            transition: all 0.2s ease;
        }}
        .search-container input:focus {{
            border-color: var(--gold);
            box-shadow: 0 0 12px rgba(245, 200, 98, 0.25);
        }}
        .search-icon {{
            position: absolute;
            left: 12px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--text-muted);
            font-size: 0.95rem;
        }}

        .header-badges {{
            display: flex;
            gap: 10px;
        }}
        .stat-badge {{
            background: #111822;
            border: 1px solid #233042;
            padding: 4px 10px;
            border-radius: 4px;
            font-size: 0.75rem;
            display: flex;
            align-items: center;
            gap: 6px;
            color: var(--gold);
        }}

        /* Layout Container */
        .wiki-container {{
            display: flex;
            flex: 1;
            max-width: 1680px;
            width: 100%;
            margin: 0 auto;
            padding: 24px 30px;
            gap: 30px;
        }}

        /* Barra Lateral de Navegação (Sticky) */
        nav.wiki-sidebar {{
            width: 280px;
            flex-shrink: 0;
            position: sticky;
            top: 90px;
            height: calc(100vh - 120px);
            overflow-y: auto;
            background: var(--bg-card);
            border: 1px solid var(--border-color);
            border-radius: 8px;
            padding: 16px;
        }}
        .nav-group-title {{
            font-family: var(--font-display);
            font-size: 0.8rem;
            color: var(--gold);
            text-transform: uppercase;
            letter-spacing: 1.5px;
            padding: 8px 12px;
            margin-top: 14px;
            border-bottom: 1px solid rgba(197, 159, 66, 0.2);
        }}
        .nav-group-title:first-child {{
            margin-top: 0;
        }}
        .nav-item {{
            display: flex;
            align-items: center;
            gap: 10px;
            padding: 9px 12px;
            color: var(--text-muted);
            text-decoration: none;
            font-size: 0.86rem;
            border-radius: 6px;
            transition: all 0.15s ease;
            margin: 2px 0;
        }}
        .nav-item:hover, .nav-item.active {{
            background: rgba(245, 200, 98, 0.12);
            color: var(--gold-bright);
            transform: translateX(4px);
        }}

        /* Área de Conteúdo Principal */
        main.wiki-content {{
            flex: 1;
            min-width: 0;
            display: flex;
            flex-direction: column;
            gap: 36px;
        }}

        /* Seções da Wiki */
        .wiki-section {{
            background: var(--bg-card);
            border: 1px solid var(--border-color);
            border-radius: 8px;
            padding: 26px 30px;
            position: relative;
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
        }}
        .section-header {{
            display: flex;
            align-items: center;
            justify-content: space-between;
            border-bottom: 2px solid var(--border-gold);
            padding-bottom: 12px;
            margin-bottom: 20px;
        }}
        .section-title {{
            font-family: var(--font-display);
            font-size: 1.45rem;
            color: var(--gold);
            display: flex;
            align-items: center;
            gap: 12px;
        }}
        .section-tag {{
            font-size: 0.75rem;
            background: rgba(197, 159, 66, 0.15);
            color: var(--gold);
            border: 1px solid var(--gold);
            padding: 2px 8px;
            border-radius: 4px;
            text-transform: uppercase;
        }}

        /* Grid de Controles & Teclas */
        .controls-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 14px;
            margin-top: 15px;
        }}
        .control-card {{
            background: #0e121a;
            border: 1px solid #232d3d;
            border-radius: 6px;
            padding: 14px 18px;
            display: flex;
            align-items: center;
            gap: 16px;
        }}
        .key-badge {{
            background: #1b2433;
            border: 2px solid var(--gold);
            color: var(--gold);
            font-family: monospace;
            font-weight: 700;
            font-size: 1rem;
            min-width: 44px;
            height: 40px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 6px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.5);
        }}
        .control-info h4 {{
            font-size: 0.95rem;
            color: #fff;
            margin-bottom: 2px;
        }}
        .control-info p {{
            font-size: 0.8rem;
            color: var(--text-muted);
        }}

        /* Fisiologia & Barras */
        .physiology-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
            gap: 16px;
            margin-top: 16px;
        }}
        .physio-card {{
            background: #0f141d;
            border: 1px solid #232e40;
            border-radius: 6px;
            padding: 16px;
            position: relative;
            overflow: hidden;
        }}
        .physio-card::before {{
            content: '';
            position: absolute;
            left: 0;
            top: 0;
            bottom: 0;
            width: 4px;
        }}
        .physio-card.warmth::before {{ background: var(--orange); }}
        .physio-card.nutrition::before {{ background: var(--red); }}
        .physio-card.hydration::before {{ background: var(--cyan); }}
        .physio-card.energy::before {{ background: var(--gold); }}

        .physio-title {{
            font-size: 1rem;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 8px;
            margin-bottom: 6px;
        }}
        .physio-card.warmth .physio-title {{ color: var(--orange); }}
        .physio-card.nutrition .physio-title {{ color: var(--red); }}
        .physio-card.hydration .physio-title {{ color: var(--cyan); }}
        .physio-card.energy .physio-title {{ color: var(--gold); }}

        .physio-desc {{
            font-size: 0.82rem;
            color: var(--text-muted);
        }}

        /* Abas de Categorias de Culinária */
        .category-tabs {{
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin-bottom: 20px;
            border-bottom: 1px solid var(--border-color);
            padding-bottom: 12px;
        }}
        .tab-btn {{
            background: #111722;
            border: 1px solid #273447;
            color: var(--text-muted);
            padding: 8px 16px;
            border-radius: 6px;
            font-size: 0.84rem;
            font-weight: 500;
            cursor: pointer;
            transition: all 0.2s ease;
        }}
        .tab-btn:hover {{
            background: #1c2638;
            color: #fff;
            border-color: #3b4e6b;
        }}
        .tab-btn.active {{
            background: rgba(245, 200, 98, 0.15);
            color: var(--gold-bright);
            border-color: var(--gold);
        }}

        /* Grid de Receitas */
        .recipes-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
            gap: 20px;
        }}
        .recipe-card {{
            background: #101520;
            border: 1px solid #253142;
            border-radius: 8px;
            padding: 18px 20px;
            display: flex;
            flex-direction: column;
            gap: 12px;
            transition: all 0.2s ease;
            position: relative;
        }}
        .recipe-card:hover {{
            border-color: var(--gold);
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4);
        }}
        .recipe-top {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }}
        .recipe-name {{
            font-family: var(--font-display);
            font-size: 1.15rem;
            color: var(--gold);
            line-height: 1.3;
        }}
        .recipe-tier {{
            font-size: 0.7rem;
            background: #1e2838;
            color: var(--text-muted);
            padding: 2px 8px;
            border-radius: 4px;
            white-space: nowrap;
            border: 1px solid #2f3e57;
        }}
        .recipe-desc {{
            font-size: 0.82rem;
            color: #cbd5e1;
            font-style: italic;
            border-left: 2px solid var(--gold);
            padding-left: 10px;
            margin: 2px 0;
        }}
        .recipe-meta-row {{
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            font-size: 0.75rem;
        }}
        .meta-pill {{
            background: #18202d;
            border: 1px solid #2b394f;
            padding: 3px 8px;
            border-radius: 4px;
            display: flex;
            align-items: center;
            gap: 4px;
            color: #cbd5e1;
        }}
        .meta-pill.highlight {{
            color: var(--gold);
            border-color: rgba(245, 200, 98, 0.4);
        }}

        .recipe-section-title {{
            font-size: 0.75rem;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 1px;
            color: var(--gold);
            margin-top: 4px;
        }}
        .recipe-ingredients-list {{
            list-style: none;
            display: flex;
            flex-direction: column;
            gap: 6px;
        }}
        .recipe-ingredients-list li {{
            font-size: 0.8rem;
            display: flex;
            justify-content: space-between;
            background: #0a0d14;
            padding: 6px 10px;
            border-radius: 4px;
            border: 1px solid #1c2534;
        }}
        .ing-qty {{
            color: var(--gold-bright);
            font-weight: 600;
        }}
        .ing-tag {{
            font-size: 0.7rem;
            color: #64748b;
        }}

        .recipe-effects-box {{
            background: #090c12;
            border: 1px solid #202b3a;
            border-radius: 6px;
            padding: 8px 12px;
            display: flex;
            flex-wrap: wrap;
            gap: 12px;
            font-size: 0.78rem;
        }}
        .eff-item {{
            display: flex;
            align-items: center;
            gap: 4px;
        }}
        .eff-warmth {{ color: var(--orange); }}
        .eff-nutr {{ color: var(--red); }}
        .eff-thirst {{ color: var(--cyan); }}
        .eff-energy {{ color: var(--gold); }}

        /* Skills Matrix */
        .skills-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
            gap: 14px;
        }}
        .skill-card {{
            background: #101520;
            border: 1px solid #232e3f;
            border-radius: 6px;
            padding: 14px 16px;
            transition: all 0.2s ease;
        }}
        .skill-card:hover {{
            border-color: var(--gold);
            background: #141c2b;
        }}
        .skill-header {{
            display: flex;
            justify-content: space-between;
            align-items: baseline;
            margin-bottom: 6px;
        }}
        .skill-name {{
            font-weight: 600;
            color: #fff;
            font-size: 0.95rem;
        }}
        .skill-badge {{
            font-size: 0.68rem;
            padding: 2px 6px;
            border-radius: 3px;
            text-transform: uppercase;
        }}
        .skill-badge.combate {{ background: rgba(245, 101, 101, 0.2); color: var(--red); border: 1px solid var(--red); }}
        .skill-badge.sobrevivencia {{ background: rgba(72, 187, 120, 0.2); color: var(--green); border: 1px solid var(--green); }}
        .skill-badge.fisico {{ background: rgba(66, 153, 225, 0.2); color: var(--blue); border: 1px solid var(--blue); }}

        .skill-attribs {{
            font-size: 0.75rem;
            color: var(--gold);
            margin-bottom: 4px;
        }}
        .skill-desc {{
            font-size: 0.8rem;
            color: var(--text-muted);
            line-height: 1.4;
        }}

        /* BDO Explorer Table */
        .bdo-table-container {{
            max-height: 520px;
            overflow-y: auto;
            border: 1px solid #273447;
            border-radius: 6px;
            background: #090c12;
        }}
        table.bdo-table {{
            width: 100%;
            border-collapse: collapse;
            font-size: 0.82rem;
            text-align: left;
        }}
        table.bdo-table th {{
            background: #121924;
            color: var(--gold);
            padding: 10px 14px;
            position: sticky;
            top: 0;
            z-index: 2;
            border-bottom: 1px solid var(--border-color);
            font-weight: 600;
        }}
        table.bdo-table td {{
            padding: 8px 14px;
            border-bottom: 1px solid #161e2a;
            color: #cbd5e1;
        }}
        table.bdo-table tr:hover td {{
            background: rgba(245, 200, 98, 0.05);
            color: #fff;
        }}

        /* Footer */
        footer.wiki-footer {{
            background: #080a0e;
            border-top: 1px solid #1e2634;
            padding: 24px 30px;
            text-align: center;
            font-size: 0.8rem;
            color: var(--text-muted);
            margin-top: auto;
        }}
        footer.wiki-footer span {{
            color: var(--gold);
        }}
    </style>
</head>
<body>

    <!-- Header Fixo Superior -->
    <header class="top-header">
        <div class="logo-area">
            <div class="logo-icon">🗺️</div>
            <div class="logo-titles">
                <h1>UnReal World Reborn</h1>
                <span>Enciclopédia & Wiki Oficial de Sobrevivência</span>
            </div>
        </div>

        <div class="search-container">
            <span class="search-icon">🔍</span>
            <input type="text" id="globalSearchInput" placeholder="Buscar receita, habilidade, tecla ou mecânica..." oninput="handleGlobalSearch()">
        </div>

        <div class="header-badges">
            <div class="stat-badge">🍲 <strong>15</strong> Receitas Nórdicas</div>
            <div class="stat-badge">⚔️ <strong>30</strong> Habilidades</div>
            <div class="stat-badge">📜 <strong>321</strong> Fórmulas BDO</div>
        </div>
    </header>

    <!-- Estrutura Principal -->
    <div class="wiki-container">
        
        <!-- Navegação Lateral Fixa -->
        <nav class="wiki-sidebar">
            <div class="nav-group-title">Introdução & Motor</div>
            <a href="#visao-geral" class="nav-item">🌲 Visão Geral & Lore</a>
            <a href="#controles-hud" class="nav-item">⌨️ Controles & HUD</a>
            <a href="#fisiologia-clima" class="nav-item">❄️ Fisiologia & Calor</a>
            <a href="#sistema-mapas" class="nav-item">🗺️ Sistema de 2 Mapas</a>

            <div class="nav-group-title">Culinária & Botânica</div>
            <a href="#culinaria-manual" class="nav-item">🍲 Manual de Culinária</a>
            <a href="#mecanicas-quimicas" class="nav-item">🔬 Química & Processos</a>
            <a href="#receitas-nordicas" class="nav-item">📜 Receitas Implementadas</a>

            <div class="nav-group-title">Personagem & Habilidades</div>
            <a href="#skills-sistema" class="nav-item">⚔️ Matriz de Habilidades (30)</a>
            <a href="#bdo-referencia" class="nav-item">📚 Banco de Dados BDO</a>
            <a href="#roadmap" class="nav-item">🚀 Roadmap de Módulos</a>
        </nav>

        <!-- Conteúdo Principal -->
        <main class="wiki-content">

            <!-- 1. VISÃO GERAL -->
            <section class="wiki-section" id="visao-geral">
                <div class="section-header">
                    <h2 class="section-title">🌲 Visão Geral & Filosofia do Projeto</h2>
                    <span class="section-tag">Módulo 1</span>
                </div>
                <p>
                    <strong>UnReal World Reborn</strong> é uma re-implementação moderna, robusta e autônoma do lendário roguelike de sobrevivência nórdico *UnReal World*, ambientado na Finlândia da Idade do Ferro.
                </p>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 14px; margin-top: 14px;">
                    <div style="background: #0f141d; padding: 12px; border-radius: 6px; border: 1px solid #232d3d;">
                        <strong style="color: var(--gold);">⚡ C# Puro (.NET 9) + Raylib</strong>
                        <p style="font-size: 0.8rem; color: var(--text-muted); margin-top: 4px;">Zero sobrecarga de engines de 20 GB. Inicialização instantânea, aceleração GPU nativa a 60 FPS.</p>
                    </div>
                    <div style="background: #0f141d; padding: 12px; border-radius: 6px; border: 1px solid #232d3d;">
                        <strong style="color: var(--gold);">🇧🇷 Localização Nativa PT-BR</strong>
                        <p style="font-size: 0.8rem; color: var(--text-muted); margin-top: 4px;">Suporte de primeira classe ao Português do Brasil em todas as descrições, receitas e lore.</p>
                    </div>
                    <div style="background: #0f141d; padding: 12px; border-radius: 6px; border: 1px solid #232d3d;">
                        <strong style="color: var(--gold);">📦 Data-Driven Total</strong>
                        <p style="font-size: 0.8rem; color: var(--text-muted); margin-top: 4px;">Separação estrita de dados e lógica: itens, receitas e biomas ficam em arquivos JSON fáceis de moddar.</p>
                    </div>
                </div>
            </section>

            <!-- 2. CONTROLES & INTERFACE -->
            <section class="wiki-section" id="controles-hud">
                <div class="section-header">
                    <h2 class="section-title">⌨️ Controles & Guia da Interface (HUD)</h2>
                    <span class="section-tag">Controles Oficiais</span>
                </div>
                <p>O jogo combina a precisão clássica dos atalhos de teclado com a agilidade do mouse moderno para inspeção de inventário e menus contextuais.</p>

                <div class="controls-grid">
                    <div class="control-card">
                        <div class="key-badge">W A S D</div>
                        <div class="control-info">
                            <h4>Movimentação</h4>
                            <p>Move o sobrevivente pelas 4/8 direções do grid local ou do mapa continental.</p>
                        </div>
                    </div>
                    <div class="control-card">
                        <div class="key-badge">ENTER</div>
                        <div class="control-info">
                            <h4>Zoom no Mapa (Wilderness)</h4>
                            <p>Ao estar sobre um tile no Mapa Mundi, entra no setor tático 60x60.</p>
                        </div>
                    </div>
                    <div class="control-card">
                        <div class="key-badge">M</div>
                        <div class="control-info">
                            <h4>Alternar Mapa Mundi / Local</h4>
                            <p>Sobe para a visão continental de biomas ou retorna ao acampamento.</p>
                        </div>
                    </div>
                    <div class="control-card">
                        <div class="key-badge">I / TAB</div>
                        <div class="control-info">
                            <h4>Mochila & Equipamentos</h4>
                            <p>Abre o container do Ranger com as 4 barras vitais, equipamentos 3x3 e 21 slots da mochila.</p>
                        </div>
                    </div>
                    <div class="control-card">
                        <div class="key-badge">C</div>
                        <div class="control-info">
                            <h4>Estação de Culinária</h4>
                            <p>Abre a interface completa de preparo de alimentos, moagem e purificação.</p>
                        </div>
                    </div>
                    <div class="control-card">
                        <div class="key-badge">BOTÃO DIR.</div>
                        <div class="control-info">
                            <h4>Menu de Contexto</h4>
                            <p>Clique em árvores (derrubar/casca), pedras (lascar pontas), fogueiras ou solo para agir.</p>
                        </div>
                    </div>
                </div>
            </section>

            <!-- 3. FISIOLOGIA & CLIMA -->
            <section class="wiki-section" id="fisiologia-clima">
                <div class="section-header">
                    <h2 class="section-title">❄️ Fisiologia Corporal, Tempo & Calor</h2>
                    <span class="section-tag">Simulação Viva</span>
                </div>
                <p>O sobrevivente possui 4 métricas biológicas contínuas que reagem ao clima, vento e esforço muscular:</p>

                <div class="physiology-grid">
                    <div class="physio-card warmth">
                        <div class="physio-title">🔥 Calor Corporal (Warmth)</div>
                        <div class="physio-desc">
                            Cai continuamente em temperaturas negativas (-2°C na taiga). Estar perto de uma fogueira recupera <strong>+0.1%/s</strong>. Alimentos e sopas quentes concedem aumentos imediatos e proteção prolongada contra hipotermia.
                        </div>
                    </div>
                    <div class="physio-card nutrition">
                        <div class="physio-title">🍖 Nutrição & Fome</div>
                        <div class="physio-desc">
                            Diminui 0.05% por turno/minuto. Ensopados de alce, trutas na brasa e pães de centeio restauram entre 30 a 70 pontos de nutrição calórica.
                        </div>
                    </div>
                    <div class="physio-card hydration">
                        <div class="physio-title">💧 Hidratação & Sede</div>
                        <div class="physio-desc">
                            Cai 0.08% por turno. Beber água pura esterilizada, caldos de tutano ou sopas reconfortantes hidrata o corpo sem risco de cólera ou parasitas fluviais.
                        </div>
                    </div>
                    <div class="physio-card energy">
                        <div class="physio-title">⚡ Energia & Fadiga</div>
                        <div class="physio-desc">
                            Consumida pesadamente em ações de força: derrubar pinheiros (-10%), moer grãos em pedra (-8%), montar abrigo (-12%). Recuperada com descanso ou refeições ricas em gordura e manteiga.
                        </div>
                    </div>
                </div>
            </section>

            <!-- 4. SISTEMA DE DOIS MAPAS -->
            <section class="wiki-section" id="sistema-mapas">
                <div class="section-header">
                    <h2 class="section-title">🗺️ O Sistema de Dois Níveis de Mapa (Dual-Map Engine)</h2>
                    <span class="section-tag">Inovação URW</span>
                </div>
                <p>
                    Preservando a genialidade do *UnReal World*, o jogo não é um mapa minúsculo e nem uma engine pesada que engasga ao carregar mundos infinitos:
                </p>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 18px; margin-top: 14px;">
                    <div style="background: #0d121a; border: 1px solid #233044; padding: 14px; border-radius: 6px;">
                        <h4 style="color: var(--gold); margin-bottom: 6px;">1. Mapa Mundi Continental (Overworld)</h4>
                        <p style="font-size: 0.82rem; color: var(--text-muted);">
                            Viaje por dezenas de quilômetros em segundos. Cada passo consome entre 15 e 30 minutos de tempo de jogo. Permite cruzar taigas de pinheiro, florestas de bétulas, pântanos (mires) e cordilheiras montanhosas.
                        </p>
                    </div>
                    <div style="background: #0d121a; border: 1px solid #233044; padding: 14px; border-radius: 6px;">
                        <h4 style="color: var(--gold); margin-bottom: 6px;">2. Setor Local Tático (Wilderness) com Delta Saves</h4>
                        <p style="font-size: 0.82rem; color: var(--text-muted);">
                            Ao pressionar ENTER, você faz o "zoom" para o mapa local com grid tático detalhado. O motor gera o relevo por hash determinístico e salva apenas as alterações que você fez (árvores derrubadas, fogueiras acesas), ocupando míseros kilobytes de memória.
                        </p>
                    </div>
                </div>
            </section>

            <!-- 5. MANUAL DE CULINÁRIA & PROCESSAMENTO -->
            <section class="wiki-section" id="culinaria-manual">
                <div class="section-header">
                    <h2 class="section-title">🍲 Manual Completo de Culinária & Processamento</h2>
                    <span class="section-tag">Fusão URW + BDO</span>
                </div>
                <p>
                    O sistema de culinária foi desenvolvido combinando o <strong>realismo químico de sobrevivência do UnReal World</strong> com a <strong>hierarquia de 4 Tiers e substituição por categorias do Black Desert Online</strong>.
                </p>

                <!-- Processos Químicos -->
                <div id="mecanicas-quimicas" style="margin-top: 18px;">
                    <h3 style="font-family: var(--font-display); color: var(--gold); font-size: 1.1rem; margin-bottom: 10px;">🔬 Processos Físicos e Químicos da Taiga</h3>
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 12px;">
                        <div style="background: #090d14; border: 1px solid #1f2a3a; padding: 12px; border-radius: 6px;">
                            <strong style="color: var(--cyan);">🧂 Evaporação & Salmoura</strong>
                            <p style="font-size: 0.78rem; color: var(--text-muted); margin-top: 4px;">Ferva água salgada na fogueira até evaporar para obter sal puro. Misture sal e água potável para criar salmoura e curar carnes contra decomposição.</p>
                        </div>
                        <div style="background: #090d14; border: 1px solid #1f2a3a; padding: 12px; border-radius: 6px;">
                            <strong style="color: var(--green);">🍄 Neutralização de Toxinas</strong>
                            <p style="font-size: 0.78rem; color: var(--text-muted); margin-top: 4px;">Falsa-morchella (Gyromitra) possui toxinas letais crua. A fervura dupla por 20 minutos com descarte da água elimina 100% dos venenos, transformando-a em iguaria segura.</p>
                        </div>
                        <div style="background: #090d14; border: 1px solid #1f2a3a; padding: 12px; border-radius: 6px;">
                            <strong style="color: var(--gold);">🌾 Moagem Mecânica em Quern</strong>
                            <p style="font-size: 0.78rem; color: var(--text-muted); margin-top: 4px;">Duas pedras chatas de moer atritadas transformam grãos colhidos de centeio ou cevada em farinhas rústicas nutritivas para pães e mingaus.</p>
                        </div>
                        <div style="background: #090d14; border: 1px solid #1f2a3a; padding: 12px; border-radius: 6px;">
                            <strong style="color: var(--orange);">🧈 Batedura & Separação</strong>
                            <p style="font-size: 0.78rem; color: var(--text-muted); margin-top: 4px;">Leite espesso batido com sal em batedor de madeira separa a manteiga pura do soro líquido (whey), permitindo conservação calórica prolongada.</p>
                        </div>
                    </div>
                </div>

                <!-- Abas de Filtro de Receitas -->
                <div id="receitas-nordicas" style="margin-top: 26px;">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
                        <h3 style="font-family: var(--font-display); color: var(--gold-bright); font-size: 1.15rem;">📜 Compêndio de Receitas Implementadas (15)</h3>
                        <span style="font-size: 0.8rem; color: var(--text-muted);">Pressione <strong>C</strong> no jogo para abrir a estação</span>
                    </div>

                    <div class="category-tabs" id="recipeTabs">
                        <button class="tab-btn active" onclick="filterRecipes('Todas')">Todas as Receitas (15)</button>
                        <button class="tab-btn" onclick="filterRecipes('Processamento Básico')">Processamento Básico (6)</button>
                        <button class="tab-btn" onclick="filterRecipes('Bases e Molhos')">Bases & Molhos (2)</button>
                        <button class="tab-btn" onclick="filterRecipes('Sopas e Pratos Quentes')">Sopas & Pratos Quentes (4)</button>
                        <button class="tab-btn" onclick="filterRecipes('Assados e Pães')">Assados & Pães (2)</button>
                        <button class="tab-btn" onclick="filterRecipes('Grandes Banquetes e Rituais')">Grandes Banquetes (1)</button>
                    </div>

                    <div class="recipes-grid" id="recipesGrid">
                        <!-- Gerado via JavaScript dinamicamente a partir do JSON autêntico -->
                    </div>
                </div>
            </section>

            <!-- 6. MATRIZ DE HABILIDADES (30 SKILLS) -->
            <section class="wiki-section" id="skills-sistema">
                <div class="section-header">
                    <h2 class="section-title">⚔️ Árvore Completa de Habilidades (Skills)</h2>
                    <span class="section-tag">30 Habilidades Autênticas</span>
                </div>
                <p>
                    As habilidades determinam o sucesso de suas ações na taiga. Cada habilidade é influenciada por atributos genéticos do personagem e progride com o uso diário:
                </p>

                <div class="category-tabs" style="margin-top: 14px;">
                    <button class="tab-btn active" onclick="filterSkills('Todas')">Todas as Habilidades (30)</button>
                    <button class="tab-btn" onclick="filterSkills('Combate')">Combate (10)</button>
                    <button class="tab-btn" onclick="filterSkills('Sobrevivência')">Sobrevivência & Lore (16)</button>
                    <button class="tab-btn" onclick="filterSkills('Físico')">Físico (4)</button>
                </div>

                <div class="skills-grid" id="skillsGrid">
                    <!-- Gerado via JavaScript dinamicamente -->
                </div>
            </section>

            <!-- 7. BANCO DE DADOS BDO (REFERÊNCIA TÉCNICA) -->
            <section class="wiki-section" id="bdo-referencia">
                <div class="section-header">
                    <h2 class="section-title">📚 Banco de Dados de Referência do Black Desert Online</h2>
                    <span class="section-tag">321 Fórmulas Extraídas</span>
                </div>
                <p>
                    Extraído diretamente da API do BDO Codex via nosso script multithread. Serve como matriz de referência de equilíbrio para expandir novas refeições, molhos e banquetes no <em>UnReal World Reborn</em>:
                </p>

                <div style="margin: 14px 0;">
                    <input type="text" id="bdoSearchInput" placeholder="Filtrar por nome no BDO (ex: Vinagre, Cerveja, Molho, Carne, Chá)..." 
                           oninput="filterBdoTable()" 
                           style="width: 100%; background: #090c12; border: 1px solid #273447; border-radius: 6px; padding: 10px 14px; color: #fff; font-size: 0.86rem;">
                </div>

                <div class="bdo-table-container">
                    <table class="bdo-table" id="bdoTable">
                        <thead>
                            <tr>
                                <th style="width: 25%;">Prato / Receita</th>
                                <th style="width: 15%;">Nível Mínimo</th>
                                <th style="width: 35%;">Ingredientes (Traduzidos PT-BR)</th>
                                <th style="width: 25%;">Produtos Obtidos</th>
                            </tr>
                        </thead>
                        <tbody id="bdoTableBody">
                            <!-- Preenchido via script -->
                        </tbody>
                    </table>
                </div>
            </section>

            <!-- 8. ROADMAP DE MÓDULOS -->
            <section class="wiki-section" id="roadmap">
                <div class="section-header">
                    <h2 class="section-title">🚀 Roadmap do Projeto: UnReal World Reborn</h2>
                    <span class="section-tag">Status em Tempo Real</span>
                </div>
                <div style="display: flex; flex-direction: column; gap: 10px; margin-top: 10px;">
                    <div style="background: rgba(72, 187, 120, 0.1); border: 1px solid var(--green); padding: 10px 16px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                        <span><strong>Módulo 1:</strong> Renderizador 2D, Câmera e Y-Sorting Suave</span>
                        <span style="color: var(--green); font-weight: 700; font-size: 0.8rem;">[CONCLUÍDO]</span>
                    </div>
                    <div style="background: rgba(72, 187, 120, 0.1); border: 1px solid var(--green); padding: 10px 16px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                        <span><strong>Módulo 2:</strong> Sistema de 2 Níveis de Mapa (Overworld -> Wilderness com Zoom ENTER)</span>
                        <span style="color: var(--green); font-weight: 700; font-size: 0.8rem;">[CONCLUÍDO]</span>
                    </div>
                    <div style="background: rgba(72, 187, 120, 0.1); border: 1px solid var(--green); padding: 10px 16px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                        <span><strong>Módulo 4:</strong> Inventário, Mochila 21 Slots, Atributos & HUD de Personagem (I / TAB)</span>
                        <span style="color: var(--green); font-weight: 700; font-size: 0.8rem;">[CONCLUÍDO]</span>
                    </div>
                    <div style="background: rgba(72, 187, 120, 0.1); border: 1px solid var(--green); padding: 10px 16px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                        <span><strong>Módulo 7:</strong> Sistema Culinário, Botânica & Processamento Avançado (Fusão URW + BDO)</span>
                        <span style="color: var(--green); font-weight: 700; font-size: 0.8rem;">[CONCLUÍDO]</span>
                    </div>
                    <div style="background: rgba(245, 200, 98, 0.1); border: 1px solid var(--gold); padding: 10px 16px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                        <span><strong>Módulo 6:</strong> Arquitetura MMORPG / Multiplayer & Menu Atmosférico</span>
                        <span style="color: var(--gold); font-weight: 700; font-size: 0.8rem;">[EM PROGRESSO]</span>
                    </div>
                    <div style="background: #111722; border: 1px solid #28374d; padding: 10px 16px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                        <span><strong>Módulo 3:</strong> Modelo Anatômico de Ferimentos, Camadas de Roupas & 4 Estações</span>
                        <span style="color: var(--text-muted); font-size: 0.8rem;">[PLANEJADO]</span>
                    </div>
                    <div style="background: #111722; border: 1px solid #28374d; padding: 10px 16px; border-radius: 6px; display: flex; justify-content: space-between; align-items: center;">
                        <span><strong>Módulo 5:</strong> Fauna, Rastreamento Olfativo & Vento (Pegadas na neve)</span>
                        <span style="color: var(--text-muted); font-size: 0.8rem;">[PLANEJADO]</span>
                    </div>
                </div>
            </section>

        </main>
    </div>

    <footer class="wiki-footer">
        <p><strong>UnReal World Reborn</strong> — Enciclopédia & Wiki de Sobrevivência</p>
        <p style="margin-top: 4px; color: #64748b;">Desenvolvido em C# (.NET 9) com Raylib-cs | Inspiração: UnReal World & Black Desert Online</p>
    </footer>

    <!-- Dados Embutidos & Script de Interatividade -->
    <script>
        const NORDIC_RECIPES = {json.dumps(nordic_recipes, ensure_ascii=False)};
        const SKILLS_DATA = {json.dumps(skills_data, ensure_ascii=False)};
        const BDO_RECIPES = {json.dumps(bdo_recipes, ensure_ascii=False)};

        let currentRecipeCat = 'Todas';
        let currentSkillGroup = 'Todas';

        // Renderiza Receitas Nórdicas
        function renderRecipes() {{
            const grid = document.getElementById('recipesGrid');
            grid.innerHTML = '';

            const filtered = (currentRecipeCat === 'Todas')
                ? NORDIC_RECIPES
                : NORDIC_RECIPES.filter(r => r.category === currentRecipeCat);

            filtered.forEach(rec => {{
                const card = document.createElement('div');
                card.className = 'recipe-card';
                card.setAttribute('data-search', (rec.name + ' ' + rec.description + ' ' + rec.category + ' ' + rec.cookware).toLowerCase());

                let ingsHtml = rec.ingredients.map(ing => `
                    <li>
                        <span>${{ing.name}} <small class="ing-tag">[${{ing.consumed ? 'Consome' : 'Ferramenta'}}]</small></span>
                        <span class="ing-qty">${{ing.qty}}x</span>
                    </li>
                `).join('');

                let prodsHtml = rec.products.map(p => `
                    <span class="meta-pill highlight">✨ ${{p.name}} (${{p.qty_min}}${{p.qty_min !== p.qty_max ? '~' + p.qty_max : ''}}x)</span>
                `).join('');

                let eff = rec.survival_effects;
                let specialBuffHtml = eff.special_buff ? `<div style="color: var(--gold); font-size: 0.72rem; margin-top: 4px; width: 100%;">👑 ${{eff.special_buff}}</div>` : '';

                card.innerHTML = `
                    <div class="recipe-top">
                        <div class="recipe-name">${{rec.name}}</div>
                        <div class="recipe-tier">${{rec.category}}</div>
                    </div>
                    <div class="recipe-desc">"${{rec.description}}"</div>
                    
                    <div class="recipe-meta-row">
                        <span class="meta-pill">🍳 ${{rec.cookware}}</span>
                        <span class="meta-pill">🔥 ${{rec.heat_required}}</span>
                        <span class="meta-pill">⏱️ ${{rec.prep_minutes + rec.cook_minutes}} min (${{rec.prep_minutes}}p + ${{rec.cook_minutes}}c)</span>
                    </div>

                    <div class="recipe-section-title">Ingredientes Requeridos</div>
                    <ul class="recipe-ingredients-list">
                        ${{ingsHtml}}
                    </ul>

                    <div class="recipe-section-title">Produtos Finais</div>
                    <div class="recipe-meta-row">
                        ${{prodsHtml}}
                    </div>

                    <div class="recipe-section-title">Efeitos Vitais</div>
                    <div class="recipe-effects-box">
                        <div class="eff-item eff-warmth">🔥 +${{eff.warmth_bonus}}% Calor</div>
                        <div class="eff-item eff-nutr">🍖 +${{eff.nutrition}} Fome</div>
                        <div class="eff-item eff-thirst">💧 +${{eff.thirst}} Sede</div>
                        <div class="eff-item eff-energy">⚡ +${{eff.energy}} Energia</div>
                        ${{specialBuffHtml}}
                    </div>
                `;

                grid.appendChild(card);
            }});
        }}

        function filterRecipes(cat) {{
            currentRecipeCat = cat;
            document.querySelectorAll('#recipeTabs .tab-btn').forEach(btn => {{
                btn.classList.toggle('active', btn.innerText.startsWith(cat) || (cat === 'Todas' && btn.innerText.startsWith('Todas')));
            }});
            renderRecipes();
        }}

        // Renderiza Skills
        function renderSkills() {{
            const grid = document.getElementById('skillsGrid');
            grid.innerHTML = '';

            const filtered = (currentSkillGroup === 'Todas')
                ? SKILLS_DATA
                : SKILLS_DATA.filter(s => s.group === currentSkillGroup);

            filtered.forEach(s => {{
                const card = document.createElement('div');
                card.className = 'skill-card';
                card.setAttribute('data-search', (s.name + ' ' + s.desc + ' ' + s.attribs + ' ' + s.group).toLowerCase());

                let badgeClass = s.group.toLowerCase().replace('é', 'e');

                card.innerHTML = `
                    <div class="skill-header">
                        <div class="skill-name">${{s.name}}</div>
                        <span class="skill-badge ${{badgeClass}}">${{s.group}}</span>
                    </div>
                    <div class="skill-attribs">Atributos Base: ${{s.attribs}}</div>
                    <div class="skill-desc">${{s.desc}}</div>
                    <div style="font-size: 0.7rem; color: #64748b; margin-top: 6px;">Uso: ${{s.key}}</div>
                `;

                grid.appendChild(card);
            }});
        }}

        function filterSkills(grp) {{
            currentSkillGroup = grp;
            event.target.parentElement.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
            event.target.classList.add('active');
            renderSkills();
        }}

        // Renderiza Tabela BDO
        function renderBdoTable(filterText = '') {{
            const tbody = document.getElementById('bdoTableBody');
            tbody.innerHTML = '';

            let count = 0;
            const searchLower = filterText.toLowerCase();

            for (let r of BDO_RECIPES) {{
                let matNames = r.ingredients.map(m => m.name).join(', ');
                let prodNames = r.products.map(p => p.name).join(', ');

                if (searchLower && !r.name.toLowerCase().includes(searchLower) && !matNames.toLowerCase().includes(searchLower)) {{
                    continue;
                }}

                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td style="font-weight: 600; color: var(--gold);">${{r.name}}</td>
                    <td><span style="background: #192332; padding: 2px 6px; border-radius: 4px; font-size: 0.75rem;">${{r.level}}</span></td>
                    <td>${{r.ingredients.map(i => `${{i.name}} (${{i.qty}}x)`).join(', ')}}</td>
                    <td style="color: var(--cyan);">${{r.products.map(p => `${{p.name}} (${{p.qty}}x)`).join(', ')}}</td>
                `;
                tbody.appendChild(tr);

                count++;
                if (count >= 100 && !searchLower) break; // Limite de 100 linhas iniciais para renderização instantânea
            }}
        }}

        function filterBdoTable() {{
            const query = document.getElementById('bdoSearchInput').value;
            renderBdoTable(query);
        }}

        // Busca Global Instantânea
        function handleGlobalSearch() {{
            const query = document.getElementById('globalSearchInput').value.toLowerCase().trim();
            if (!query) {{
                document.querySelectorAll('.recipe-card, .skill-card, .control-card').forEach(el => el.style.display = '');
                return;
            }}

            // Filtra receitas
            document.querySelectorAll('.recipe-card').forEach(card => {{
                const text = card.getAttribute('data-search') || '';
                card.style.display = text.includes(query) ? '' : 'none';
            }});

            // Filtra skills
            document.querySelectorAll('.skill-card').forEach(card => {{
                const text = card.getAttribute('data-search') || '';
                card.style.display = text.includes(query) ? '' : 'none';
            }});
        }}

        // Inicialização
        document.addEventListener('DOMContentLoaded', () => {{
            renderRecipes();
            renderSkills();
            renderBdoTable();
        }});
    </script>
</body>
</html>
"""

    output_file = os.path.join(base_dir, 'WIKI_URW_REBORN.html')
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(html_content)

    print(f"Sucesso! WikiGame gerada em: {output_file}")
    print(f"Tamanho do arquivo: {os.path.getsize(output_file)} bytes")

if __name__ == '__main__':
    build_wiki()
