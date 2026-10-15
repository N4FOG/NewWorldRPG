using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace UrwReborn;

public static class CookingHUD
{
    public static bool IsOpen { get; set; } = false;

    private static int selectedCategoryIdx = 0;
    private static int selectedRecipeIdx = 0;
    private static CookingRecipe? selectedRecipe = null;

    public static void Open()
    {
        IsOpen = true;
        InventoryHUD.IsOpen = false; // Fecha inventário se aberto
        if (CookingSystem.Recipes.Count > 0 && selectedRecipe == null)
        {
            selectedRecipe = CookingSystem.Recipes[0];
        }
    }

    public static void Draw(int screenW, int screenH, Action<int> onMinutesElapsed, Action<string, Color> onLogMessage)
    {
        if (!IsOpen) return;

        // Fundo escuro translúcido
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(10, 14, 20, 215));

        // Dimensões do Painel Principal
        int hudW = 920;
        int hudH = 620;
        int hudX = (screenW - hudW) / 2;
        int hudY = (screenH - hudH) / 2;

        // Painel principal estilo dashboard
        UiTheme.Panel(hudX, hudY, hudW, hudH, 0.035f, UiTheme.Bg, UiTheme.Border);

        // Cabeçalho
        UiTheme.Header(hudX + 20, hudY + 14, "Estação de Culinária & Processamento Nórdico", UiTheme.Gold, 14);
        UiTheme.Text("[ ESC / C ] fechar", hudX + hudW - 130, hudY + 16, 12, UiTheme.TextMuted);

        Vector2 mouse = Raylib.GetMousePosition();
        bool mouseClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

        // --- 1. ABAS DE CATEGORIA NO TOPO ---
        int catY = hudY + 52;
        int catX = hudX + 16;
        for (int i = 0; i < CookingSystem.Categories.Count; i++)
        {
            string catName = CookingSystem.Categories[i];
            float textW = UiTheme.TextSize(catName, 13, bold: false).X;
            int btnW = (int)textW + 18;
            Rectangle catRect = new Rectangle(catX, catY, btnW, 26);

            bool isSelected = (i == selectedCategoryIdx);
            bool isHovered = Raylib.CheckCollisionPointRec(mouse, catRect);

            Color bgColor = isSelected ? UiTheme.AccentDim : (isHovered ? UiTheme.CardHover : UiTheme.CardSoft);
            Color textColor = isSelected ? Color.White : (isHovered ? UiTheme.Gold : UiTheme.TextSecondary);

            Raylib.DrawRectangleRounded(catRect, 0.25f, 6, bgColor);
            Raylib.DrawRectangleRoundedLines(catRect, 0.25f, 6, isSelected ? UiTheme.Accent : UiTheme.Border);
            UiTheme.Text(catName, catX + 10, catY + 5.5f, 13, textColor, bold: isSelected);

            if (isHovered && mouseClicked)
            {
                selectedCategoryIdx = i;
                selectedRecipeIdx = 0;
            }

            catX += btnW + 8;
        }

        // Filtra receitas da categoria
        string currentCat = CookingSystem.Categories[selectedCategoryIdx];
        List<CookingRecipe> filtered = (currentCat == "Todas as Receitas")
            ? CookingSystem.Recipes
            : CookingSystem.Recipes.FindAll(r => r.Category == currentCat);

        if (filtered.Count > 0 && (selectedRecipe == null || !filtered.Contains(selectedRecipe)))
        {
            selectedRecipe = filtered[0];
        }

        // --- 2. COLUNA ESQUERDA: LISTA DE RECEITAS (380px) ---
        int listX = hudX + 16;
        int listY = hudY + 86;
        int listW = 380;
        int listH = hudH - 102;

        UiTheme.Panel(listX, listY, listW, listH, 0.06f, UiTheme.CardSunken, UiTheme.Border, shadow: false);

