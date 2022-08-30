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
        var kickerServer = Environment.GetEnvironmentVariable("KICKER_SERVER") ?? "localhost";
        
        _connection = new HubConnectionBuilder()
            .AddNewtonsoftJsonProtocol()
            .WithUrl($"http://{kickerServer}:7014/gamehub")
            .Build();

        _connection.StartAsync().Wait();
    }

    public void SendCommand(ClientCommand command)
    {
        if (_connection is null) Connect();
        else _connection.SendAsync("Command", command).Wait();
    }
}