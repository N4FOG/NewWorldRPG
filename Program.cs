using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace UrwReborn;

public enum EntityType
{
    None,
    Birch,
    Pine,
    Boulder,
    FallenLog,
    Fireplace
}

public class MapEntity
{
    public int X { get; set; }
    public int Y { get; set; }
    public EntityType Type { get; set; }
    public bool IsIdentified { get; set; }

    public MapEntity(int x, int y, EntityType type)
    {
        X = x;
        Y = y;
        Type = type;
        IsIdentified = false;
    }
}

public class ContextAction
{
    public string Label { get; set; } = "";
    public Action Execute { get; set; } = () => { };
    public Color TextColor { get; set; } = Color.RayWhite;
}

public enum GameState
{
    Menu,
    Gameplay
}

public enum GameViewMode
{
    Wilderness,
    Overworld
}

public static class Program
{
    private const int ScreenWidth = 1160;
    private const int ScreenHeight = 720;
    private const int TileSize = 44;
    private const int MapWidthTiles = 17;
    private const int MapHeightTiles = 15;

    // Estado da Aplicação (Menu Principal vs Jogo)
    private static GameState currentState = GameState.Menu;

    // Modo Atual de Visualização (Mapa Local vs Mapa Mundi)
    private static GameViewMode currentMode = GameViewMode.Wilderness;

    // Estado do Jogo
    private static int playerX = 8;
    private static int playerY = 7;
    private static int turnsElapsed = 0;
    private static int gameHour = 14;
    private static int gameMinute = 20;

    // Fisiologia
    private static float bodyWarmth = 95f;   // 0 a 100%
    private static float nutrition = 85f;    // 0 a 100%
    private static float hydration = 90f;    // 0 a 100%
    private static float energy = 100f;      // 0 a 100%

    // Entidades do mundo local
    private static List<MapEntity> entities = new();

    // Menu de contexto flutuante (botão direito)
    private static bool menuOpen = false;
    private static Vector2 menuPos = Vector2.Zero;
    private static List<ContextAction> menuActions = new();

    // Log de Eventos da Barra Lateral
    private static List<(string text, Color color)> messageLog = new();

    public static void Main(string[] args)
    {
        if (args != null && args.Contains("--selftest"))
        {
            RunSelfTest();
            return;
        }

        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "UnReal World Reborn - HUD & Menu de Contexto Moderno");
        Raylib.SetTargetFPS(60);

        // Kit de UI moderno (fontes TTF + paleta dashboard)
        UiTheme.Load();

