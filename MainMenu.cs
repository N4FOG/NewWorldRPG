using System;
using System.Numerics;
using Raylib_cs;

namespace UrwReborn;

public enum MenuScreen
{
    Title,
    MultiplayerSetup,
    Settings
}

public static class MainMenu
{
    public static MenuScreen CurrentScreen { get; set; } = MenuScreen.Title;

    // Campos de texto para Multiplayer
    public static string Nickname { get; set; } = "Kari_O_Ranger";
    public static string ServerHost { get; set; } = "127.0.0.1";
    public static string ServerPort { get; set; } = "7777";
    public static int ActiveInputField { get; set; } = 0; // 0=None, 1=Nick, 2=Host, 3=Port

    public static void Draw(int screenW, int screenH, Action onStartSingleplayer, Action onStartMultiplayer)
    {
        // Fundo atmosférico nórdico (degradê azul-escuro taiga)
        Raylib.ClearBackground(new Color(12, 18, 28, 255));

        // Partículas/neve caindo suavemente ao fundo
        DrawSnowflakes(screenW, screenH);

        Vector2 mouse = Raylib.GetMousePosition();

        if (CurrentScreen == MenuScreen.Title)
        {
            DrawTitleScreen(screenW, screenH, mouse, onStartSingleplayer);
        }
        else if (CurrentScreen == MenuScreen.MultiplayerSetup)
        {
            DrawMultiplayerSetupScreen(screenW, screenH, mouse, onStartMultiplayer);
        }
    }

    private static void DrawTitleScreen(int screenW, int screenH, Vector2 mouse, Action onStartSingleplayer)
    {
        int centerX = screenW / 2;

        // TÍTULO DO JOGO
        string title = "UNREAL WORLD";
        float titleW = UiTheme.TextSize(title, 42, bold: true, spacing: 4f).X;
        UiTheme.Text(title, centerX - titleW / 2, 110, 42, UiTheme.TextPrimary, bold: true, spacing: 4f);

        string sub = "R E B O R N";
        float subW = UiTheme.TextSize(sub, 21, bold: true, spacing: 4f).X;
        UiTheme.Text(sub, centerX - subW / 2, 158, 21, UiTheme.Success, bold: true, spacing: 4f);

        string desc = "Simulador de Sobrevivência na Idade do Ferro Boreal";
        float descW = UiTheme.TextSize(desc, 13).X;
        UiTheme.Text(desc, centerX - descW / 2, 192, 13, UiTheme.TextSecondary);

        UiTheme.Separator(centerX - 180, 220, 360);

        // BOTÕES DO MENU PRINCIPAL
        int btnW = 320;
        int btnH = 48;
        int btnX = centerX - btnW / 2;
        int startY = 250;
        int spacing = 60;

        // 1. Botão Singleplayer
        if (DrawMenuButton(btnX, startY, btnW, btnH, "🌲  Um Jogador (Singleplayer)", mouse, new Color(32, 60, 42, 255), Color.Green))
        {
            NetworkSession.StartSingleplayer();
            onStartSingleplayer();
        }

        // 2. Botão Multiplayer / MMORPG
        if (DrawMenuButton(btnX, startY + spacing, btnW, btnH, "🌐  Multijogador (MMORPG / Rede)", mouse, new Color(28, 48, 76, 255), new Color(100, 175, 255, 255)))
        {
            CurrentScreen = MenuScreen.MultiplayerSetup;
        }

        // 3. Botão Opções
        if (DrawMenuButton(btnX, startY + spacing * 2, btnW, btnH, "⚙️  Configurações & Idioma", mouse, new Color(28, 36, 50, 255), Color.LightGray))
        {
            // Placeholder de opções
        }

        // 4. Botão Sair
        if (DrawMenuButton(btnX, startY + spacing * 3, btnW, btnH, "❌  Sair do Jogo", mouse, new Color(48, 26, 26, 255), Color.Red))
        {
            Environment.Exit(0);
        }

        // Rodapé de versão
        string ver = "Motor C# Puro (.NET 9) • Modo Solo & Arquitetura MMORPG Pronta";
        float verW = UiTheme.TextSize(ver, 11.5f).X;
        UiTheme.Text(ver, centerX - verW / 2, screenH - 40, 11.5f, UiTheme.TextMuted);
    }

