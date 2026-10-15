# AGENTS.md — UnReal World Reborn

Instruções para agentes de IA trabalhando neste repositório.

## Visão Geral do Projeto

Re-implementação moderna e independente do simulador de sobrevivência roguelike **UnReal World** (Idade do Ferro nórdica/finlandesa), em **C# puro (.NET 9)** com **Raylib-cs** para renderização acelerada por GPU. Sem engines (Unity/Unreal): simulação lógica pesada, arquitetura limpa e 100% de código próprio.

## Contexto Crítico: Relação com o Jogo Original ⚠️

O diretório-pai **`K:\UnRealWorld` é a instalação ORIGINAL do UnReal World** (versão comercial, 3.50+), e `urw_reborn/` é o nosso projeto dentro dela:

```
K:\UnRealWorld\                  ← INSTALAÇÃO ORIGINAL (somente-leitura!)
├── truetile/    Sprites PNG do jogo: ter-*, wp-*, ar-*, npc-*, av-*, flo-*, it-*, tr-*...
├── truegfx/     Arte HD, retratos, paisagens de bioma, backgrounds
├── audio/       Sons e músicas do jogo original
├── messages/    Textos de eventos/mensagens do jogo
├── gameinfo/, defaults/, data *.DAT/.NFO → constantes e configuração do original
├── cookery_glossary.txt, diy_glossary.txt, biy_glossary.txt → glossários de receitas/processos
├── flora_berries.txt, flora_herbs.txt, flora_mushroom.txt, flora_newherbs.txt... → botânica
├── ini_skills.txt, CONSTANT.OBJ, GAME.NFO, GFX.NFO → dados de sistema
└── urw_reborn/  ★ NOSSO PROJETO (única pasta onde escrevemos)
```

**Regra de ouro para qualquer trabalho:**
1. **O original é a FONTE DE VERDADE de conteúdo:** ao implementar mecânicas, itens, receitas, animais, biomas, clima ou strings de UI, consulte PRIMEIRO os arquivos do original (glossários, `messages/`, `truetile/`, `gameinfo/`). Temos acesso pleno de leitura a eles.
2. **Somente-leitura:** NUNCA modificar, sobrescrever, mover ou apagar qualquer arquivo fora de `urw_reborn/`. Isso protege a instalação original de qualquer corrompimento.
3. **Assets próprios:** nunca referenciar diretamente sprites/som do original em runtime do urw_reborn. Se um sprite do original for útil como referência visual, recriamos/copiamos para `urw_reborn/assets/`.
4. Ao discutir "como o URW faz", "o dano de um urso", "tempo de sopa" etc., prefira extrair valores/regras REAIS do original do que inventar.

**Princípios do projeto:**
- **Data-Driven & Modding:** lógica do motor separada dos dados. Itens, receitas, biomas e textos ficam em JSON legível (`data/`).
- **Localização nativa PT-BR:** todo texto de UI e conteúdo voltado ao jogador é em Português do Brasil.
- **Referência do original:** a instalação original em `K:\UnRealWorld` é fonte de verdade de conteúdo (leitura) e inviolável para escrita — ver seção acima.
- **Controles híbridos:** atalhos numéricos de 8 direções (roguelike clássico) + suporte a mouse (clique para andar, arrastar itens, menu de contexto com botão direito).

## Build & Execução

```bash
# Compilar
dotnet build urw_reborn.csproj

# Rodar (a partir da pasta do projeto)
dotnet run

# Há também um launcher na raiz de K:\UnRealWorld: JOGAR_URW_REBORN.bat
```

