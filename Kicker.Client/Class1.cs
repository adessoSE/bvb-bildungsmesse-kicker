using Kicker.Domain;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Kicker.Client;

internal class KickerHubClient
{
    private HubConnection? _connection;

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
        if (_connection is null) Connect();
        else _connection.SendAsync("Command", command).Wait();
    }
}

public static class KickerClient
{
    private static Lazy<KickerHubClient> _client = new(KickerHubClient.CreateAndConnect);
    private static KickerHubClient Client => _client.Value;

    public static void MoveLeft()
    {
        Client.SendCommand(GameCommand.NewMove(new Player(Team.BVB, 1), Direction.Left));
        Thread.Sleep(200);
    }

    public static void MoveRight()
    {
        Client.SendCommand(GameCommand.NewMove(new Player(Team.BVB, 1), Direction.Right));
        Thread.Sleep(200);
    }

    public static void MoveDown()
    {
        Client.SendCommand(GameCommand.NewMove(new Player(Team.BVB, 1), Direction.Down));
        Thread.Sleep(200);
    }

    public static void MoveUp()
    {
        Client.SendCommand(GameCommand.NewMove(new Player(Team.BVB, 1), Direction.Up));
        Thread.Sleep(200);
    }
}