    private static void DrawMultiplayerSetupScreen(int screenW, int screenH, Vector2 mouse, Action onStartMultiplayer)
    {
        int centerX = screenW / 2;

        string lobbyTitle = "LOBBY MULTIJOGADOR (MMORPG)";
        float lobW = UiTheme.TextSize(lobbyTitle, 23, bold: true, spacing: 2f).X;
        UiTheme.Text(lobbyTitle, centerX - lobW / 2, 90, 23, UiTheme.Gold, bold: true, spacing: 2f);
        string lobbySub = "Conecte-se a um servidor mundial ou hospede uma partida local com amigos.";
        float sub2W = UiTheme.TextSize(lobbySub, 12).X;
        UiTheme.Text(lobbySub, centerX - sub2W / 2, 122, 12, UiTheme.TextSecondary);

        int boxW = 460;
        int boxH = 370;
        int boxX = centerX - boxW / 2;
        int boxY = 150;

        UiTheme.Panel(boxX, boxY, boxW, boxH, 0.05f, UiTheme.Card, UiTheme.Border);

        // Trata digitação nos campos
        HandleTextInput();

        // 1. Campo Nickname
        UiTheme.Text("Nome do Sobrevivente (Nick)", boxX + 30, boxY + 28, 12.5f, UiTheme.TextSecondary, bold: true);
        DrawInputField(boxX + 30, boxY + 48, boxW - 60, 36, Nickname, 1, mouse);

        // 2. Campo Servidor / IP
        UiTheme.Text("Endereço do Servidor (IP / Host)", boxX + 30, boxY + 98, 12.5f, UiTheme.TextSecondary, bold: true);
        DrawInputField(boxX + 30, boxY + 118, (boxW - 60) - 100, 36, ServerHost, 2, mouse);

        // 3. Campo Porta
        UiTheme.Text("Porta", boxX + boxW - 120, boxY + 98, 12.5f, UiTheme.TextSecondary, bold: true);
        DrawInputField(boxX + boxW - 120, boxY + 118, 90, 36, ServerPort, 3, mouse);

        // Status do Servidor
        UiTheme.Panel(boxX + 30, boxY + 170, boxW - 60, 40, 0.15f, UiTheme.CardSunken, UiTheme.Border, shadow: false);
        Raylib.DrawCircle(boxX + 48, boxY + 190, 5, UiTheme.Success);
        UiTheme.Text("Servidor Oficial Nórdico Online  ·  Latência estimada: 24ms", boxX + 62, boxY + 182, 11.5f, UiTheme.TextPrimary);

        // (label "Status:" integrado acima em texto pequeno)
        UiTheme.Label("Status", boxX + 62, boxY + 170 + 2, 9, UiTheme.TextMuted, bold: true, spacing: 2f);

        // BOTÕES DE AÇÃO
        int btnW = (boxW - 70) / 2;
        int btnY = boxY + 230;

        // Botão Conectar
        if (DrawMenuButton(boxX + 30, btnY, btnW, 44, "▶ Conectar", mouse, new Color(30, 80, 50, 255), Color.Green))
        {
            int port = int.TryParse(ServerPort, out int p) ? p : 7777;
            NetworkSession.ConnectToMultiplayer(Nickname, ServerHost, port);
            onStartMultiplayer();
        }

        // Botão Hospedar Servidor
        if (DrawMenuButton(boxX + 40 + btnW, btnY, btnW, 44, "🏠 Hospedar Local", mouse, new Color(30, 55, 90, 255), new Color(120, 185, 255, 255)))
        {
            int port = int.TryParse(ServerPort, out int p) ? p : 7777;
            NetworkSession.HostMultiplayerServer(Nickname, port);
            onStartMultiplayer();
        }

        // Botão Voltar
        if (DrawMenuButton(boxX + 30, boxY + 295, boxW - 60, 40, "⬅ Voltar ao Menu Principal", mouse, new Color(36, 42, 54, 255), Color.LightGray))
        {
            CurrentScreen = MenuScreen.Title;
        }
    }

