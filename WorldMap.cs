using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace UrwReborn;

public enum BiomeType
{
    DeepWater,
    ShallowWater,
    Plains,
    BirchForest,
    PineTaiga,
    Swamp,
    Hills,
    Mountains,
    Settlement
}

public class WorldTile
{
    public BiomeType Biome { get; set; }
    public float Elevation { get; set; }
    public float Moisture { get; set; }
    public string Name { get; set; } = "";
    public Color Color { get; set; }
    public string Icon { get; set; } = "";
    public int MovementMinutes { get; set; } = 20; // Tempo gasto para atravessar
}

public static class WorldMap
{
    public const int WorldWidth = 64;
    public const int WorldHeight = 48;
    public const int WorldTileSize = 14; // Tamanho do pixel de cada tile no mapa mundi

    public static WorldTile[,] Tiles { get; private set; } = new WorldTile[WorldWidth, WorldHeight];
    public static int PlayerWorldX { get; set; } = 32;
    public static int PlayerWorldY { get; set; } = 24;
    public static int WorldSeed { get; set; } = 1337;

    // Deltas de modificações do jogador em cada setor local: Key = (WorldX, WorldY)
    public static Dictionary<(int wx, int wy), List<MapEntity>> LocalSectorDeltas { get; } = new();

    static WorldMap()
    {
        GenerateWorld(WorldSeed);
    }

    public static void GenerateWorld(int seed)
    {
        WorldSeed = seed;
        Random rnd = new Random(seed);

        // Gera elevação e umidade procedural com ruído suave de múltiplas oitavas
        for (int x = 0; x < WorldWidth; x++)
        {
            for (int y = 0; y < WorldHeight; y++)
            {
                float nx = x / (float)WorldWidth - 0.5f;
                float ny = y / (float)WorldHeight - 0.5f;

                // Ruído pseudo-Perlin fractal
                float elev = SampleOctaveNoise(x, y, 4, 0.06f, seed);
                float moist = SampleOctaveNoise(x, y, 3, 0.08f, seed + 9999);

                // Gradiente para criar ilhas/continente nórdico cercado de água
                float distFromCenter = MathF.Sqrt(nx * nx + ny * ny) * 1.8f;
                elev = Math.Clamp(elev - distFromCenter * 0.45f, 0f, 1f);

                // Determina o bioma com base na elevação e umidade
                Tiles[x, y] = DetermineBiome(elev, moist);
            }
        }

        // Garante que o jogador comece em terra firme (uma floresta segura)
        FindSafeSpawn();

        // Insere 2 assentamentos/aldeias tribais históricas
        PlaceSettlements(rnd);
    }

    private static float SampleOctaveNoise(int x, int y, int octaves, float frequency, int seed)
    {
        float total = 0f;
        float maxVal = 0f;
        float amp = 1f;
        float freq = frequency;

        for (int i = 0; i < octaves; i++)
        {
            float n = PseudoNoise((x + seed * 13) * freq, (y + seed * 17) * freq);
            total += n * amp;
            maxVal += amp;
            amp *= 0.5f;
            freq *= 2f;
        }

        return total / maxVal;
    }

    private static float PseudoNoise(float x, float y)
    {
        int n = (int)(x * 374761393 + y * 668265263);
        n = (n ^ (n >> 13)) * 1274126177;
        return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / (float)0x7fffffff;
    }

