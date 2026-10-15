using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Raylib_cs;

namespace UrwReborn;

public class InventoryItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Count { get; set; } = 1;
    public string Category { get; set; } = "";
    public int DurabilityPct { get; set; } = 100;
    public string DamageStr { get; set; } = "";
    public string RangeStr { get; set; } = "";
    public List<(string Name, int Current, int Required)> UpgradeComponents { get; set; } = new();

    // Propriedades Espaciais de Grade (Tetris Inventory / Estilo Tarkov)
    public int BaseWidth { get; set; } = 1;  // em células
    public int BaseHeight { get; set; } = 1; // em células
    public bool IsRotated { get; set; } = false;

    public int Width => IsRotated ? BaseHeight : BaseWidth;
    public int Height => IsRotated ? BaseWidth : BaseHeight;

    public int GridX { get; set; } = 0; // Coluna na grade espacial
    public int GridY { get; set; } = 0; // Linha na grade espacial

    public float WeightKg { get; set; } = 0.5f;
    public string StackText { get; set; } = ""; // ex: "1/75", "1/10"
}

public static class InventoryHUD
{
    public static bool IsOpen { get; set; } = false;

    // Item atualmente selecionado para exibir detalhes
    public static InventoryItem? SelectedItem { get; set; }

    // Equipamentos (Grid 3x3)
    public static InventoryItem?[,] Equipment { get; set; } = new InventoryItem?[3, 3];

    // ─── Atributos & capacidade ───
    public static int Strength { get; set; } = 14; // Atributo Força do URW (exibido no HUD)
    public static float MaxWeightKg => Strength * 1.8f; // Capacidade de carga derivada da Força

    // Lista ÚNICA de Itens no Inventário Espacial de Grade (fonte de verdade do jogo)
    public static List<InventoryItem> SpatialItems { get; set; } = new();

    // Configurações da Grade Espacial
    public const int GridCols = 10;
    public const int GridRows = 6;
    public const int CellSize = 42;

    // Estado de Arraste e Rotação
    public static InventoryItem? DraggedItem { get; private set; } = null;
    private static int dragOriginalX = 0;
    private static int dragOriginalY = 0;
    private static bool dragOriginalRotated = false;
    private static int dragGrabOffsetCellX = 0;
    private static int dragGrabOffsetCellY = 0;

