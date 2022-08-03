﻿using Kicker.Domain;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using static Kicker.Domain.Game;

namespace Kicker.Client;

internal class KickerHubClient
{
    private HubConnection _connection;

    public static KickerHubClient CreateAndConnect()
    {
        var instance = new KickerHubClient();
        instance.Connect();
        return instance;
    }
    
    private void Connect()
    {
        _connection = new HubConnectionBuilder()
            .AddNewtonsoftJsonProtocol()
            .WithUrl("http://localhost:7014/gamehub")
            .Build();

        _connection.StartAsync().Wait();
    }

    public void SendCommand(GameCommand command)
    {
        _connection.SendAsync("Command", command).Wait();
    }
}

public static class KickerClient
{
    private static Lazy<KickerHubClient> _client = new(KickerHubClient.CreateAndConnect);
    private static KickerHubClient Client => _client.Value;

    public static void MoveLeft()
    {
        Client.SendCommand(GameCommand.NewMove(new Player(Team.Team1, 1), Direction.Left));
        Thread.Sleep(200);
    }

    public static void MoveRight()
    {
        Client.SendCommand(GameCommand.NewMove(new Player(Team.Team1, 1), Direction.Right));
        Thread.Sleep(200);
    }
}