    private static WorldTile DetermineBiome(float elevation, float moisture)
    {
        if (elevation < 0.28f)
        {
            return new WorldTile
            {
                Biome = BiomeType.DeepWater,
                Name = "Mar Báltico (Águas Profundas)",
                Color = new Color(25, 45, 95, 255),
                Icon = "🌊",
                MovementMinutes = 60
            };
        }
        if (elevation < 0.38f)
        {
            return new WorldTile
            {
                Biome = BiomeType.ShallowWater,
                Name = "Lago / Rio Boreal",
                Color = new Color(45, 85, 145, 255),
                Icon = "💧",
                MovementMinutes = 45
            };
        }

        // Terra Firme
        if (elevation > 0.82f)
        {
            return new WorldTile
            {
                Biome = BiomeType.Mountains,
                Name = "Cordilheira Rochosa (Picos Nevados)",
                Color = new Color(160, 165, 175, 255),
                Icon = "🏔️",
                MovementMinutes = 45
            };
        }
        if (elevation > 0.70f)
        {
            return new WorldTile
            {
                Biome = BiomeType.Hills,
                Name = "Colinas com Penhascos",
                Color = new Color(110, 115, 100, 255),
                Icon = "⛰️",
                MovementMinutes = 30
            };
        }

        // Planícies, Pântanos e Florestas pela umidade
        if (moisture > 0.68f)
        {
            return new WorldTile
            {
                Biome = BiomeType.Swamp,
                Name = "Pântano e Turfeira (Mire)",
                Color = new Color(55, 68, 48, 255),
                Icon = "🌾",
                MovementMinutes = 35
            };
        }
        if (moisture > 0.44f)
        {
            return new WorldTile
            {
                Biome = BiomeType.PineTaiga,
                Name = "Taiga Fechada de Pinheiros",
                Color = new Color(20, 52, 28, 255),
                Icon = "🌲",
                MovementMinutes = 25
            };
        }
        if (moisture > 0.25f)
        {
            return new WorldTile
            {
                Biome = BiomeType.BirchForest,
                Name = "Bosque Misto de Bétulas",
                Color = new Color(42, 85, 40, 255),
                Icon = "🍃",
                MovementMinutes = 20
            };
        }

        return new WorldTile
        {
            Biome = BiomeType.Plains,
            Name = "Planície Boreal de Musgo e Líquens",
            Color = new Color(75, 105, 55, 255),
            Icon = "🌱",
            MovementMinutes = 15
        };
    }

    private static void FindSafeSpawn()
    {
        // Encontra o tile de floresta de pinheiro mais próximo do centro
        int bestDist = int.MaxValue;
        int bestX = WorldWidth / 2;
        int bestY = WorldHeight / 2;

        for (int x = 5; x < WorldWidth - 5; x++)
        {
            for (int y = 5; y < WorldHeight - 5; y++)
            {
                if (Tiles[x, y].Biome == BiomeType.PineTaiga || Tiles[x, y].Biome == BiomeType.BirchForest)
                {
                    int d = Math.Abs(x - WorldWidth / 2) + Math.Abs(y - WorldHeight / 2);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestX = x;
                        bestY = y;
                    }
                }
            }
        }