        int itemH = 46;
        for (int i = 0; i < filtered.Count; i++)
        {
            var rec = filtered[i];
            int recY = listY + 6 + (i * (itemH + 4));
            if (recY + itemH > listY + listH) break; // Limite de altura

            Rectangle itemRect = new Rectangle(listX + 6, recY, listW - 12, itemH);
            bool isRecSelected = (rec == selectedRecipe);
            bool isRecHovered = Raylib.CheckCollisionPointRec(mouse, itemRect);
            bool canCraft = CookingSystem.CanCraft(rec);

            Color recBg = isRecSelected ? UiTheme.AccentDim : (isRecHovered ? UiTheme.CardHover : UiTheme.Card);
            Raylib.DrawRectangleRounded(itemRect, 0.08f, 6, recBg);
            Raylib.DrawRectangleRoundedLines(itemRect, 0.08f, 6, isRecSelected ? UiTheme.Accent : (canCraft ? UiTheme.Success : UiTheme.Border));

            // Indicador de disponibilidade (Bolinha verde ou vermelha)
            Raylib.DrawCircle(listX + 20, recY + itemH / 2, 5, canCraft ? UiTheme.Success : UiTheme.Danger);

            // Nome e Subcategoria da Receita
            UiTheme.Text(rec.Name, listX + 34, recY + 7, 14, isRecSelected ? UiTheme.Gold : UiTheme.TextPrimary, bold: true);
            string subInfo = $"{rec.Cookware} | {rec.HeatRequired} | {rec.PrepMinutes + rec.CookMinutes} min";
            UiTheme.Text(subInfo, listX + 34, recY + 26, 11, UiTheme.TextMuted);

            if (isRecHovered && mouseClicked)
            {
                selectedRecipe = rec;
            }
        }

        // --- 3. COLUNA DIREITA: DETALHES, INGREDIENTES E COZIMENTO (490px) ---
        int detX = listX + listW + 16;
        int detY = listY;
        int detW = hudW - listW - 48;
        int detH = listH;

        UiTheme.Panel(detX, detY, detW, detH, 0.06f, UiTheme.CardSunken, UiTheme.Border, shadow: false);