- Framework: `net9.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- Único pacote NuGet: `Raylib-cs 8.1.0`. Não adicionar dependências sem necessidade.
- Sem suíte de testes .NET formal no momento; scripts Python em `scripts/` servem de ferramentas de suporte (validação de receitas, importação BDO, geração de wiki).

## Estrutura de Arquivos

| Arquivo | Responsabilidade |
|---|---|
| `Program.cs` (~990 linhas) | Ponto de entrada, game loop (Raylib), estado do jogo (jogador, fisiologia, entidades, log), menu de contexto, modo Wilderness vs Overworld |
| `WorldMap.cs` (~470 linhas) | Mapa mundi continental procedural (ruído fractal de altitude/umidade, 8 biomas), geração determinística de setores locais (`LocalSeed = Hash(WorldSeed, WorldX, WorldY)`), delta saves (`LocalSectorDeltas`) |
| `InventoryHUD.cs` (~700 linhas) | HUD do personagem (barras de sobrevivência, 11 atributos URW), inventário, equipamentos |
| `CookingSystem.cs` (~320 linhas) | Motor culinário: carrega receitas de `data/`, sistema de tags/famílias de ingredientes, verificações de craft, efeitos fisiológicos |
| `CookingHUD.cs` (~280 linhas) | Interface gráfica de culinária (atalho `C`, tabs de categoria, verificação de ingredientes, efeitos) |
| `MainMenu.cs` (~230 linhas) | Menu principal: Singleplayer / Multiplayer (nick, IP, porta) |
| `NetworkSession.cs` (~90 linhas) | Estado de sessão (modo, jogador local, jogadores remotos) — multiplayer ainda é scaffold/placeholder |
| `data/*.json` | Dados do jogo: `nordic_cooking_recipes.json` (receitas nórdicas originais), `bdo_cooking_recipes_enriched_pt.json` e afins (receitas BDO traduzidas/enriquecidas) |
| `scripts/*.py` | Ferramentas: `bdo_recipe_importer.py`, `enrich_bdo_recipes.py`, `generate_wiki.py` (gera `WIKI_URW_REBORN.html`), `test_tips.py` |
| `assets/sprites/` | Sprites próprios PNG 32-bit RGBA (jogador, bétula, pinheiro, pedra etc.) |
| `PLANEJAMENTO.md` | **Documento vivo de decisões e roadmap.** Leia antes de trabalhos grandes; atualize após implementar módulos |

## Estado Atual (Roadmap — ver PLANEJAMENTO.md)

Implementado: menu principal, mapa mundi + wilderness com dois níveis e transição (tecla `M` / `ENTER`), persistência de setores (delta saves), sistema culinário completo (Módulo 7), HUD de inventário/atributos.

Em progresso / pendente: Y-sorting e pivôs no renderizador (Módulo 1), fisiologia e tempo contínuo completos (Módulo 3), mochila espacial estilo Tetris/Tarkov com rotação `R` (Módulo 4), fauna/rastreamento/IA (Módulo 5), multiplayer real com desacoplamento client-server (Módulo 6).

## Convenções de Código

- **Idioma:** nomes de classes/métodos em inglês; strings de UI, logs de mensagem e conteúdo de dados em PT-BR.
- **Estilo:** classes estáticas como "serviços de sistema" (`WorldMap`, `CookingSystem`, `InventoryHUD`) com estado em propriedades estáticas; estado mutável simples via propriedades `{ get; set; }`. Manter esse padrão até a refatoração client-server (Módulo 6) introduzir camadas de simulação.
- **Sprites:** sempre verificar se o asset existe em `assets/sprites/` antes de usar; carregamentos usam `LoadSprite` que retorna `Texture2D?` (nullable).
- **Constantes de layout:** tamanho de tile (44px), dimensões de tela e grid ficam no topo de `Program.cs`; reutilizar em vez de duplicar números mágicos.
- **Determinismo:** geração procedural deve ser determinística por seed (usar a convenção `LocalSeed = Hash(WorldSeed, WorldX, WorldY)`); alterações locais devem passar pelo sistema `LocalSectorDeltas`.
- **UI e tipografia:** TODO texto/painel/interface usa o kit `UiTheme.cs` (fonte TTF Segoe UI/Arial carregada em `UiTheme.Load()` após `InitWindow`, paleta dashboard `UiTheme.Card/Border/Accent`, helpers `Text/Label/Header/Panel/StatBar/Separator`). **NUNCA** usar `Raylib.DrawText`/`MeasureText` para textos de UI (reservado apenas a emojis); prefira as classes de cor do UiTheme a `Color.` cru.

## Padrões de Trabalho do Agente

1. Antes de implementar módulos grandes, consultar a seção correspondente em `PLANEJAMENTO.md` (possui especificações detalhadas, ex.: tabela de dimensões físicas de itens).
2. Ao concluir entrega que muda o estado de um módulo, marcar o item correspondente (`[x]`) em `PLANEJAMENTO.md` e anotar decisões novas na tabela de log de decisões.
3. Receitas novas: adicionar em `data/nordic_cooking_recipes.json` seguindo o schema de `CookingRecipe` (tags de ingredientes em vez de IDs fixos sempre que aplicável); usar `scripts/test_tips.py` para checagens rápidas de dados.
4. Verificar o build (`dotnet build`) após qualquer alteração de código — é a única verificação automatizada disponível no momento.
5. **Nunca escrever fora de `urw_reborn/`** — toda alteração fica em `K:\UnRealWorld\urw_reborn\`. Ler do restante da instalação original é permitido e encorajado como referência.
6. Ao extrair dados do original para o projeto (tabela de itens, receitas do `cookery_glossary.txt`, flora), transformar em JSON/data-driven em `urw_reborn/data/`, nunca referenciar o arquivo original em runtime.