        PlayerWorldX = bestX;
        PlayerWorldY = bestY;
    }

    private static void PlaceSettlements(Random rnd)
    {
        int placed = 0;
        for (int attempts = 0; attempts < 50 && placed < 3; attempts++)
        {
            int rx = rnd.Next(8, WorldWidth - 8);
            int ry = rnd.Next(8, WorldHeight - 8);

            if (Tiles[rx, ry].Biome == BiomeType.BirchForest || Tiles[rx, ry].Biome == BiomeType.Plains)
            {
                Tiles[rx, ry] = new WorldTile
                {
                    Biome = BiomeType.Settlement,
                    Name = placed == 0 ? "Aldeia da Tribo Kaumo" : "Acampamento de Pescadores Sartola",
                    Color = new Color(175, 120, 60, 255),
                    Icon = "⛺",
                    MovementMinutes = 15
                };
                placed++;
            }
        }
    }

    /// <summary>
    /// Gera deterministicamente o mapa local tático com base no bioma e nas coordenadas do mapa mundi
    /// </summary>
    public static List<MapEntity> GenerateWildernessForSector(int wx, int wy, int localWidth, int localHeight)
    {
        var key = (wx, wy);
        if (LocalSectorDeltas.ContainsKey(key))
        {
            // Se o jogador já modificou este setor, retorna o estado salvo!
            return LocalSectorDeltas[key];
        }

        var list = new List<MapEntity>();
        var tile = Tiles[wx, wy];

        // Seed única para este setor
        int sectorSeed = HashCoords(WorldSeed, wx, wy);
        Random rnd = new Random(sectorSeed);

        // Geração por Bioma
        switch (tile.Biome)
        {
            case BiomeType.PineTaiga:
                // Taiga: densa em pinheiros (35%), poucas bétulas (5%), pedras (10%)
                SpawnEntities(list, EntityType.Pine, localWidth, localHeight, 0.30f, rnd);
                SpawnEntities(list, EntityType.Birch, localWidth, localHeight, 0.06f, rnd);
                SpawnEntities(list, EntityType.Boulder, localWidth, localHeight, 0.08f, rnd);
                break;

            case BiomeType.BirchForest:
                // Bosque: bétulas abundantes (35%), poucos pinheiros (8%), flores/rochas
                SpawnEntities(list, EntityType.Birch, localWidth, localHeight, 0.32f, rnd);
                SpawnEntities(list, EntityType.Pine, localWidth, localHeight, 0.06f, rnd);
                SpawnEntities(list, EntityType.Boulder, localWidth, localHeight, 0.06f, rnd);
                break;

            case BiomeType.Hills:
            case BiomeType.Mountains:
                // Rochoso: muitos blocos de pedra (25%), pinheiros isolados
                SpawnEntities(list, EntityType.Boulder, localWidth, localHeight, 0.24f, rnd);
                SpawnEntities(list, EntityType.Pine, localWidth, localHeight, 0.12f, rnd);
                break;

            case BiomeType.Swamp:
                // Pântano: árvores espaçadas, troncos caídos
                SpawnEntities(list, EntityType.Pine, localWidth, localHeight, 0.12f, rnd);
                SpawnEntities(list, EntityType.FallenLog, localWidth, localHeight, 0.10f, rnd);
                break;

            case BiomeType.Settlement:
                // Aldeia: fogueiras comunitárias, toras cortadas e espaço aberto
                list.Add(new MapEntity(localWidth / 2, localHeight / 2, EntityType.Fireplace) { IsIdentified = true });
                list.Add(new MapEntity(localWidth / 2 - 2, localHeight / 2, EntityType.FallenLog) { IsIdentified = true });
                list.Add(new MapEntity(localWidth / 2 + 2, localHeight / 2, EntityType.FallenLog) { IsIdentified = true });
                SpawnEntities(list, EntityType.Birch, localWidth, localHeight, 0.08f, rnd);
                break;

            default: // Plains / Shore
                SpawnEntities(list, EntityType.Birch, localWidth, localHeight, 0.08f, rnd);
                SpawnEntities(list, EntityType.Pine, localWidth, localHeight, 0.08f, rnd);
                SpawnEntities(list, EntityType.Boulder, localWidth, localHeight, 0.05f, rnd);
                break;
        }

        // Salva na memória de persistência
        LocalSectorDeltas[key] = list;
        return list;
    }

    private static void SpawnEntities(List<MapEntity> list, EntityType type, int w, int h, float density, Random rnd)
    {
        int count = (int)(w * h * density * 0.4f);
        for (int i = 0; i < count; i++)
        {
            int ex = rnd.Next(1, w - 1);
            int ey = rnd.Next(1, h - 1);

            // Não nasce no centro exato (onde o jogador vai surgir) nem em cima de outro
            if (Math.Abs(ex - w / 2) <= 1 && Math.Abs(ey - h / 2) <= 1) continue;
            if (list.Any(e => e.X == ex && e.Y == ey)) continue;

            list.Add(new MapEntity(ex, ey, type));
        }
    }

    private static int HashCoords(int seed, int x, int y)
    {
        int h = seed;
        h = (h * 397) ^ x;
        h = (h * 397) ^ y;
        return h;
    }

    /// <summary>
    /// Renderiza a tela completa do Mapa Mundi Continental
    /// </summary>
    public static void DrawOverworld(int screenW, int screenH)
    {
        // Fundo do oceano profundo
        Raylib.ClearBackground(new Color(14, 22, 38, 255));

        int mapPixelW = WorldWidth * WorldTileSize;
        int mapPixelH = WorldHeight * WorldTileSize;

        int mapX = 30;
        int mapY = 30;

        // Moldura do Mapa Mundi
        Raylib.DrawRectangle(mapX - 6, mapY - 6, mapPixelW + 12, mapPixelH + 12, new Color(48, 65, 88, 255));
        Raylib.DrawRectangle(mapX, mapY, mapPixelW, mapPixelH, new Color(18, 30, 50, 255));

        Vector2 mouse = Raylib.GetMousePosition();
        int hoverWx = (int)((mouse.X - mapX) / WorldTileSize);
        int hoverWy = (int)((mouse.Y - mapY) / WorldTileSize);

        // 1. Renderiza os tiles do continente
        for (int x = 0; x < WorldWidth; x++)
        {
            for (int y = 0; y < WorldHeight; y++)
            {
                var t = Tiles[x, y];
                int px = mapX + x * WorldTileSize;
                int py = mapY + y * WorldTileSize;

                Raylib.DrawRectangle(px, py, WorldTileSize, WorldTileSize, t.Color);

                // Pequeno detalhe para rios e montanhas
                if (t.Biome == BiomeType.Mountains)
                {
                    Raylib.DrawCircle(px + WorldTileSize / 2, py + WorldTileSize / 2, 2.5f, Color.White);
                }
                else if (t.Biome == BiomeType.Settlement)
                {
                    Raylib.DrawCircle(px + WorldTileSize / 2, py + WorldTileSize / 2, 3f, Color.Gold);
                }
            }
        }

        // 2. Realce no tile sob o cursor
        if (hoverWx >= 0 && hoverWx < WorldWidth && hoverWy >= 0 && hoverWy < WorldHeight)
        {
            int hx = mapX + hoverWx * WorldTileSize;
            int hy = mapY + hoverWy * WorldTileSize;
            Raylib.DrawRectangleLines(hx, hy, WorldTileSize, WorldTileSize, Color.Yellow);
        }

        // 3. Avatar do Jogador no Mapa Mundi (Ponto Dourado pulsante)
        int playerPx = mapX + PlayerWorldX * WorldTileSize;
        int playerPy = mapY + PlayerWorldY * WorldTileSize;
        Raylib.DrawCircle(playerPx + WorldTileSize / 2, playerPy + WorldTileSize / 2, 6f, Color.Gold);
        Raylib.DrawCircle(playerPx + WorldTileSize / 2, playerPy + WorldTileSize / 2, 3f, Color.RayWhite);

        // 4. Painel Lateral de Informações do Mapa Mundi
        int panelX = mapX + mapPixelW + 30;
        int panelW = screenW - panelX - 30;
        int panelH = mapPixelH + 12;

        UiTheme.Panel(panelX, mapY - 6, panelW, panelH, 0.05f, UiTheme.Card, UiTheme.Border);

        // Título
        UiTheme.Text("MAPA MUNDI", panelX + 20, mapY + 12, 21, UiTheme.TextPrimary, bold: true, spacing: 2.5f);
        UiTheme.Label("Fennoscândia Pré-Histórica", panelX + 20, mapY + 42, 11, new Color(160, 200, 240, 255), bold: false, spacing: 2f);
        UiTheme.Separator(panelX + 20, mapY + 60, panelW - 40);

        // Coordenadas e Bioma Atual
        var currentTile = Tiles[PlayerWorldX, PlayerWorldY];
        UiTheme.Text("Posição Continental", panelX + 20, mapY + 75, 12.5f, UiTheme.TextMuted);
        UiTheme.Text($"Setor ({PlayerWorldX}, {PlayerWorldY})", panelX + 20, mapY + 92, 15, UiTheme.TextPrimary, bold: true);

        UiTheme.Text("Bioma da Região", panelX + 20, mapY + 125, 12.5f, UiTheme.TextMuted);
        UiTheme.Text($"{currentTile.Icon} {currentTile.Name}", panelX + 20, mapY + 142, 14, UiTheme.Gold, bold: true);

        UiTheme.Text($"Custo de Viagem: {currentTile.MovementMinutes} min/passo", panelX + 20, mapY + 175, 12, UiTheme.TextSecondary);

        UiTheme.Separator(panelX + 20, mapY + 205, panelW - 40);

        // Info do cursor do mouse
        if (hoverWx >= 0 && hoverWx < WorldWidth && hoverWy >= 0 && hoverWy < WorldHeight)
        {
            var hoveredTile = Tiles[hoverWx, hoverWy];
            UiTheme.Text("Sob o Cursor", panelX + 20, mapY + 218, 11.5f, UiTheme.TextMuted);
            UiTheme.Text($"({hoverWx}, {hoverWy})  ·  {hoveredTile.Name}", panelX + 20, mapY + 234, 12, UiTheme.TextPrimary, bold: true);
        }

        // LEGENDA DOS BIOMAS
        int legY = mapY + 275;
        UiTheme.Header(panelX + 20, legY - 2, "Legenda dos Biomas", UiTheme.Gold, 11);
        DrawLegendItem(panelX + 20, legY + 22, new Color(20, 52, 28, 255), "Taiga de Pinheiros");
        DrawLegendItem(panelX + 20, legY + 42, new Color(42, 85, 40, 255), "Bosque de Bétulas");
        DrawLegendItem(panelX + 20, legY + 62, new Color(55, 68, 48, 255), "Pântano / Turfeira");
        DrawLegendItem(panelX + 20, legY + 82, new Color(75, 105, 55, 255), "Planícies de Musgo");
        DrawLegendItem(panelX + 20, legY + 102, new Color(110, 115, 100, 255), "Colinas e Penhascos");
        DrawLegendItem(panelX + 20, legY + 122, new Color(160, 165, 175, 255), "Cordilheira Rochosa");
        DrawLegendItem(panelX + 20, legY + 142, new Color(45, 85, 145, 255), "Lagos e Rios");
        DrawLegendItem(panelX + 20, legY + 162, new Color(175, 120, 60, 255), "Assentamento Tribal");

        // BOTÃO DE TRANSIÇÃO / ZOOM
        int actionY = mapY + panelH - 80;
        Raylib.DrawLine(panelX + 20, actionY, panelX + panelW - 20, actionY, new Color(50, 75, 105, 255));

        bool canEnter = currentTile.Biome != BiomeType.DeepWater;
        UiTheme.Panel(panelX + 20, actionY + 12, panelW - 40, 48, 0.14f,
            canEnter ? new Color(34, 92, 56, 255) : new Color(72, 40, 42, 255),
            canEnter ? UiTheme.Success : UiTheme.Danger);

        string enterText = canEnter ? "▶ PRESSIONE [ENTER] PARA ENTRAR NO MAPA LOCAL" : "ÁGUA PROFUNDA (PRECISA DE EMBARCAÇÃO)";
        UiTheme.Text(enterText, panelX + 32, actionY + 26, 11.5f, Color.White, bold: true, spacing: 1.2f);

        UiTheme.Text("• Setas / WASD: Viajar pelo Continente", panelX + 20, actionY + 68, 11, UiTheme.TextMuted);
    }

    private static void DrawLegendItem(int x, int y, Color c, string text)
    {
        Raylib.DrawRectangleRounded(new Rectangle(x, y + 2, 12, 12), 0.25f, 4, c);
        Raylib.DrawRectangleRoundedLines(new Rectangle(x, y + 2, 12, 12), 0.25f, 4, new Color(120, 135, 150, 255));
        UiTheme.Text(text, x + 20, y + 1, 11, UiTheme.TextPrimary);
    }
}