    static InventoryHUD()
    {
        InitStarterKit();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Starter Kit do Sobrevivente (spawn real, data-driven em código)
    // As Categories batem com as TAGS de receita do CookingSystem
    // (ex: "Carne" → Meat_Quadruped, "água" → Liquid_Water, etc.)
    // ──────────────────────────────────────────────────────────────────────
    public static void InitStarterKit()
    {
        SpatialItems.Clear();
        Equipment = new InventoryItem?[3, 3];
        SelectedItem = null;

        // State persistido entre sessões tem prioridade
        if (TryLoadState())
        {
            return;
        }

        // Equipamentos iniciais (Grid 3x3)
        Equipment[0, 0] = new InventoryItem { Id = "coat", Name = "Casaco de Pele", Icon = "🧥", Category = "Roupa", DurabilityPct = 85, WeightKg = 2.5f };
        Equipment[0, 1] = new InventoryItem { Id = "pack", Name = "Mochila de Couro", Icon = "🎒", Category = "Contêiner", DurabilityPct = 90, WeightKg = 1.2f };
        Equipment[0, 2] = new InventoryItem { Id = "bow_wood", Name = "Arco Curto de Caça", Icon = "🏹", Category = "Arma", DurabilityPct = 72, DamageStr = "5-12", WeightKg = 1.5f };
        Equipment[1, 0] = new InventoryItem { Id = "axe", Name = "Machado de Lenhador", Icon = "🪓", Category = "Ferramenta", DurabilityPct = 68, DamageStr = "8-16", WeightKg = 2.4f };
        Equipment[1, 1] = new InventoryItem { Id = "knife", Name = "Faca de Caça", Icon = "🗡️", Category = "Ferramenta", DurabilityPct = 95, DamageStr = "4-8", WeightKg = 0.4f };
        Equipment[1, 2] = new InventoryItem { Id = "quiver", Name = "Aljava de Couro", Icon = "🎯", Category = "Contêiner", Count = 12, WeightKg = 1.1f, StackText = "12/20" };
        Equipment[2, 0] = new InventoryItem { Id = "bedroll", Name = "Saco de Dormir", Icon = "🛏️", Category = "Cabana", DurabilityPct = 100, WeightKg = 3.0f };
        Equipment[2, 2] = new InventoryItem { Id = "arrows", Name = "Flechas de Caça", Icon = "🏹", Category = "Munição", Count = 18, WeightKg = 0.6f, StackText = "18/20" };

        // Itens da mochila espacial (starter kit do ranger)
        var meat = new InventoryItem { Id = "meat_elk_raw", Name = "Carne Fresca de Alce", Icon = "🥩", Category = "Comida Crua", BaseWidth = 1, BaseHeight = 2, WeightKg = 1.8f, DurabilityPct = 90 };
        var trout = new InventoryItem { Id = "fish_trout", Name = "Truta Fresca do Lago", Icon = "🐟", Category = "Comida Crua", BaseWidth = 1, BaseHeight = 2, WeightKg = 1.2f, DurabilityPct = 95 };
        var berries = new InventoryItem { Id = "berries_wild", Name = "Frutas Silvestres", Icon = "🫐", Category = "Botânica", Count = 4, BaseWidth = 1, BaseHeight = 1, WeightKg = 0.1f, DurabilityPct = 80 };
        var mushroom = new InventoryItem { Id = "shroom_chantarelle", Name = "Cogumelo Chantarela", Icon = "🍄", Category = "Botânica", Count = 2, BaseWidth = 1, BaseHeight = 1, WeightKg = 0.15f, DurabilityPct = 85 };
        var herb = new InventoryItem { Id = "herb_milefoil", Name = "Erva Milefólio", Icon = "🌿", Category = "Botânica", BaseWidth = 1, BaseHeight = 1, WeightKg = 0.05f, DurabilityPct = 100 };
        var waterFlask = new InventoryItem { Id = "water_flask", Name = "Cantil de Água Doce", Icon = "🍶", Category = "Bebida", BaseWidth = 1, BaseHeight = 1, WeightKg = 0.9f, StackText = "1/2" };
        var grain = new InventoryItem { Id = "grain_rye", Name = "Grãos de Centeio", Icon = "🌾", Category = "Botânica", Count = 3, BaseWidth = 1, BaseHeight = 1, WeightKg = 0.1f, DurabilityPct = 100 };
        var flint = new InventoryItem { Id = "flint_iron", Name = "Pederneira e Metal", Icon = "🔥", Category = "Ferramenta", BaseWidth = 1, BaseHeight = 1, WeightKg = 0.3f, DurabilityPct = 100 };

        // Auto-posiciona via API (empilha quando possível, senão busca espaço livre)
        AddItem(meat); AddItem(trout); AddItem(berries); AddItem(mushroom);
        AddItem(herb); AddItem(waterFlask); AddItem(grain); AddItem(flint);

        SelectedItem = SpatialItems.FirstOrDefault();
    }

    // ──────────────────────────────────────────────────────────────────────
    // API de Inventário Espacial (rotação, colisão, spawn, peso real)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Adiciona um item à mochila espacial: empilha por Id quando aplicável,
    /// senão busca automaticamente o primeiro espaço livre no grid. Retorna false se cheio.</summary>
    public static bool AddItem(InventoryItem item)
    {
        // Empilhar sobre item existente de mesmo Id (somente itens de mesmo tamanho)
        var existing = SpatialItems.FirstOrDefault(i => i.Id == item.Id);
        if (existing != null)
        {
            existing.Count += item.Count;
            if (string.IsNullOrEmpty(existing.StackText) || existing.StackText.StartsWith("x"))
            {
                existing.StackText = $"x{existing.Count}";
            }
            MarkDirty();
            return true;
        }

        if (TryFindFreeSpot(item.Width, item.Height, out int col, out int row))
        {
            item.GridX = col;
            item.GridY = row;
            SpatialItems.Add(item);
            MarkDirty();
            return true;
        }
        return false; // mochila cheia
    }

    /// <summary>Procura o primeiro espaço livre compatível com item W x H no grid.</summary>
    public static bool TryFindFreeSpot(int w, int h, out int col, out int row)
    {
        for (int r = 0; r <= GridRows - h; r++)
        {
            for (int c = 0; c <= GridCols - w; c++)
            {
                var probe = new InventoryItem { BaseWidth = w, BaseHeight = h, GridX = c, GridY = r };
                if (CanPlaceItemProbed(probe, c, r))
                {
                    col = c; row = r;
                    return true;
                }
            }
        }
        col = -1; row = -1;
        return false;
    }

    private static bool CanPlaceItemProbed(InventoryItem probe, int col, int row)
    {
        if (col < 0 || col + probe.Width > GridCols) return false;
        if (row < 0 || row + probe.Height > GridRows) return false;
        foreach (var other in SpatialItems)
        {
            bool overlapX = col < other.GridX + other.Width && col + probe.Width > other.GridX;
            bool overlapY = row < other.GridY + other.Height && row + probe.Height > other.GridY;
            if (overlapX && overlapY) return false;
        }
        return true;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Persistência (state sobrevive a restarts — salva em arquivo ao lado do binário)
    // ──────────────────────────────────────────────────────────────────────

    public static bool NeedSave { get; set; } = false;
    public static void MarkDirty() => NeedSave = true;

    private static string SavePath => Path.Combine(AppContext.BaseDirectory, "save_inventory.txt");

    public static void SaveState()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("# urw_reborn_inventory_v1");
            sb.AppendLine($"STR|{Strength}");
            foreach (var it in SpatialItems)
                sb.AppendLine("S|" + EncodeItem(it));
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    sb.AppendLine($"E|{r}|{c}|" + (Equipment[r, c] == null ? "NONE" : EncodeItem(Equipment[r, c]!)));
            File.WriteAllText(SavePath, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryHUD] Erro ao salvar inventario: {ex.Message}");
        }
        NeedSave = false;
    }

