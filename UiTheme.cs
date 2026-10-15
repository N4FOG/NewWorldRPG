using System;
using System.Numerics;
using Raylib_cs;

namespace UrwReborn;

/// <summary>
/// Kit de UI moderno padrão "Dashboard": fontes TTF nativas (Segoe UI / Arial),
/// paleta dark-slate com acento azul, cards com sombra suave, headers com
/// tracking de letras e barras arredondadas.
/// Usar UiTheme.Text / UiTheme.Card / UiTheme.Header / UiTheme.StatBar em
/// todas as interfaces; emojis continuam via Raylib.DrawText (fonte default).
/// </summary>
public static class UiTheme
{
    // ─────────────────────────────────────────────────────────── Fontes ──
    public static Font Main { get; private set; }
    public static Font Bold { get; private set; }
    public static bool UseCustomFont { get; private set; }

    /// <summary>Carrega as fontes nativas do sistema. Chamar UMA vez após InitWindow().</summary>
    public static void Load()
    {
        // Tenta Segoe UI (Windows) e Arial antes de desistir (fallback = fonte default do raylib)
        string[] regularPaths =
        {
            "C:/Windows/Fonts/segoeui.ttf",
            "C:/Windows/Fonts/arial.ttf",
        };
        string[] boldPaths =
        {
            "C:/Windows/Fonts/segoeuib.ttf",
            "C:/Windows/Fonts/arialbd.ttf",
        };

        var codepoints = BuildCodepoints();
        Main = TryLoadFont(regularPaths, codepoints, out bool okMain);
        Bold = TryLoadFont(boldPaths, codepoints, out bool okBold);

        UseCustomFont = okMain && okBold;
        if (UseCustomFont)
        {
            Raylib.SetTextureFilter(Main.Texture, TextureFilter.Bilinear);
            Raylib.SetTextureFilter(Bold.Texture, TextureFilter.Bilinear);
        }
    }

    private static Font TryLoadFont(string[] paths, int[] codepoints, out bool loaded)
    {
        loaded = false;
        foreach (var p in paths)
        {
            if (!System.IO.File.Exists(p)) continue;
            try
            {
                var f = Raylib.LoadFontEx(p, 20, codepoints, codepoints.Length);
                loaded = f.Texture.Id != 0;
                return f;
            }
            catch { /* tenta o próximo */ }
        }
        return default;
    }

    private static int[] BuildCodepoints()
    {
        // ASCII + Latin-1 (acentuação PT-BR) + símbolos comuns usados na UI
        var list = new System.Collections.Generic.List<int>();
        for (int i = 32; i <= 255; i++) list.Add(i);
        foreach (var c in new[] { 0x2022, 0x2026, 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D,
                                  0x2190, 0x2191, 0x2192, 0x2193, 0x2713, 0x2717, 0x25CF, 0x25A0 })
            list.Add(c);
        return list.ToArray();
    }

    // ──────────────────────────────────────────────────────────── Paleta ──
    public static readonly Color Bg            = new(13, 17, 25, 246);
    public static readonly Color Card          = new(23, 29, 41, 255);
    public static readonly Color CardHover     = new(29, 37, 53, 255);
    public static readonly Color CardSoft      = new(18, 23, 33, 255);
    public static readonly Color CardSunken    = new(15, 19, 28, 255);
    public static readonly Color Border        = new(47, 59, 83, 255);
    public static readonly Color BorderHover   = new(78, 108, 148, 255);
    public static readonly Color Accent        = new(88, 166, 245, 255);
    public static readonly Color AccentDim     = new(54, 106, 170, 255);
    public static readonly Color TextPrimary   = new(233, 238, 246, 255);
    public static readonly Color TextSecondary = new(163, 178, 200, 255);
    public static readonly Color TextMuted     = new(112, 127, 153, 255);
    public static readonly Color Success       = new(95, 205, 125, 255);
    public static readonly Color Danger        = new(228, 88, 84, 255);
    public static readonly Color Warn          = new(235, 185, 80, 255);
    public static readonly Color Gold          = new(240, 195, 105, 255);
    public static readonly Color Shadow        = new(0, 0, 0, 80);