        // Caminho dos sprites próprios
        string assetsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "assets", "sprites"));
        if (!Directory.Exists(assetsDir)) assetsDir = @"k:\UnRealWorld\urw_reborn\assets\sprites";

        Texture2D? playerTex = LoadSprite(Path.Combine(assetsDir, "player_woodsman.png"));
        Texture2D? birchTex = LoadSprite(Path.Combine(assetsDir, "tree_birch.png"));
        Texture2D? pineTex = LoadSprite(Path.Combine(assetsDir, "tree_pine.png"));
        Texture2D? boulderTex = LoadSprite(Path.Combine(assetsDir, "rock_boulder.png"));

        InitWorld();

        AddLog("Você desperta em uma clareira na floresta boreal.", Color.Gold);
        AddLog("O ar está cortante (-2°C). Vento suave do norte.", Color.SkyBlue);
        AddLog("Dica: Clique com o BOTÃO DIREITO em árvores ou pedras para interagir.", Color.Green);

        while (!Raylib.WindowShouldClose())
        {
            HandleInput();
            UpdateGame();

            Raylib.BeginDrawing();

            if (currentState == GameState.Menu)
            {
                // Renderiza o Menu Principal Nórdico (Singleplayer vs. MMORPG)
                MainMenu.Draw(ScreenWidth, ScreenHeight,
                    onStartSingleplayer: () =>
                    {
                        currentState = GameState.Gameplay;
                        InitWorld();
                        AddLog("Jornada iniciada no Modo Solo.", Color.Gold);
                    },
                    onStartMultiplayer: () =>
                    {
                        currentState = GameState.Gameplay;
                        InitWorld();
                        AddLog($"Conectado ao servidor MMORPG como {NetworkSession.LocalPlayerName}!", Color.Green);
                        AddLog("Outros sobreviventes foram detectados no continente.", Color.SkyBlue);
                    });
            }
            else
            {
                Raylib.ClearBackground(new Color(15, 20, 16, 255)); // Fundo geral escuro e sofisticado

                if (currentMode == GameViewMode.Overworld)
                {
                    // Renderiza o Mapa Mundi Continental
                    WorldMap.DrawOverworld(ScreenWidth, ScreenHeight);
                }
                else
                {
                    // 1. Desenha o mapa local tático com Y-Sorting
                    DrawMap(playerTex, birchTex, pineTex, boulderTex);

                    // 2. Desenha a Barra Lateral Moderna à direita
                    DrawModernSidebar();

                    // 3. Desenha o Menu de Contexto flutuante (se aberto)
                    if (menuOpen)
                    {
                        DrawContextMenu();
                    }
                }

                // 4. Desenha o HUD Completo de Personagem & Inventário (se aberto via I ou Tab)
                InventoryHUD.Draw(ScreenWidth, ScreenHeight, playerTex, nutrition, hydration, energy, 100f);

                // 5. Desenha a Estação de Culinária & Processamento Nórdico (se aberta via C ou Fogueira)
                CookingHUD.Draw(ScreenWidth, ScreenHeight, (mins) => PassTurn(mins), (msg, col) => AddLog(msg, col));
            }

            Raylib.EndDrawing();
        }

        if (playerTex.HasValue) Raylib.UnloadTexture(playerTex.Value);
        if (birchTex.HasValue) Raylib.UnloadTexture(birchTex.Value);
        if (pineTex.HasValue) Raylib.UnloadTexture(pineTex.Value);
        if (boulderTex.HasValue) Raylib.UnloadTexture(boulderTex.Value);

        Raylib.CloseWindow();
    }

    private static void InitWorld()
    {
        // Gera o setor local a partir das coordenadas continentais do jogador
        entities = WorldMap.GenerateWildernessForSector(WorldMap.PlayerWorldX, WorldMap.PlayerWorldY, MapWidthTiles, MapHeightTiles);

        // Garante uma fogueira de acampamento perto do jogador inicial para cozinhar
        if (!entities.Any(e => e.Type == EntityType.Fireplace))
        {
            entities.Add(new MapEntity(playerX + 1, playerY, EntityType.Fireplace) { IsIdentified = true });
        }
    }

    private static void HandleInput()
    {
        // Se estiver no Menu Principal, o MainMenu cuida de tudo
        if (currentState == GameState.Menu) return;

        // Toggle da Tela de Mochila & Personagem (I ou TAB)
        if (Raylib.IsKeyPressed(KeyboardKey.I) || Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            InventoryHUD.IsOpen = !InventoryHUD.IsOpen;
            menuOpen = false;
        }

        // Se a mochila estiver aberta, gerencia apenas o fechamento via ESC
        if (InventoryHUD.IsOpen)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                InventoryHUD.IsOpen = false;
            }
            return; // Bloqueia movimentação enquanto mexe na mochila
        }

        // Toggle da Estação de Culinária & Processamento (C)
        if (Raylib.IsKeyPressed(KeyboardKey.C))
        {
            if (CookingHUD.IsOpen)
            {
                CookingHUD.IsOpen = false;
            }
            else
            {
                CookingHUD.Open();
            }
            menuOpen = false;
        }

        // Se a tela de culinária estiver aberta, gerencia fechamento via ESC ou C
        if (CookingHUD.IsOpen)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.C))
            {
                CookingHUD.IsOpen = false;
            }
            return; // Bloqueia movimentação enquanto cozinha
        }

        // SE ESTIVER NO MAPA MUNDI (OVERWORLD)
        if (currentMode == GameViewMode.Overworld)
        {
            int wdx = 0;
            int wdy = 0;
            if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W)) wdy = -1;
            else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S)) wdy = 1;
            else if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A)) wdx = -1;
            else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D)) wdx = 1;

            if (wdx != 0 || wdy != 0)
            {
                int nextWx = Math.Clamp(WorldMap.PlayerWorldX + wdx, 0, WorldMap.WorldWidth - 1);
                int nextWy = Math.Clamp(WorldMap.PlayerWorldY + wdy, 0, WorldMap.WorldHeight - 1);
                var targetTile = WorldMap.Tiles[nextWx, nextWy];

                if (targetTile.Biome != BiomeType.DeepWater)
                {
                    WorldMap.PlayerWorldX = nextWx;
                    WorldMap.PlayerWorldY = nextWy;
                    PassTurn(targetTile.MovementMinutes);
                }
            }

            // TECLA ENTER: Dá o ZOOM para o mapa local tático!
            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
            {
                var curTile = WorldMap.Tiles[WorldMap.PlayerWorldX, WorldMap.PlayerWorldY];
                if (curTile.Biome != BiomeType.DeepWater)
                {
                    currentMode = GameViewMode.Wilderness;
                    entities = WorldMap.GenerateWildernessForSector(WorldMap.PlayerWorldX, WorldMap.PlayerWorldY, MapWidthTiles, MapHeightTiles);
                    playerX = MapWidthTiles / 2;
                    playerY = MapHeightTiles / 2;
                    AddLog($"Você entra no setor ({WorldMap.PlayerWorldX}, {WorldMap.PlayerWorldY}): {curTile.Name}.", Color.Gold);
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.M))
            {
                currentMode = GameViewMode.Wilderness;
            }
            return;
        }

        // SE ESTIVER NO MAPA LOCAL (WILDERNESS)
        if (Raylib.IsKeyPressed(KeyboardKey.M))
        {
            currentMode = GameViewMode.Overworld;
            menuOpen = false;
            return;
        }

        // Se clicar fora com botão esquerdo e o menu estava aberto, fecha o menu
        Vector2 mouse = Raylib.GetMousePosition();
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && menuOpen)
        {
            Rectangle menuRect = new Rectangle(menuPos.X, menuPos.Y, 220, menuActions.Count * 30 + 10);
            if (!Raylib.CheckCollisionPointRec(mouse, menuRect))
            {
                menuOpen = false;
            }
        }

        // Interação com Botão Direito no Mapa (Abrir Menu de Contexto)
        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
        {
            int mapOffsetX = 20;
            int mapOffsetY = 20;
            int clickedTileX = (int)((mouse.X - mapOffsetX) / TileSize);
            int clickedTileY = (int)((mouse.Y - mapOffsetY) / TileSize);

            if (clickedTileX >= 0 && clickedTileX < MapWidthTiles && clickedTileY >= 0 && clickedTileY < MapHeightTiles)
            {
                OpenContextMenuForTile(clickedTileX, clickedTileY, mouse);
            }
        }

        // Movimentação em 4 DIREÇÕES no Mapa Local (WASD e Setas)
        int dx = 0;
        int dy = 0;

        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W)) dy = -1;
        else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S)) dy = 1;
        else if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A)) dx = -1;
        else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D)) dx = 1;
        else if (Raylib.IsKeyPressed(KeyboardKey.Space)) { PassTurn(1, "Você aguarda atento ao som da floresta."); }

        if (dx != 0 || dy != 0)
        {
            int targetX = playerX + dx;
            int targetY = playerY + dy;

            // Transição ao andar para além da borda do mapa local!
            if (targetX < 0 || targetX >= MapWidthTiles || targetY < 0 || targetY >= MapHeightTiles)
            {
                currentMode = GameViewMode.Overworld;
                WorldMap.PlayerWorldX = Math.Clamp(WorldMap.PlayerWorldX + dx, 0, WorldMap.WorldWidth - 1);
                WorldMap.PlayerWorldY = Math.Clamp(WorldMap.PlayerWorldY + dy, 0, WorldMap.WorldHeight - 1);
                AddLog("Você atingiu a borda do setor e retornou ao Mapa Mundi continental.", Color.SkyBlue);
                menuOpen = false;
                return;
            }

            // Colisão com rochas ou árvores sólidas
            var blocker = entities.FirstOrDefault(e => e.X == targetX && e.Y == targetY);
            if (blocker != null && blocker.Type != EntityType.FallenLog && blocker.Type != EntityType.Fireplace)
            {
                string name = blocker.IsIdentified ? GetEntityName(blocker.Type) : "obstáculo à sua frente";
                AddLog($"Caminho bloqueado por {name}. Use o botão direito para interagir.", Color.Orange);
            }
            else
            {
                playerX = targetX;
                playerY = targetY;
                PassTurn(1);
            }
            menuOpen = false;
        }
    }

    private static void OpenContextMenuForTile(int tx, int ty, Vector2 screenPos)
    {
        menuActions.Clear();
        int dist = Math.Max(Math.Abs(playerX - tx), Math.Abs(playerY - ty));
        bool isClose = dist <= 1; // Alcance direto de toque/interação (1 tile)

        var ent = entities.FirstOrDefault(e => e.X == tx && e.Y == ty);

        if (ent != null)
        {
            // 1. AÇÃO: INSPECIONAR (Sempre disponível, mesmo de longe)
            menuActions.Add(new ContextAction
            {
                Label = ent.IsIdentified ? $"🔍 Inspecionar ({GetEntityName(ent.Type)})" : "🔍 Inspecionar (Descobrir)",
                TextColor = Color.Yellow,
                Execute = () =>
                {
                    ent.IsIdentified = true;
                    if (ent.Type == EntityType.Birch)
                        AddLog("Você examina a árvore: É uma Bétula saudável (casca excelente).", Color.Gold);
                    else if (ent.Type == EntityType.Pine)
                        AddLog("Você examina a árvore: É um Pinheiro Boreal maduro (madeira forte).", Color.Gold);
                    else if (ent.Type == EntityType.Boulder)
                        AddLog("Você examina a rocha: Bloco de granito firme com musgo antigo.", Color.Gold);
                    else if (ent.Type == EntityType.FallenLog)
                        AddLog("Um tronco de madeira caído pronto para ser cortado em tábuas.", Color.Gold);
                    else if (ent.Type == EntityType.Fireplace)
                        AddLog("Uma fogueira de pedras com brasas aquecendo o ambiente.", Color.Orange);

                    PassTurn(1);
                }
            });

            // AÇÕES QUE REQUEREM ESTAR PERTO
            if (isClose)
            {
                if (ent.Type == EntityType.Birch)
                {
                    menuActions.Add(new ContextAction
                    {
                        Label = "🪓 Derrubar Bétula",
                        TextColor = Color.RayWhite,
                        Execute = () =>
                        {
                            ent.Type = EntityType.FallenLog;
                            ent.IsIdentified = true;
                            AddLog("CRACK! Você golpeia com o machado e a bétula tomba no chão!", Color.Green);
                            energy = Math.Max(0, energy - 8);
                            PassTurn(20);
                        }
                    });

                    menuActions.Add(new ContextAction
                    {
                        Label = "🌿 Arrancar Casca (Birch Bark)",
                        TextColor = Color.RayWhite,
                        Execute = () =>
                        {
                            AddLog("Você retira tiras de casca flexível de bétula para artesanato.", Color.Green);
                            PassTurn(5);
                        }
                    });
                }
                else if (ent.Type == EntityType.Pine)
                {
                    menuActions.Add(new ContextAction
                    {
                        Label = "🪓 Derrubar Pinheiro",
                        TextColor = Color.RayWhite,
                        Execute = () =>
                        {
                            ent.Type = EntityType.FallenLog;
                            ent.IsIdentified = true;
                            AddLog("TIMBER! O pinheiro gigante cai estrondosamente na neve!", Color.Green);
                            energy = Math.Max(0, energy - 10);
                            PassTurn(25);
                        }
                    });

                    menuActions.Add(new ContextAction
                    {
                        Label = "🌲 Colher Agulhas e Ramos",
                        TextColor = Color.RayWhite,
                        Execute = () =>
                        {
                            AddLog("Você colhe ramos verdes de pinheiro para isolar sua cama do frio.", Color.Green);
                            PassTurn(4);
                        }
                    });
                }
                else if (ent.Type == EntityType.Boulder)
                {
                    menuActions.Add(new ContextAction
                    {
                        Label = "🪨 Coletar Pedras Soltas",
                        TextColor = Color.RayWhite,
                        Execute = () =>
                        {
                            AddLog("Você recolhe 3 pedras duras ao redor do bloco rochoso.", Color.Green);
                            PassTurn(2);
                        }
                    });

                    menuActions.Add(new ContextAction
                    {
                        Label = "⛏️ Lascar Pedra (Flake Stone)",
                        TextColor = Color.RayWhite,
                        Execute = () =>
                        {
                            AddLog("Tink! Tink! Você lasca uma pedra afiada para usar como ponta de lança.", Color.Green);
                            energy = Math.Max(0, energy - 3);
                            PassTurn(15);
                        }
                    });
                }
                else if (ent.Type == EntityType.FallenLog)
                {
                    menuActions.Add(new ContextAction
                    {
                        Label = "🪓 Cortar em Toras de Lenha",
                        TextColor = Color.RayWhite,
                        Execute = () =>
                        {
                            AddLog("Você talha o tronco em toretes ideais para fogueira e construção.", Color.Green);
                            energy = Math.Max(0, energy - 6);
                            PassTurn(15);
                        }
                    });
                }
                else if (ent.Type == EntityType.Fireplace)
                {
                    menuActions.Add(new ContextAction
                    {
                        Label = "🍲 Cozinhar / Processar (C)",
                        TextColor = Color.Gold,
                        Execute = () =>
                        {
                            CookingHUD.Open();
                        }
                    });

                    menuActions.Add(new ContextAction
                    {
                        Label = "🔥 Alimentar Fogo (Lenha)",
                        TextColor = Color.Orange,
                        Execute = () =>
                        {
                            AddLog("Você alimenta as brasas com gravetos. O calor aumenta!", Color.Orange);
                            bodyWarmth = Math.Min(100, bodyWarmth + 10);
                            PassTurn(5);
                        }
                    });
                }
            }
            else
            {
                menuActions.Add(new ContextAction
                {
                    Label = "(Aproxime-se para interagir)",
                    TextColor = Color.DarkGray,
                    Execute = () => { }
                });
            }
        }
        else
        {
            // CHÃO VAZIO
            menuActions.Add(new ContextAction
            {
                Label = "🔍 Examinar Solo",
                TextColor = Color.Yellow,
                Execute = () =>
                {
                    AddLog("Chão coberto por musgo úmido e neve rasa. Nenhuma pegada recente.", Color.LightGray);
                    PassTurn(1);
                }
            });

            menuActions.Add(new ContextAction
            {
                Label = "🍲 Culinária e Artesanato (C)",
                TextColor = Color.Gold,
                Execute = () =>
                {
                    CookingHUD.Open();
                }
            });

            if (isClose)
            {
                menuActions.Add(new ContextAction
                {
                    Label = "🔥 Montar Fogueira Simples",
                    TextColor = Color.Orange,
                    Execute = () =>
                    {
                        entities.Add(new MapEntity(tx, ty, EntityType.Fireplace) { IsIdentified = true });
                        AddLog("Fogo aceso! As labaredas espantam a névoa e aquecem o ar ao redor.", Color.Orange);
                        bodyWarmth = Math.Min(100, bodyWarmth + 15);
                        PassTurn(10);
                    }
                });

                menuActions.Add(new ContextAction
                {
                    Label = "⛺ Montar Abrigo de Galhos",
                    TextColor = Color.RayWhite,
                    Execute = () =>
                    {
                        AddLog("Você constrói um abrigo simples (leanto) com galhos para passar a noite.", Color.Green);
                        energy = Math.Max(0, energy - 12);
                        PassTurn(45);
                    }
                });
            }
        }

        // Posiciona a janela do menu sem sair da tela
        float clampedX = Math.Min(screenPos.X, ScreenWidth - 240);
        float clampedY = Math.Min(screenPos.Y, ScreenHeight - (menuActions.Count * 32 + 20));
        menuPos = new Vector2(clampedX, clampedY);
        menuOpen = true;
    }

    private static void PassTurn(int turns, string? customMessage = null)
    {
        turnsElapsed += turns;
        gameMinute += turns;
        while (gameMinute >= 60)
        {
            gameMinute -= 60;
            gameHour = (gameHour + 1) % 24;
        }

        // Fisiologia contínua
        nutrition = Math.Max(0, nutrition - (turns * 0.05f));
        hydration = Math.Max(0, hydration - (turns * 0.08f));
        energy = Math.Max(0, energy - (turns * 0.04f));

        if (customMessage != null)
        {
            AddLog(customMessage, Color.RayWhite);
        }
    }

    public static void ApplyCookingEffects(RecipeSurvivalEffects eff)
    {
        bodyWarmth = Math.Clamp(bodyWarmth + eff.WarmthBonus, 0, 100);
        nutrition = Math.Clamp(nutrition + eff.Nutrition, 0, 100);
        hydration = Math.Clamp(hydration + eff.Thirst, 0, 100);
        energy = Math.Clamp(energy + eff.Energy, 0, 100);
    }

    private static void UpdateGame()
    {
        // Se perto de uma fogueira, aquece
        bool nearFire = entities.Any(e => e.Type == EntityType.Fireplace && Math.Abs(e.X - playerX) <= 1 && Math.Abs(e.Y - playerY) <= 1);
        if (nearFire)
        {
            bodyWarmth = Math.Min(100f, bodyWarmth + 0.1f);
        }
        else
        {
            bodyWarmth = Math.Max(20f, bodyWarmth - 0.02f);
        }
    }

    private static void DrawMap(Texture2D? playerTex, Texture2D? birchTex, Texture2D? pineTex, Texture2D? boulderTex)
    {
        int mapOffsetX = 20;
        int mapOffsetY = 20;
        int mapPixelW = MapWidthTiles * TileSize;
        int mapPixelH = MapHeightTiles * TileSize;

        // Borda do mapa
        Raylib.DrawRectangle(mapOffsetX - 4, mapOffsetY - 4, mapPixelW + 8, mapPixelH + 8, new Color(40, 55, 42, 255));
        Raylib.DrawRectangle(mapOffsetX, mapOffsetY, mapPixelW, mapPixelH, new Color(22, 36, 24, 255));

        // Tilemap de base (Chão da floresta)
        for (int x = 0; x < MapWidthTiles; x++)
        {
            for (int y = 0; y < MapHeightTiles; y++)
            {
                int px = mapOffsetX + x * TileSize;
                int py = mapOffsetY + y * TileSize;

                // Gradezinha sutil
                Raylib.DrawRectangleLines(px, py, TileSize, TileSize, new Color(28, 46, 30, 255));

                // Detalhe de textura orgânica
                if ((x + y * 3) % 4 == 0)
                {
                    Raylib.DrawCircle(px + 10, py + 12, 1.5f, new Color(38, 62, 40, 255));
                }
            }
        }

        // Highlight do mouse sob o tile
        Vector2 mouse = Raylib.GetMousePosition();
        int hoverTileX = (int)((mouse.X - mapOffsetX) / TileSize);
        int hoverTileY = (int)((mouse.Y - mapOffsetY) / TileSize);
        if (hoverTileX >= 0 && hoverTileX < MapWidthTiles && hoverTileY >= 0 && hoverTileY < MapHeightTiles)
        {
            int hx = mapOffsetX + hoverTileX * TileSize;
            int hy = mapOffsetY + hoverTileY * TileSize;
            Raylib.DrawRectangle(hx, hy, TileSize, TileSize, new Color(255, 255, 255, 25));
            Raylib.DrawRectangleLines(hx, hy, TileSize, TileSize, new Color(200, 230, 150, 120));
        }

        // ORDENAÇÃO DE PROFUNDIDADE (Y-SORTING)
        // Desenha todas as entidades e o jogador ordenados pelo seu Y de base!
        var renderList = new List<(int yOrder, Action drawAction)>();

        // Entidades do mapa
        foreach (var ent in entities)
        {
            int ex = mapOffsetX + ent.X * TileSize;
            int ey = mapOffsetY + ent.Y * TileSize;

            if (ent.Type == EntityType.Birch && birchTex.HasValue)
            {
                renderList.Add((ent.Y, () =>
                {
                    float h = TileSize * 2.4f;
                    float w = h * (birchTex.Value.Width / (float)birchTex.Value.Height);
                    float drawX = ex + (TileSize - w) / 2f;
                    float drawY = ey + TileSize - h; // Raiz na base do tile
                    DrawTextureSmooth(birchTex.Value, drawX, drawY, w, h);
                }));
            }
            else if (ent.Type == EntityType.Pine && pineTex.HasValue)
            {
                renderList.Add((ent.Y, () =>
                {
                    float h = TileSize * 2.5f;
                    float w = h * (pineTex.Value.Width / (float)pineTex.Value.Height);
                    float drawX = ex + (TileSize - w) / 2f;
                    float drawY = ey + TileSize - h;
                    DrawTextureSmooth(pineTex.Value, drawX, drawY, w, h);
                }));
            }
            else if (ent.Type == EntityType.Boulder && boulderTex.HasValue)
            {
                renderList.Add((ent.Y, () =>
                {
                    float h = TileSize * 1.1f;
                    float w = h * (boulderTex.Value.Width / (float)boulderTex.Value.Height);
                    float drawX = ex + (TileSize - w) / 2f;
                    float drawY = ey + TileSize - h;
                    DrawTextureSmooth(boulderTex.Value, drawX, drawY, w, h);
                }));
            }
            else if (ent.Type == EntityType.FallenLog)
            {
                renderList.Add((ent.Y, () =>
                {
                    Raylib.DrawRectangle(ex + 4, ey + 18, TileSize - 8, 12, new Color(110, 75, 45, 255));
                    Raylib.DrawRectangleLines(ex + 4, ey + 18, TileSize - 8, 12, new Color(70, 45, 25, 255));
                }));
            }
            else if (ent.Type == EntityType.Fireplace)
            {
                renderList.Add((ent.Y, () =>
                {
                    Raylib.DrawCircle(ex + TileSize / 2, ey + TileSize / 2, 10, Color.Orange);
                    Raylib.DrawCircle(ex + TileSize / 2, ey + TileSize / 2, 5, Color.Yellow);
                }));
            }
        }

        // Jogador Local
        renderList.Add((playerY, () =>
        {
            int px = mapOffsetX + playerX * TileSize;
            int py = mapOffsetY + playerY * TileSize;

            if (playerTex.HasValue)
            {
                float h = TileSize * 1.4f;
                float w = h * (playerTex.Value.Width / (float)playerTex.Value.Height);
                float drawX = px + (TileSize - w) / 2f;
                float drawY = py + TileSize - h; // Pés na base do tile!
                DrawTextureSmooth(playerTex.Value, drawX, drawY, w, h);
            }
            else
            {
                Raylib.DrawCircle(px + TileSize / 2, py + TileSize / 2, TileSize / 3, Color.Gold);
            }
        }));

        // Outros Jogadores Conectados no Servidor (MMORPG)
        if (NetworkSession.CurrentMode != NetworkMode.Singleplayer)
        {
            foreach (var other in NetworkSession.OtherPlayers.Values)
            {
                if (other.WorldX == WorldMap.PlayerWorldX && other.WorldY == WorldMap.PlayerWorldY && !other.IsInOverworld)
                {
                    renderList.Add((other.LocalY, () =>
                    {
                        int ox = mapOffsetX + other.LocalX * TileSize;
                        int oy = mapOffsetY + other.LocalY * TileSize;

                        if (playerTex.HasValue)
                        {
                            float h = TileSize * 1.4f;
                            float w = h * (playerTex.Value.Width / (float)playerTex.Value.Height);
                            float drawX = ox + (TileSize - w) / 2f;
                            float drawY = oy + TileSize - h;
                            // Outros jogadores têm um tom levemente azulado para diferenciar
                            Raylib.DrawTexturePro(playerTex.Value, new Rectangle(0, 0, playerTex.Value.Width, playerTex.Value.Height), new Rectangle(drawX, drawY, w, h), Vector2.Zero, 0, new Color(180, 220, 255, 255));
                        }
                        else
                        {
                            Raylib.DrawCircle(ox + TileSize / 2, oy + TileSize / 2, TileSize / 3, Color.SkyBlue);
                        }

                        // Placa com o nome do outro jogador flutuando sobre a cabeça
                        int nameW = Raylib.MeasureText(other.Name, 10);
                        int badgeX = ox + TileSize / 2 - nameW / 2 - 4;
                        int badgeY = oy - 16;
                        Raylib.DrawRectangle(badgeX, badgeY, nameW + 8, 14, new Color(12, 18, 30, 220));
                        Raylib.DrawRectangleLines(badgeX, badgeY, nameW + 8, 14, Color.SkyBlue);
                        Raylib.DrawText(other.Name, badgeX + 4, badgeY + 2, 10, Color.SkyBlue);
                    }));
                }
            }
        }

        // Renderiza tudo ordenado pelo eixo Y (quem está mais embaixo fica na frente)
        foreach (var item in renderList.OrderBy(i => i.yOrder))
        {
            item.drawAction();
        }
    }

    private static void DrawModernSidebar()
    {
        int x = MapWidthTiles * TileSize + 40;
        int y = 20;
        int width = ScreenWidth - x - 20;
        int height = ScreenHeight - 40;
        Vector2 mouse = Raylib.GetMousePosition();

        // Painel lateral estilo dashboard
        UiTheme.Panel(x, y, width, height, 0.05f, UiTheme.Card, UiTheme.Border);

        // 1. TÍTULO E RELÓGIO NÓRDICO
        UiTheme.Text("UNREAL WORLD", x + 16, y + 15, 18, UiTheme.TextPrimary, bold: true, spacing: 1.6f);
        float uw = UiTheme.TextSize("UNREAL WORLD", 18, true, 1.6f).X;
        Rectangle chip = new Rectangle(x + 16 + uw + 10, y + 17, 78, 21);
        Raylib.DrawRectangleRounded(chip, 0.45f, 6, new Color(28, 58, 42, 255));
        Raylib.DrawRectangleRoundedLines(chip, 0.45f, 6, new Color(72, 142, 102, 255));
        UiTheme.Text("REBORN", chip.X + 10, chip.Y + 4.5f, 11, UiTheme.Success, bold: true, spacing: 2.2f);

        string timeStr = $"{gameHour:D2}:{gameMinute:D2}";
        string netText = NetworkSession.CurrentMode == NetworkMode.Singleplayer ? "Solo" : $"MMORPG ({NetworkSession.LocalPlayerName})";
        UiTheme.Text($"Outono  |  {timeStr}  |  {netText}", x + 16, y + 44, 12, UiTheme.TextSecondary);

        UiTheme.Separator(x + 16, y + 64, width - 32);

        // 2. BARRAS MODERNAS DE FISIOLOGIA
        int barY = y + 74;
        DrawStatusBar(x + 16, barY, width - 32, "Calor Corporal", bodyWarmth, 100, new Color(225, 115, 45, 255));
        DrawStatusBar(x + 16, barY + 36, width - 32, "Nutrição (Fome)", nutrition, 100, new Color(80, 190, 100, 255));
        DrawStatusBar(x + 16, barY + 72, width - 32, "Hidratação (Sede)", hydration, 100, new Color(60, 155, 230, 255));
        DrawStatusBar(x + 16, barY + 108, width - 32, "Vigor (Energia)", energy, 100, new Color(230, 195, 55, 255));

        int sep2Y = barY + 154;
        UiTheme.Separator(x + 16, sep2Y, width - 32);

        // 3. DIÁRIO DE EVENTOS / LOG DA JORNADA
        UiTheme.Header(x + 16, sep2Y + 9, "Registro de Sobrevivencia", UiTheme.Gold, 11); // acentos ok via codepoints

        int logY = sep2Y + 30;
        int maxLogs = 6;
        var recentLogs = messageLog.TakeLast(maxLogs).Reverse().ToList();

        for (int i = 0; i < recentLogs.Count; i++)
        {
            var msg = recentLogs[i];
            int alpha = 255 - (i * 30);
            Color c = new Color(msg.color.R, msg.color.G, msg.color.B, (byte)Math.Max(50, alpha));
            UiTheme.Text($"• {msg.text}", x + 16, logY + (i * 22), 11.5f, c);
        }

        // 4. BOTÕES DE ACESSO RÁPIDO (cards de ação)
        // Botão 1: Mochila [I]
        int btn1Y = y + height - 160;
        Rectangle invBtnRec = new Rectangle(x + 16, btn1Y, width - 32, 28);
        bool isInvHover = Raylib.CheckCollisionPointRec(mouse, invBtnRec);
        UiTheme.Panel(invBtnRec.X, invBtnRec.Y, invBtnRec.Width, invBtnRec.Height, 0.14f,
            isInvHover ? UiTheme.AccentDim : UiTheme.Card,
            isInvHover ? UiTheme.Accent : UiTheme.Border,
            shadow: isInvHover);
        UiTheme.Text("Mochila & Equipamentos", x + 26, btn1Y + 7.5f, 12.5f,
            isInvHover ? Color.White : UiTheme.TextPrimary, bold: true);
        UiTheme.Text("[I]", x + width - 40, btn1Y + 8, 11, UiTheme.TextMuted, bold: true);

        if (isInvHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            InventoryHUD.IsOpen = !InventoryHUD.IsOpen;
            menuOpen = false;
        }

        // Botão 2: Mapa Mundi [M]
        int btn2Y = y + height - 124;
        Rectangle mapBtnRec = new Rectangle(x + 16, btn2Y, width - 32, 28);
        bool isMapHover = Raylib.CheckCollisionPointRec(mouse, mapBtnRec);
        UiTheme.Panel(mapBtnRec.X, mapBtnRec.Y, mapBtnRec.Width, mapBtnRec.Height, 0.14f,
            isMapHover ? UiTheme.AccentDim : UiTheme.Card,
            isMapHover ? UiTheme.Accent : UiTheme.Border,
            shadow: isMapHover);
        UiTheme.Text("Mapa Mundi Continental", x + 26, btn2Y + 7.5f, 12.5f,
            isMapHover ? Color.White : UiTheme.TextPrimary, bold: true);
        UiTheme.Text("[M]", x + width - 40, btn2Y + 8, 11, UiTheme.TextMuted, bold: true);

        if (isMapHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            currentMode = GameViewMode.Overworld;
            menuOpen = false;
        }

        // Botão 3: Voltar ao Menu Principal
        int btn3Y = y + height - 88;
        Rectangle menuBtnRec = new Rectangle(x + 16, btn3Y, width - 32, 26);
        bool isMenuHover = Raylib.CheckCollisionPointRec(mouse, menuBtnRec);
        UiTheme.Panel(menuBtnRec.X, menuBtnRec.Y, menuBtnRec.Width, menuBtnRec.Height, 0.14f,
            isMenuHover ? new Color(74, 36, 38, 255) : new Color(38, 24, 25, 255),
            isMenuHover ? UiTheme.Danger : new Color(92, 48, 50, 255),
            shadow: isMenuHover);
        UiTheme.Text("Menu Principal (Desconectar)", x + 26, btn3Y + 6.5f, 11.5f,
            isMenuHover ? Color.White : UiTheme.TextSecondary, bold: isMenuHover);

        if (isMenuHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            currentState = GameState.Menu;
            MainMenu.CurrentScreen = MenuScreen.Title;
            menuOpen = false;
        }

        // 5. ATALHOS RÁPIDOS NA BASE
        int footerY = y + height - 56;
        UiTheme.Separator(x + 16, footerY + 6, width - 32);
        UiTheme.Text("WASD: Mover  •  Botão Dir: Ações  •  [M]: Mapa Mundi", x + 16, footerY + 14, 10.5f, UiTheme.TextMuted);
    }

    private static void DrawStatusBar(int x, int y, int width, string label, float current, float max, Color fillColor)
    {
        // Título e valor
        UiTheme.Text(label, x, y, 12, UiTheme.TextSecondary, bold: true, spacing: 1.2f);
        string valStr = $"{(int)current}%";
        Vector2 vs = UiTheme.TextSize(valStr, 11);
        UiTheme.Text(valStr, x + width - vs.X, y, 11, UiTheme.TextPrimary);

        // Barra arredondada estilo dashboard
        UiTheme.StatBar(x, y + 17, width, 9, current / max, fillColor);
    }

    private static void RunSelfTest()
    {
        Console.WriteLine("=== SELFTEST: INVENTARIO ESPACIAL & COZINHA ===");

        // [1] Starter kit real (sem save prévio)
        string savePath = Path.Combine(AppContext.BaseDirectory, "save_inventory.txt");
        if (File.Exists(savePath)) File.Delete(savePath);
        InventoryHUD.SpatialItems.Clear();
        InventoryHUD.Equipment = new InventoryItem?[3, 3];
        InventoryHUD.InitStarterKit();
        Console.WriteLine($"[1] Itens no grid pos-starter-kit: {InventoryHUD.SpatialItems.Count} (esperado 8)");

        // [2..4] Contagem por tags/pesquisa em SpatialItems + Equipment
        var berriesReq = new RecipeIngredientRequirement { Tag = "Berries_Wild", Qty = 1 };
        var waterReq = new RecipeIngredientRequirement { Tag = "Liquid_Water", Qty = 1 };
        var knifeReq = new RecipeIngredientRequirement { Tag = "Tool_Knife", Qty = 1 };
        Console.WriteLine($"[2] Berries_Wild = {CookingSystem.GetPlayerItemCount(berriesReq)} (esperado >=1)");
        Console.WriteLine($"[3] Liquid_Water = {CookingSystem.GetPlayerItemCount(waterReq)} (esperado >=1)");
        Console.WriteLine($"[4] Tool_Knife equipado = {CookingSystem.GetPlayerItemCount(knifeReq)} (esperado >=1)");

        // [5] Consumo via fluxo do TryCook (count de berries antes/depois)
        int berriesBefore = CookingSystem.GetPlayerItemCount(berriesReq);
        var recipe = new CookingRecipe
        {
            Name = "Teste",
            PrepMinutes = 1,
            CookMinutes = 1,
            Ingredients = new() { new RecipeIngredientRequirement { Tag = "Berries_Wild", Qty = 1 } },
            Products = new() { new RecipeProduct { ItemId = "soup_test", Name = "Extrato Teste", QtyMin = 1, QtyMax = 1 } },
            SurvivalEffects = new() { Nutrition = 10 }
        };
        bool cooked = CookingSystem.TryCook(recipe, out string msg, out int mins);
        Console.WriteLine($"[5] TryCook = {cooked};berries {berriesBefore}->{CookingSystem.GetPlayerItemCount(berriesReq)};produto 'soup_test' no grid = {InventoryHUD.SpatialItems.Any(i => i.Id == "soup_test")}");

        // [6] Persistência: salvar / apagar memória / recarregar
        InventoryHUD.SaveState();
        bool fileExists = File.Exists(savePath);
        int countBefore = InventoryHUD.SpatialItems.Count;
        InventoryHUD.SpatialItems.Clear();
        bool loaded = InventoryHUD.TryLoadState();
        Console.WriteLine($"[6] SaveState escreveu arquivo = {fileExists}");
        Console.WriteLine($"[7] Recarga = {loaded} / itens após recarga = {InventoryHUD.SpatialItems.Count} (antes {countBefore})");

        // [8] Capacidade derivada da Força
        Console.WriteLine($"[8] Força = {InventoryHUD.Strength} -> MaxWeightKg = {InventoryHUD.MaxWeightKg:0.0} kg (esperado 25.2)");

        // Limpa o save de teste para manter o primeiro run do usuário limpo
        if (File.Exists(savePath)) File.Delete(savePath);

        Console.WriteLine("=== FIM DO SELFTEST ===");
    }

    private static void DrawContextMenu()
    {
        int itemH = 30;
        int menuW = 240;
        int menuH = menuActions.Count * itemH + 26;

        // Painel estilo dashboard com sombra e borda em realce
        UiTheme.Panel(menuPos.X, menuPos.Y, menuW, menuH, 0.10f, UiTheme.Card, UiTheme.BorderHover);

        Vector2 mouse = Raylib.GetMousePosition();

        // Micro-header do menu
        UiTheme.Label("Ações", menuPos.X + 12, menuPos.Y + 2, 8.5f, UiTheme.TextMuted, bold: true, spacing: 2f);

        for (int i = 0; i < menuActions.Count; i++)
        {
            var action = menuActions[i];
            int itemY = (int)menuPos.Y + 20 + (i * itemH);
            Rectangle itemRect = new Rectangle(menuPos.X + 5, itemY, menuW - 10, itemH - 2);

            bool isHover = Raylib.CheckCollisionPointRec(mouse, itemRect);
            if (isHover)
            {
                Raylib.DrawRectangleRounded(itemRect, 0.25f, 4, UiTheme.AccentDim);
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    action.Execute();
                    menuOpen = false;
                    break;
                }
            }

            UiTheme.Text(action.Label, (int)menuPos.X + 13, itemY + 8, 12,
                isHover ? UiTheme.TextPrimary : action.TextColor, bold: isHover);
        }
    }

    private static void AddLog(string text, Color color)
    {
        messageLog.Add((text, color));
        if (messageLog.Count > 40)
        {
            messageLog.RemoveAt(0);
        }
    }

    private static string GetEntityName(EntityType type) => type switch
    {
        EntityType.Birch => "Árvore de Bétula",
        EntityType.Pine => "Pinheiro Boreal",
        EntityType.Boulder => "Rocha de Granito",
        EntityType.FallenLog => "Tronco de Madeira Caído",
        EntityType.Fireplace => "Fogueira",
        _ => "Elemento"
    };

    private static Texture2D? LoadSprite(string path)
    {
        if (!File.Exists(path)) return null;
        Texture2D tex = Raylib.LoadTexture(path);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        return tex;
    }

    private static void DrawTextureSmooth(Texture2D tex, float x, float y, float w, float h)
    {
        Rectangle src = new Rectangle(0, 0, tex.Width, tex.Height);
        Rectangle dest = new Rectangle(x, y, w, h);
        Raylib.DrawTexturePro(tex, src, dest, Vector2.Zero, 0, Color.White);
    }
}