    private static void DrawInputField(int x, int y, int w, int h, string text, int fieldId, Vector2 mouse)
    {
        Rectangle rec = new Rectangle(x, y, w, h);
        bool isHover = Raylib.CheckCollisionPointRec(mouse, rec);

        if (isHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            ActiveInputField = fieldId;
        }

        bool isActive = (ActiveInputField == fieldId);
        Color bg = isActive ? new Color(30, 42, 66, 255) : UiTheme.CardSunken;
        Color border = isActive ? UiTheme.Accent : (isHover ? UiTheme.BorderHover : UiTheme.Border);

        Raylib.DrawRectangleRounded(rec, 0.15f, 4, bg);
        Raylib.DrawRectangleRoundedLines(rec, 0.15f, 4, border);

        string display = text + (isActive && (Raylib.GetTime() % 1.0 > 0.5) ? "|" : "");
        UiTheme.Text(display, x + 12, y + 10, 13.5f, isActive ? UiTheme.TextPrimary : UiTheme.TextSecondary, bold: isActive);
    }

    private static void HandleTextInput()
    {
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if ((key >= 32) && (key <= 125))
            {
                char c = (char)key;
                if (ActiveInputField == 1 && Nickname.Length < 16) Nickname += c;
                else if (ActiveInputField == 2 && ServerHost.Length < 30) ServerHost += c;
                else if (ActiveInputField == 3 && ServerPort.Length < 5 && char.IsDigit(c)) ServerPort += c;
            }
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            if (ActiveInputField == 1 && Nickname.Length > 0) Nickname = Nickname.Substring(0, Nickname.Length - 1);
            else if (ActiveInputField == 2 && ServerHost.Length > 0) ServerHost = ServerHost.Substring(0, ServerHost.Length - 1);
            else if (ActiveInputField == 3 && ServerPort.Length > 0) ServerPort = ServerPort.Substring(0, ServerPort.Length - 1);
        }
    }

    private static bool DrawMenuButton(int x, int y, int w, int h, string text, Vector2 mouse, Color baseColor, Color hoverTextColor)
    {
        Rectangle rec = new Rectangle(x, y, w, h);
        bool isHover = Raylib.CheckCollisionPointRec(mouse, rec);

        Color bg = isHover ? new Color((byte)Math.Min(255, baseColor.R + 18), (byte)Math.Min(255, baseColor.G + 18), (byte)Math.Min(255, baseColor.B + 24), (byte)255) : baseColor;
        Color border = isHover ? UiTheme.Accent : UiTheme.Border;
        Color txt = isHover ? hoverTextColor : UiTheme.TextPrimary;

        UiTheme.Panel(rec.X, rec.Y, rec.Width, rec.Height, 0.15f, bg, border, shadow: isHover);

        Vector2 tw = UiTheme.TextSize(text, 14);
        UiTheme.Text(text, x + (w - tw.X) / 2, y + (h - tw.Y) / 2, 14, txt, bold: isHover);

        return isHover && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    private static void DrawSnowflakes(int screenW, int screenH)
    {
        double t = Raylib.GetTime();
        for (int i = 0; i < 35; i++)
        {
            float x = (float)((i * 37 + t * 20) % screenW);
            float y = (float)((i * 53 + t * 35) % screenH);
            Raylib.DrawCircle((int)x, (int)y, 1.2f, new Color(200, 225, 255, 90));
        }
    }
}