    // ─────────────────────────────────────────────────── Texto & métrica ──
    public static void Text(string text, float x, float y, float size, Color color,
                            bool bold = false, float spacing = 1.0f)
    {
        if (UseCustomFont) Raylib.DrawTextEx(bold ? Bold : Main, text, new Vector2(x, y), size, spacing, color);
        else Raylib.DrawText(text, (int)x, (int)y, (int)Math.Max(1, MathF.Round(size)), color);
    }

    public static Vector2 TextSize(string text, float size, bool bold = false, float spacing = 1.0f)
    {
        if (UseCustomFont)
            return Raylib.MeasureTextEx(bold ? Bold : Main, text, size, spacing);
        return new Vector2(Raylib.MeasureText(text, (int)Math.Max(1, MathF.Round(size))), size);
    }

    /// <summary>Texto em MAIÚSCULAS com espaçamento largo (estilo dashboard).</summary>
    public static void Label(string text, float x, float y, float size, Color color, bool bold = true, float spacing = 2.5f)
        => Text(text.ToUpperInvariant(), x, y, size, color, bold, spacing);

    public static Vector2 LabelSize(string text, float size, bool bold = true, float spacing = 2.5f)
        => TextSize(text.ToUpperInvariant(), size, bold, spacing);

    // ───────────────────────────────────────────────────── Cards & painéis ──
    public static void Panel(float x, float y, float w, float h, float radius = 0.06f,
                            Color? bg = null, Color? border = null, bool shadow = true)
    {
        if (shadow)
            Raylib.DrawRectangleRounded(new Rectangle(x + 2, y + 4, w, h), radius, 8, Shadow);
        Raylib.DrawRectangleRounded(new Rectangle(x, y, w, h), radius, 8, bg ?? Card);
        Raylib.DrawRectangleRoundedLines(new Rectangle(x, y, w, h), radius, 8, border ?? Border);
    }

    /// <summary>Header de seção: barra de acento + título em maiúsculas com tracking.</summary>
    public static void Header(float x, float y, string title, Color? titleColor = null, float size = 11.5f)
    {
        Raylib.DrawRectangleRounded(new Rectangle(x, y + 2, 3, 11), 0.5f, 4, Accent);
        Label(title, x + 9, y, size, titleColor ?? Gold);
    }

    public static void Separator(float x, float y, float w)
        => Raylib.DrawLineEx(new Vector2(x, y), new Vector2(x + w, y), 1, Border);

    /// <summary>Botão estilo dashboard. Retorna true se houver clique (assumindo hover já checado). Quando hovered==true, também desenha o realce.</summary>
    public static bool ActionButton(Rectangle rec, string text, bool hovered, bool enabled = true)
    {
        Color bg = !enabled ? CardSoft : hovered ? AccentDim : Card;
        Color edge = !enabled ? Border : hovered ? Accent : Border;
        Panel(rec.X, rec.Y, rec.Width, rec.Height, 0.12f, bg, edge, shadow: enabled);
        Vector2 tsize = TextSize(text, 13, bold: true);
        Color tcol = !enabled ? TextMuted : hovered ? Color.White : TextPrimary;
        Text(text, rec.X + (rec.Width - tsize.X) / 2, rec.Y + (rec.Height - tsize.Y) / 2, 13, tcol, bold: true);
        return hovered && enabled && Raylib.IsMouseButtonReleased(MouseButton.Left);
    }

    // ────────────────────────────────────────────────────────────── Barras ──
    /// <summary>Barra arredondada típica de dashboard (trilha escura + preenchimento).</summary>
    public static void StatBar(float x, float y, float w, float h, float pct, Color fillColor,
                               bool showBorder = true)
    {
        pct = Math.Clamp(pct, 0f, 1f);
        Raylib.DrawRectangleRounded(new Rectangle(x, y, w, h), 0.5f, 6, CardSunken);
        if (w - 4 > 0 && pct > 0)
        {
            float fillW = Math.Max(h, (w - 2) * pct);
            Raylib.DrawRectangleRounded(new Rectangle(x + 1, y + 1, fillW, h - 2), 0.5f, 6, fillColor);
        }
        if (showBorder)
            Raylib.DrawRectangleRoundedLines(new Rectangle(x, y, w, h), 0.5f, 6, Border);
    }
}