        if (selectedRecipe != null)
        {
            var rec = selectedRecipe;
            bool canCraftNow = CookingSystem.CanCraft(rec);

            // Nome e Categoria
            UiTheme.Text(rec.Name, detX + 16, detY + 12, 17, UiTheme.Gold, bold: true);
            UiTheme.Label(rec.Category, detX + 16, detY + 36, 11, UiTheme.TextMuted, bold: true, spacing: 2f);

            // Descrição rica / lore nórdico
            DrawWrappedText(rec.Description, detX + 16, detY + 54, detW - 32, 12, UiTheme.TextSecondary);

            // Barra de Requisitos de Preparo
            int reqBoxY = detY + 115;
            UiTheme.Panel(detX + 16, reqBoxY, detW - 32, 38, 0.12f, UiTheme.Card, UiTheme.Border, shadow: false);

            string reqUtensil = $"Utensílio: {rec.Cookware}";
            string reqHeat = $"Calor: {rec.HeatRequired}";
            string reqTime = $"Tempo: {rec.PrepMinutes + rec.CookMinutes} min ({rec.PrepMinutes}p + {rec.CookMinutes}c)";

            UiTheme.Text(reqUtensil, detX + 24, reqBoxY + 12, 12, UiTheme.TextPrimary);
            UiTheme.Text(reqHeat, detX + 180, reqBoxY + 12, 12, UiTheme.TextPrimary);
            UiTheme.Text(reqTime, detX + 320, reqBoxY + 12, 12, UiTheme.Gold, bold: true);

            // --- SEÇÃO DE INGREDIENTES NECESSÁRIOS ---
            int ingHeaderY = reqBoxY + 48;
            UiTheme.Header(detX + 16, ingHeaderY - 2, "Ingredientes Necessários", UiTheme.Gold, 11);

            int ingRowY = ingHeaderY + 20;
            for (int i = 0; i < rec.Ingredients.Count; i++)
            {
                var ing = rec.Ingredients[i];
                int currentCount = CookingSystem.GetPlayerItemCount(ing);
                bool hasEnough = currentCount >= ing.Qty;

                Rectangle ingRect = new Rectangle(detX + 16, ingRowY, detW - 32, 28);
                Raylib.DrawRectangleRounded(ingRect, 0.12f, 6, hasEnough ? new Color(24, 46, 32, 255) : new Color(46, 26, 28, 255));
                Raylib.DrawRectangleRoundedLines(ingRect, 0.12f, 6, hasEnough ? UiTheme.Success : UiTheme.Danger);

                string consumedBadge = ing.Consumed ? "Consome" : "Ferramenta";
                Color badgeColor = ing.Consumed ? new Color(200, 140, 80, 255) : new Color(100, 160, 220, 255);

                UiTheme.Text(ing.Name, detX + 26, ingRowY + 7, 13, hasEnough ? UiTheme.TextPrimary : UiTheme.Danger);
                UiTheme.Text(consumedBadge, detX + 270, ingRowY + 8, 10, badgeColor, bold: true);

                string countStr = $"{currentCount} / {ing.Qty}";
                Color countColor = hasEnough ? UiTheme.Success : UiTheme.Danger;
                UiTheme.Text(countStr, detX + detW - 80, ingRowY + 7, 13, countColor, bold: true);

                ingRowY += 32;
            }

            // --- PRODUTOS E EFEITOS DE SOBREVIVÊNCIA ---
            int effY = ingRowY + 10;
            UiTheme.Header(detX + 16, effY, "Efeitos de Sobrevivência & Calor", UiTheme.Gold, 11);

            var eff = rec.SurvivalEffects;
            int statsY = effY + 22;
            UiTheme.Panel(detX + 16, statsY, detW - 32, 54, 0.12f, UiTheme.Card, UiTheme.Border, shadow: false);

            UiTheme.Text($"🔥 Calor: +{eff.WarmthBonus:F0}%", detX + 26, statsY + 10, 12, new Color(250, 140, 50, 255), bold: true);
            UiTheme.Text($"🍖 Nutrição: +{eff.Nutrition:F0}", detX + 150, statsY + 10, 12, new Color(240, 90, 90, 255), bold: true);
            UiTheme.Text($"💧 Hidratação: +{eff.Thirst:F0}", detX + 270, statsY + 10, 12, new Color(80, 170, 250, 255), bold: true);
            UiTheme.Text($"⚡ Energia: +{eff.Energy:F0}", detX + 390, statsY + 10, 12, new Color(240, 220, 60, 255), bold: true);

            if (!string.IsNullOrEmpty(eff.SpecialBuff))
            {
                UiTheme.Text($"✨ {eff.SpecialBuff}", detX + 26, statsY + 32, 11, UiTheme.Gold, bold: true);
            }
            else
            {
                UiTheme.Text("Alimento fresco purificado contra infecções da taiga.", detX + 26, statsY + 32, 11, UiTheme.TextMuted);
            }

            // --- BOTÃO DE PREPARO / COZINHAMENTO ---
            int btnY = detY + detH - 64;
            Rectangle craftBtnRect = new Rectangle(detX + 16, btnY, detW - 32, 48);
            bool isBtnHovered = Raylib.CheckCollisionPointRec(mouse, craftBtnRect);

            Color btnBg = canCraftNow
                ? (isBtnHovered ? new Color(190, 142, 48, 255) : new Color(150, 108, 32, 255))
                : UiTheme.CardSoft;
            Color btnTextColor = canCraftNow ? Color.RayWhite : UiTheme.TextMuted;

            UiTheme.Panel(craftBtnRect.X, craftBtnRect.Y, craftBtnRect.Width, craftBtnRect.Height, 0.12f,
                btnBg, canCraftNow ? UiTheme.Gold : UiTheme.Border, shadow: canCraftNow && isBtnHovered);

            if (canCraftNow)
            {
                Raylib.DrawRectangleRoundedLines(craftBtnRect, 0.12f, 6, Color.Gold);
            }

            string btnText = canCraftNow ? "🔥 COZINHAR / PROCESSAR AGORA" : "FALTAM INGREDIENTES OU UTENSÍLIOS";
            Vector2 btw = UiTheme.TextSize(btnText, 15, bold: true, spacing: 1.2f);
            UiTheme.Text(btnText, craftBtnRect.X + (craftBtnRect.Width - btw.X) / 2, btnY + 15, 15, btnTextColor, bold: true, spacing: 1.2f);

            // Clique no botão de preparo
            if (isBtnHovered && mouseClicked && canCraftNow)
            {
                if (CookingSystem.TryCook(rec, out string msg, out int elapsedMins))
                {
                    onMinutesElapsed(elapsedMins);
                    onLogMessage(msg, Color.Gold);
                }
                else
                {
                    onLogMessage(msg, Color.Red);
                }
            }
        }
    }

    private static void DrawWrappedText(string text, int x, int y, int maxWidth, int fontSize, Color color)
    {
        string[] words = text.Split(' ');
        string currentLine = "";
        int currentY = y;

        foreach (var word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            Vector2 measured = UiTheme.TextSize(testLine, fontSize);

            if (measured.X > maxWidth)
            {
                UiTheme.Text(currentLine, x, currentY, fontSize, color);
                currentLine = word;
                currentY += fontSize + 5;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            UiTheme.Text(currentLine, x, currentY, fontSize, color);
        }
    }
}
