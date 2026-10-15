using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UrwReborn;

public enum NetworkMode
{
    Singleplayer,
    MultiplayerClient,
    HostServer
}

public class RemotePlayer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "Sobrevivente";
    public int WorldX { get; set; } = 32;
    public int WorldY { get; set; } = 24;
    public int LocalX { get; set; } = 8;
    public int LocalY { get; set; } = 7;
    public bool IsInOverworld { get; set; } = false;
    public float Warmth { get; set; } = 100f;
    public float Health { get; set; } = 100f;
}

public static class NetworkSession
{
    public static NetworkMode CurrentMode { get; set; } = NetworkMode.Singleplayer;
    public static string LocalPlayerName { get; set; } = "Lenhador_Kari";
    public static string ServerAddress { get; set; } = "127.0.0.1";
    public static int ServerPort { get; set; } = 7777;
    public static bool IsConnected { get; set; } = false;
    public static string ConnectionStatus { get; set; } = "Offline";

    // Lista de outros jogadores conectados no servidor
    public static Dictionary<string, RemotePlayer> OtherPlayers { get; } = new();

    static NetworkSession()
    {
        // Cria um bot/sobrevivente remoto de exemplo para demonstrar a sincronização na Taiga
        var samplePlayer = new RemotePlayer
        {
            Id = "player_ilmar",
            Name = "Ilmari_Caçador",
            WorldX = 32,
            WorldY = 24,
            LocalX = 11,
            LocalY = 9,
            IsInOverworld = false,
            Warmth = 88f,
            Health = 100f
        };
        OtherPlayers[samplePlayer.Id] = samplePlayer;
    }

    public static void StartSingleplayer()
    {
        CurrentMode = NetworkMode.Singleplayer;
        IsConnected = true;
        ConnectionStatus = "Sessão Local (Modo Solo)";
    }

    public static void ConnectToMultiplayer(string name, string host, int port)
    {
        LocalPlayerName = string.IsNullOrWhiteSpace(name) ? "Sobrevivente" : name;
        ServerAddress = host;
        ServerPort = port;
        CurrentMode = NetworkMode.MultiplayerClient;
        IsConnected = true;
        ConnectionStatus = $"Conectado a {ServerAddress}:{ServerPort} (MMORPG Ativo)";
    }

    public static void HostMultiplayerServer(string name, int port)
    {
        LocalPlayerName = string.IsNullOrWhiteSpace(name) ? "Anfitrião" : name;
        ServerPort = port;
        CurrentMode = NetworkMode.HostServer;
        IsConnected = true;
        ConnectionStatus = $"Hospedando Servidor na porta {ServerPort} (Pronto para conexões)";
    }
}