    private static string EncodeItem(InventoryItem it) => string.Join("|",
        it.Id, it.Name, it.Icon, it.Count.ToString(CultureInfo.InvariantCulture), it.Category,
        it.DurabilityPct.ToString(CultureInfo.InvariantCulture), it.DamageStr, it.RangeStr,
        it.BaseWidth.ToString(), it.BaseHeight.ToString(),
        it.IsRotated ? "1" : "0", it.GridX.ToString(), it.GridY.ToString(),
        it.WeightKg.ToString("0.00", CultureInfo.InvariantCulture),
        it.StackText,
        string.Join(";", it.UpgradeComponents.Select(u => $"{u.Name}:{u.Current}/{u.Required}")));

    private static InventoryItem? DecodeItem(string[] f)
    {
        if (f.Length < 15) return null;
        try
        {
            var it = new InventoryItem
            {
                Id = f[0],
                Name = f[1],
                Icon = f[2],
                Count = int.Parse(f[3], CultureInfo.InvariantCulture),
                Category = f[4],
                DurabilityPct = int.Parse(f[5], CultureInfo.InvariantCulture),
                DamageStr = f[6],
                RangeStr = f[7],
                BaseWidth = int.Parse(f[8], CultureInfo.InvariantCulture),
                BaseHeight = int.Parse(f[9], CultureInfo.InvariantCulture),
                IsRotated = f[10] == "1",
                GridX = int.Parse(f[11], CultureInfo.InvariantCulture),
                GridY = int.Parse(f[12], CultureInfo.InvariantCulture),
                WeightKg = float.Parse(f[13], CultureInfo.InvariantCulture),
                StackText = f[14]
            };
            if (f.Length > 15 && f[15].Length > 0)
            {
                foreach (var c in f[15].Split(';'))
                {
                    var seg = c.Split(':');
                    if (seg.Length == 2)
                    {
                        var nums = seg[1].Split('/');
                        if (nums.Length == 2 && int.TryParse(nums[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cur) && int.TryParse(nums[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int req))
                        {
                            it.UpgradeComponents.Add((seg[0], cur, req));
                        }
                    }
                }
            }
            return it;
        }
        catch { return null; }
    }

    public static bool TryLoadState()
    {
        try
        {
            if (!File.Exists(SavePath)) return false;
            var lines = File.ReadAllLines(SavePath);
            bool any = false;
            foreach (var line in lines)
            {
                if (line.Length == 0) continue;
                var f = line.Split('|');
                if (f[0] == "S")
                {
                    var it = DecodeItem(f.Skip(1).ToArray());
                    if (it != null) { SpatialItems.Add(it); any = true; }
                }
                else if (f[0] == "E" && f.Length >= 4)
                {
                    int r = int.Parse(f[1]); int c = int.Parse(f[2]);
                    if (f[3] == "NONE") { Equipment[r, c] = null; }
                    else
                    {
                        var it = DecodeItem(f.Skip(3).ToArray());
                        if (it != null) { Equipment[r, c] = it; any = true; }
                    }
                }
                else if (f[0] == "STR")
                {
                    if (int.TryParse(f[1], out int str)) Strength = str;
                }
            }
            return any;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryHUD] Erro ao carregar inventario: {ex.Message}");
            return false;
        }
    }

    public static void Draw(int screenW, int screenH, Texture2D? playerTex, float hunger, float thirst, float energy, float health)
    {
        if (!IsOpen) return;

        // Fundo escuro semitransparente (overlay)
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(10, 14, 20, 200));

        // Dimensões do Painel Principal
        int hudW = 820;
        int hudH = 690;
        int hudX = (screenW - hudW) / 2;
        int hudY = (screenH - hudH) / 2;

        Color borderBlue  = UiTheme.Border;
        Color containerBg = UiTheme.Card;
        Color slotBg      = UiTheme.CardSunken;
        Color slotBorder  = UiTheme.Border;

        // Janela principal estilo dashboard
        UiTheme.Panel(hudX, hudY, hudW, hudH, 0.045f, UiTheme.Bg, borderBlue);

        // =========================================================================
        // I. HEADER PANEL: "Você"
        // =========================================================================
        int headerH = 38;
        UiTheme.Header(hudX + 24, hudY + 12, "Painel do Sobrevivente", UiTheme.Gold, 13);
        UiTheme.Text("[ TAB / I ] fechar", hudX + hudW - 120, hudY + 15, 11.5f, UiTheme.TextMuted);
        UiTheme.Separator(hudX + 20, hudY + headerH, hudW - 40);

        // =========================================================================
        // II. TOP ROW (Personagem e Barras de Sobrevivência)
        // =========================================================================
        int topRowY = hudY + headerH + 10;
        int charBoxW = 140;
        int charBoxH = 140;

        // Container Esquerdo: Display do Personagem Ranger
        Rectangle charBoxRec = new Rectangle(hudX + 20, topRowY, charBoxW, charBoxH);
        Raylib.DrawRectangleRounded(charBoxRec, 0.08f, 6, containerBg);
        Raylib.DrawRectangleRoundedLines(charBoxRec, 0.08f, 6, borderBlue);

        if (playerTex.HasValue)
        {
            float drawH = charBoxH - 12;
            float drawW = drawH * (playerTex.Value.Width / (float)playerTex.Value.Height);
            float drawX = charBoxRec.X + (charBoxW - drawW) / 2f;
            float drawY = charBoxRec.Y + 6;
            Rectangle src = new Rectangle(0, 0, playerTex.Value.Width, playerTex.Value.Height);
            Rectangle dest = new Rectangle(drawX, drawY, drawW, drawH);
            Raylib.DrawTexturePro(playerTex.Value, src, dest, Vector2.Zero, 0, Color.White);
        }

        // Container Direito: Painel de Sobrevivência (Progress Bars)
        int statsX = hudX + 20 + charBoxW + 14;
        int statsW = hudW - charBoxW - 54;
        Rectangle statsBoxRec = new Rectangle(statsX, topRowY, statsW, charBoxH);
        Raylib.DrawRectangleRounded(statsBoxRec, 0.08f, 6, containerBg);
        Raylib.DrawRectangleRoundedLines(statsBoxRec, 0.08f, 6, borderBlue);

        int barStartY = topRowY + 12;
        int barSpacing = 30;
        int barW = statsW - 130;

        DrawSpecBar(statsX + 16, barStartY, barW, "🍽️", "Fome", hunger, new Color(220, 60, 50, 255));
        DrawSpecBar(statsX + 16, barStartY + barSpacing, barW, "💧", "Sede", thirst, new Color(50, 150, 230, 255));
        DrawSpecBar(statsX + 16, barStartY + barSpacing * 2, barW, "⚡", "Energia", energy, new Color(230, 180, 40, 255));
        DrawSpecBar(statsX + 16, barStartY + barSpacing * 3, barW, "❤️", "Saúde", health, new Color(60, 200, 80, 255));

        // =========================================================================
        // III. MIDDLE ROW (Equipamentos, Atributos e Detalhes do Item)
        // =========================================================================
        int midRowY = topRowY + charBoxH + 10;
        int midRowH = 150;

        // 1. Grid de Equipamentos 3x3 (Esquerda)
        int eqBoxW = 145;
        Rectangle eqBoxRec = new Rectangle(hudX + 20, midRowY, eqBoxW, midRowH);
        Raylib.DrawRectangleRounded(eqBoxRec, 0.08f, 6, containerBg);
        Raylib.DrawRectangleRoundedLines(eqBoxRec, 0.08f, 6, borderBlue);

        int slotSize = 38;
        int eqStartX = (int)eqBoxRec.X + 10;
        int eqStartY = (int)eqBoxRec.Y + 12;
        int eqGap = 5;

        Vector2 mouse = Raylib.GetMousePosition();

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int sx = eqStartX + c * (slotSize + eqGap);
                int sy = eqStartY + r * (slotSize + eqGap);
                Rectangle sRec = new Rectangle(sx, sy, slotSize, slotSize);

                bool isHover = Raylib.CheckCollisionPointRec(mouse, sRec);
                var item = Equipment[r, c];
                bool isSelected = (item != null && item == SelectedItem);

                Color currentSlotBg = isHover ? new Color(38, 52, 78, 255) : slotBg;
                Color currentSlotBorder = isSelected ? Color.Gold : (isHover ? new Color(80, 110, 160, 255) : slotBorder);

                Raylib.DrawRectangleRounded(sRec, 0.15f, 4, currentSlotBg);
                Raylib.DrawRectangleRoundedLines(sRec, 0.15f, 4, currentSlotBorder);

                if (item != null)
                {
                    Raylib.DrawText(item.Icon, sx + 8, sy + 6, 18, Color.White);
                    if (isHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        SelectedItem = item;
                    }
                }
            }
        }

        // 2. Painel de Atributos Oficiais do UnReal World (11 Atributos)
        int attrX = (int)eqBoxRec.X + eqBoxW + 10;
        int attrW = 285;
        Rectangle attrBoxRec = new Rectangle(attrX, midRowY, attrW, midRowH);
        Raylib.DrawRectangleRounded(attrBoxRec, 0.08f, 6, containerBg);
        Raylib.DrawRectangleRoundedLines(attrBoxRec, 0.08f, 6, borderBlue);

        UiTheme.Header(attrX + 12, midRowY + 7, "Atributos (UnReal World)", UiTheme.Gold, 11);
        UiTheme.Separator(attrX + 12, midRowY + 24, attrW - 24);
        Raylib.DrawLine(attrX + 140, midRowY + 25, attrX + 140, midRowY + midRowH - 8, new Color(36, 50, 75, 255));

        int col1X = attrX + 10;
        int col2X = attrX + 148;
        int rowStartY = midRowY + 26;
        int rowH = 19;

        DrawAttributeRow(col1X, rowStartY + (0 * rowH), "💪", "Força", 14);
        DrawAttributeRow(col1X, rowStartY + (1 * rowH), "🏃", "Agilidade", 12);
        DrawAttributeRow(col1X, rowStartY + (2 * rowH), "🎯", "Destreza", 11);
        DrawAttributeRow(col1X, rowStartY + (3 * rowH), "🫀", "Resistência", 13);
        DrawAttributeRow(col1X, rowStartY + (4 * rowH), "⚡", "Velocidade", 10);
        DrawAttributeRow(col1X, rowStartY + (5 * rowH), "📏", "Físico", 178);

        DrawAttributeRow(col2X, rowStartY + (0 * rowH), "👁️", "Visão", 15);
        DrawAttributeRow(col2X, rowStartY + (1 * rowH), "👂", "Audição", 13);
        DrawAttributeRow(col2X, rowStartY + (2 * rowH), "👃", "Olfato", 10);
        DrawAttributeRow(col2X, rowStartY + (3 * rowH), "✋", "Tato", 10);
        DrawAttributeRow(col2X, rowStartY + (4 * rowH), "🧠", "Inteligência", 11);
        DrawAttributeRow(col2X, rowStartY + (5 * rowH), "🧘", "Vontade", 14);

        // 3. Painel de Detalhes do Item Selecionado (Direita)
        int detailX = attrX + attrW + 10;
        int detailW = hudW - (detailX - hudX) - 20;
        Rectangle detailBoxRec = new Rectangle(detailX, midRowY, detailW, midRowH);
        Raylib.DrawRectangleRounded(detailBoxRec, 0.08f, 6, containerBg);
        Raylib.DrawRectangleRoundedLines(detailBoxRec, 0.08f, 6, borderBlue);

        if (SelectedItem != null)
        {
            UiTheme.Text(SelectedItem.Name, detailX + 12, midRowY + 8, 14, UiTheme.Gold, bold: true);
            Raylib.DrawText(SelectedItem.Icon, detailX + 12, midRowY + 28, 28, Color.White);

            int infoX = detailX + 50;
            int infoY = midRowY + 28;
            if (!string.IsNullOrEmpty(SelectedItem.DamageStr))
            {
                UiTheme.Text($"Dano:", infoX, infoY, 11, UiTheme.TextMuted);
                UiTheme.Text($"{SelectedItem.DamageStr}", infoX + 46, infoY, 11, UiTheme.TextPrimary, bold: true);
                infoY += 15;
            }
            UiTheme.Text("Peso:", infoX, infoY, 11, UiTheme.TextMuted);
            UiTheme.Text($"{SelectedItem.WeightKg:0.0} kg", infoX + 46, infoY, 11, UiTheme.TextPrimary, bold: true);
            infoY += 15;
            Color durColor = SelectedItem.DurabilityPct > 70 ? UiTheme.Success : (SelectedItem.DurabilityPct > 30 ? UiTheme.Warn : UiTheme.Danger);
            UiTheme.Text("Condição:", infoX, infoY, 11, UiTheme.TextMuted);
            UiTheme.Text($"{SelectedItem.DurabilityPct}%", infoX + 62, infoY, 11, durColor, bold: true);

            if (SelectedItem.UpgradeComponents.Count > 0)
            {
                int compY = midRowY + 85;
                UiTheme.Label("Componentes de Melhoria", detailX + 12, compY, 9.5f, UiTheme.TextMuted, bold: false, spacing: 1.2f);
                compY += 14;
                foreach (var comp in SelectedItem.UpgradeComponents)
                {
                    bool isComplete = comp.Current >= comp.Required;
                    Color valColor = isComplete ? UiTheme.Success : UiTheme.Danger;
                    UiTheme.Text($"• {comp.Name}:", detailX + 12, compY, 10.5f, UiTheme.TextSecondary);
                    UiTheme.Text($"{comp.Current}/{comp.Required}", detailX + 180, compY, 10.5f, valColor, bold: true);
                    compY += 14;
                }
            }
        }

        // =========================================================================
        // IV. BOTTOM ROW (INVENTÁRIO ESPACIAL EM GRADE - ESTILO TARKOV / TETRIS)
        // =========================================================================
        int botRowY = midRowY + midRowH + 10;
        int botRowH = hudH - (botRowY - hudY) - 14;

        Rectangle botBoxRec = new Rectangle(hudX + 20, botRowY, hudW - 40, botRowH);
        Raylib.DrawRectangleRounded(botBoxRec, 0.08f, 6, containerBg);
        Raylib.DrawRectangleRoundedLines(botBoxRec, 0.08f, 6, borderBlue);

        // Header da Mochila Espacial
        float totalWeight = SpatialItems.Sum(i => i.WeightKg);
        int totalCells = SpatialItems.Sum(i => i.Width * i.Height);
        UiTheme.Header((int)botBoxRec.X + 16, (int)botBoxRec.Y + 8,
            $"Mochila Tática de Couro  ·  {totalCells}/{GridCols * GridRows} células", UiTheme.Gold, 12);

        int gridStartX = (int)botBoxRec.X + 16;
        int gridStartY = (int)botBoxRec.Y + 34;

        // Gerencia Arrastar e Rotação [R]
        HandleSpatialInventoryInput(gridStartX, gridStartY, mouse);

        // ── AUTO-SAVE: persiste o estado espacial após qualquer mudança (grid/equip./consumo)
        if (NeedSave && DraggedItem == null)
        {
            SaveState();
        }

        // 1. Desenha as células de fundo da Grade (Background Grid)
        for (int c = 0; c < GridCols; c++)
        {
            for (int r = 0; r < GridRows; r++)
            {
                int cx = gridStartX + c * CellSize;
                int cy = gridStartY + r * CellSize;
                Raylib.DrawRectangle(cx, cy, CellSize, CellSize, UiTheme.CardSoft);
                Raylib.DrawRectangleLines(cx, cy, CellSize, CellSize, new Color(40, 52, 76, 255));
            }
        }

        // 2. Se um item estiver sendo arrastado, desenha o feedback de colocação (Verde / Vermelho)
        if (DraggedItem != null)
        {
            int targetCol = (int)((mouse.X - gridStartX) / CellSize) - dragGrabOffsetCellX;
            int targetRow = (int)((mouse.Y - gridStartY) / CellSize) - dragGrabOffsetCellY;

            bool canPlace = CanPlaceItem(DraggedItem, targetCol, targetRow);
            Color highlightBg = canPlace ? new Color(40, 180, 70, 100) : new Color(220, 45, 45, 100);
            Color highlightBorder = canPlace ? Color.Green : Color.Red;

            for (int dx = 0; dx < DraggedItem.Width; dx++)
            {
                for (int dy = 0; dy < DraggedItem.Height; dy++)
                {
                    int cellCol = targetCol + dx;
                    int cellRow = targetRow + dy;
                    if (cellCol >= 0 && cellCol < GridCols && cellRow >= 0 && cellRow < GridRows)
                    {
                        int px = gridStartX + cellCol * CellSize;
                        int py = gridStartY + cellRow * CellSize;
                        Raylib.DrawRectangle(px, py, CellSize, CellSize, highlightBg);
                        Raylib.DrawRectangleLines(px, py, CellSize, CellSize, highlightBorder);
                    }
                }
            }
        }

        // 3. Desenha os Itens Alocados na Grade (exceto o que está sendo arrastado)
        foreach (var item in SpatialItems)
        {
            if (item == DraggedItem) continue;

            int ix = gridStartX + item.GridX * CellSize;
            int iy = gridStartY + item.GridY * CellSize;
            int iw = item.Width * CellSize;
            int ih = item.Height * CellSize;

            Rectangle itemRec = new Rectangle(ix, iy, iw, ih);
            bool isHover = Raylib.CheckCollisionPointRec(mouse, itemRec) && DraggedItem == null;
            bool isSel = (item == SelectedItem);

            Color itemBg = isSel ? UiTheme.AccentDim : (isHover ? UiTheme.CardHover : UiTheme.CardSoft);
            Color itemBorder = isSel ? UiTheme.Gold : (isHover ? UiTheme.BorderHover : UiTheme.Border);

            DrawSpatialItemBox(item, ix, iy, iw, ih, itemBg, itemBorder);
        }

        // 4. Desenha o Item Flutuando sob o Cursor se estiver sendo arrastado
        if (DraggedItem != null)
        {
            int dw = DraggedItem.Width * CellSize;
            int dh = DraggedItem.Height * CellSize;
            int dx = (int)mouse.X - (dragGrabOffsetCellX * CellSize) - CellSize / 2;
            int dy = (int)mouse.Y - (dragGrabOffsetCellY * CellSize) - CellSize / 2;

            DrawSpatialItemBox(DraggedItem, dx, dy, dw, dh, new Color(40, 60, 95, 220), Color.Gold);
        }

        // 5. Painel Lateral de Informações de Peso & Instruções
        int sideInfoX = gridStartX + GridCols * CellSize + 18;
        int sideInfoW = hudW - (sideInfoX - hudX) - 20;

        UiTheme.Header(sideInfoX, gridStartY + 3, "Estatísticas da Carga", UiTheme.Gold, 11);

        // Barra de Capacidade de Peso
        bool overweight = totalWeight > MaxWeightKg;
        UiTheme.Text("Peso", sideInfoX, gridStartY + 26, 10.5f, UiTheme.TextMuted);
        UiTheme.Text($"{totalWeight:0.0} / {MaxWeightKg:0.0} kg", sideInfoX + 44, gridStartY + 26, 10.5f, overweight ? UiTheme.Danger : UiTheme.TextPrimary, bold: true);
        UiTheme.StatBar(sideInfoX, gridStartY + 42, sideInfoW - 10, 8, totalWeight / MaxWeightKg, overweight ? UiTheme.Danger : new Color(70, 160, 220, 255));

        // Volume / Ocupação de Células
        UiTheme.Text("Ocupação", sideInfoX, gridStartY + 72, 10.5f, UiTheme.TextMuted);
        UiTheme.Text($"{totalCells} / {GridCols * GridRows} células", sideInfoX + 74, gridStartY + 72, 10.5f, UiTheme.TextPrimary, bold: true);

        UiTheme.Separator(sideInfoX, gridStartY + 100, sideInfoW - 10);

        // DICAS DE CONTROLE ESPACIAL
        UiTheme.Header(sideInfoX, gridStartY + 112, "Controles Espaciais", UiTheme.Gold, 10);
        UiTheme.Text("•", sideInfoX, gridStartY + 132, 10.5f, UiTheme.TextMuted);
        UiTheme.Text("Arrastar", sideInfoX + 14, gridStartY + 132, 10.5f, UiTheme.TextPrimary, bold: true);
        UiTheme.Text("Mover pelo grid", sideInfoX + 78, gridStartY + 132, 10.5f, UiTheme.TextSecondary);
        UiTheme.Text("•", sideInfoX, gridStartY + 150, 10.5f, UiTheme.TextMuted);
        UiTheme.Text("Tecla [R]", sideInfoX + 14, gridStartY + 150, 10.5f, UiTheme.Warn, bold: true);
        UiTheme.Text("Girar ao arrastar", sideInfoX + 78, gridStartY + 150, 10.5f, UiTheme.TextSecondary);
        UiTheme.Text("•", sideInfoX, gridStartY + 168, 10.5f, UiTheme.TextMuted);
        UiTheme.Text("Clique", sideInfoX + 14, gridStartY + 168, 10.5f, UiTheme.Success, bold: true);
        UiTheme.Text("Selecionar / soltar", sideInfoX + 78, gridStartY + 168, 10.5f, UiTheme.TextSecondary);
        UiTheme.Text("•", sideInfoX, gridStartY + 186, 10.5f, UiTheme.TextMuted);
        UiTheme.Text("Formatos:", sideInfoX + 14, gridStartY + 186, 10.5f, UiTheme.TextPrimary, bold: true);
        UiTheme.Text("ocupação real", sideInfoX + 78, gridStartY + 186, 10.5f, UiTheme.TextSecondary);
    }

    private static void DrawSpatialItemBox(InventoryItem item, int x, int y, int w, int h, Color bg, Color border)
    {
        // Caixa com cantos levemente arredondados
        Rectangle rec = new Rectangle(x + 2, y + 2, w - 4, h - 4);
        Raylib.DrawRectangleRounded(rec, 0.10f, 4, bg);
        Raylib.DrawRectangleRoundedLines(rec, 0.10f, 4, border);

        // Ícone grande centralizado
        int iconSize = Math.Min(w, h) / 2 + 4;
        Raylib.DrawText(item.Icon, x + (w - iconSize) / 2, y + (h - iconSize) / 2 - 2, iconSize, Color.White);

        // 1. Canto Superior Direito: Peso em kg (ex: 0.8kg, 2.9kg)
        string weightStr = $"{item.WeightKg:0.0}kg";
        Vector2 wSize = UiTheme.TextSize(weightStr, 10);
        UiTheme.Text(weightStr, x + w - wSize.X - 6, y + 5, 10, UiTheme.TextSecondary);

        // 2. Canto Inferior Direito: Durabilidade % (ex: 21%, 95%)
        string condStr = $"{item.DurabilityPct}%";
        Vector2 cSize = UiTheme.TextSize(condStr, 10);
        Color condColor = item.DurabilityPct > 70 ? UiTheme.Success : (item.DurabilityPct > 30 ? UiTheme.Warn : UiTheme.Danger);
        UiTheme.Text(condStr, x + w - cSize.X - 6, y + h - 15, 10, condColor, bold: true);

        // 3. Canto Inferior Esquerdo: Quantidade empilhada / Carga em Amarelo (ex: 1/75, x4)
        if (!string.IsNullOrEmpty(item.StackText))
        {
            UiTheme.Text(item.StackText, x + 6, y + h - 15, 10, UiTheme.Gold, bold: true);
        }
    }

    private static void HandleSpatialInventoryInput(int gridStartX, int gridStartY, Vector2 mouse)
    {
        // 1. Rotação com tecla R enquanto arrasta o item!
        if (DraggedItem != null && Raylib.IsKeyPressed(KeyboardKey.R))
        {
            DraggedItem.IsRotated = !DraggedItem.IsRotated;
            MarkDirty(); // rotação persistida também muda o estado

            // Inverte o offset de agarre para rotacionar suavemente sob o cursor
            int temp = dragGrabOffsetCellX;
            dragGrabOffsetCellX = dragGrabOffsetCellY;
            dragGrabOffsetCellY = temp;
        }

        // 2. Inicia o arraste ao clicar com o botão esquerdo sobre um item
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && DraggedItem == null)
        {
            foreach (var item in SpatialItems)
            {
                int ix = gridStartX + item.GridX * CellSize;
                int iy = gridStartY + item.GridY * CellSize;
                Rectangle itemRec = new Rectangle(ix, iy, item.Width * CellSize, item.Height * CellSize);

                if (Raylib.CheckCollisionPointRec(mouse, itemRec))
                {
                    DraggedItem = item;
                    SelectedItem = item;
                    dragOriginalX = item.GridX;
                    dragOriginalY = item.GridY;
                    dragOriginalRotated = item.IsRotated;

                    // Calcula célula interna onde o jogador clicou
                    dragGrabOffsetCellX = Math.Clamp((int)((mouse.X - ix) / CellSize), 0, item.Width - 1);
                    dragGrabOffsetCellY = Math.Clamp((int)((mouse.Y - iy) / CellSize), 0, item.Height - 1);
                    break;
                }
            }
        }

        // 3. Ao soltar o botão esquerdo do mouse: solta o item ou retorna
        if (Raylib.IsMouseButtonReleased(MouseButton.Left) && DraggedItem != null)
        {
            int targetCol = (int)((mouse.X - gridStartX) / CellSize) - dragGrabOffsetCellX;
            int targetRow = (int)((mouse.Y - gridStartY) / CellSize) - dragGrabOffsetCellY;

            if (CanPlaceItem(DraggedItem, targetCol, targetRow))
            {
                DraggedItem.GridX = targetCol;
                DraggedItem.GridY = targetRow;
                MarkDirty(); // posição final persistida
            }
            else
            {
                // Volta à posição e orientação originais
                DraggedItem.GridX = dragOriginalX;
                DraggedItem.GridY = dragOriginalY;
                DraggedItem.IsRotated = dragOriginalRotated;
            }

            DraggedItem = null;
        }
    }

    private static bool CanPlaceItem(InventoryItem itemToPlace, int col, int row)
    {
        // 1. Verifica limites da grade
        if (col < 0 || col + itemToPlace.Width > GridCols) return false;
        if (row < 0 || row + itemToPlace.Height > GridRows) return false;

        // 2. Verifica colisão com qualquer outro item na grade
        foreach (var other in SpatialItems)
        {
            if (other == itemToPlace) continue;

            // Checa interseção de retângulos na grade
            bool overlapX = col < other.GridX + other.Width && col + itemToPlace.Width > other.GridX;
            bool overlapY = row < other.GridY + other.Height && row + itemToPlace.Height > other.GridY;

            if (overlapX && overlapY)
            {
                return false; // Colisão detectada!
            }
        }

        return true;
    }

    private static void DrawSpecBar(int x, int y, int barW, string icon, string label, float value, Color barColor)
    {
        Raylib.DrawText(icon, x, y + 2, 14, Color.White);
        UiTheme.Text(label, x + 24, y + 2.5f, 12, UiTheme.TextSecondary, bold: true, spacing: 1.0f);

        int barX = x + 92;
        int barH = 11;
        int barY = y + 4;

        float pct = Math.Clamp(value / 100f, 0f, 1f);
        UiTheme.StatBar(barX, barY, barW, barH, pct, barColor);

        UiTheme.Text($"{(int)value}%", barX + barW + 10, y + 3, 11, UiTheme.TextPrimary, bold: true);
    }

    private static void DrawAttributeRow(int x, int y, string icon, string label, int value)
    {
        Raylib.DrawText(icon, x, y, 12, Color.White);
        UiTheme.Text(label, x + 20, y + 1, 11.5f, UiTheme.TextSecondary);
        UiTheme.Text(value.ToString(), x + 102, y + 1.5f, 11.5f, UiTheme.Gold, bold: true);
    }

    private static void DrawStatusBar(int x, int y, int width, string label, float current, float max, Color fillColor)
    {
        float pct = Math.Clamp(current / max, 0f, 1f);
        UiTheme.StatBar(x, y, width, 8, pct, fillColor);
    }
}
