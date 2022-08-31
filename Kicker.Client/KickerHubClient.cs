using System.Collections.Concurrent;
using System.Text;
using Kicker.Domain;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Kicker.Client;

internal class KickerHubClient
{
    private readonly HubConnection _connection;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<CommandResult?>> _tasks = new();
    private readonly object _syncLock = new();

    private Position _ownPosition = new(0,0);
    
    private KickerHubClient()
    {
        var kickerServer = Environment.GetEnvironmentVariable("KICKER_SERVER") ?? "localhost";
        
        _connection = new HubConnectionBuilder()
            .AddNewtonsoftJsonProtocol()
            .WithUrl($"http://{kickerServer}:7014/gamehub")
            .Build();

        _connection.On<Guid, CommandResult?>("CommandHandled", OnCommandHandled);
    }
    
    public Position OwnPosition
    {
        get
        {
            lock (_syncLock)
            {
                return _ownPosition;
            }
        }
        private set
        {
            lock (_syncLock)
            {
                _ownPosition = value;
            }
        }
    }

    private void OnCommandHandled(Guid id, CommandResult? result)
    {
        if (_tasks.TryRemove(id, out var handle))
        {
            handle.TrySetResult(result);
        }
    }

    public static KickerHubClient CreateAndConnect()
    {
        var instance = new KickerHubClient();
        instance._connection.StartAsync().Wait();
        return instance;
    }
    
    public void SendCommand(ClientCommand command)
    {
        var id = Guid.NewGuid();
        var taskSource = new TaskCompletionSource<CommandResult?>();
        _tasks.TryAdd(id, taskSource);
        _connection.SendAsync("Command", id, command).Wait();
        var result = taskSource.Task.Result;
        Handle(result);
    }

    private void Handle(CommandResult? result)
    {
        switch (result)
        {
            case CommandResult.Moved moved:
                var position = moved.Item.OfType<MovedObject.MovedPlayer>().SingleOrDefault()?.Item2;
                if (position != null)
                {
                    OwnPosition = new Position(position.Item1, position.Item2);
                }

                var gedribbelt = moved.Item.OfType<MovedObject.MovedBall>().Any();
                Console.WriteLine($"{(gedribbelt ? "Gedribbelt" : "Bewegt")} nach {position}");
                break;
            case CommandResult.Goal:
                Console.Beep();
                Console.WriteLine();
                Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(
                    "4paI4paI4paI4paI4paI4paI4paI4paI4pWXIOKWiOKWiOKWiOKWiOKWiOKWiOKVlyDilojilojilojilojilojiloji" +
                    "lZcg4paI4paI4pWXDQrilZrilZDilZDilojilojilZTilZDilZDilZ3ilojilojilZTilZDilZDilZDilojilojilZfi" +
                    "lojilojilZTilZDilZDilojilojilZfilojilojilZENCiAgIOKWiOKWiOKVkSAgIOKWiOKWiOKVkSAgIOKWiOKWiOKV" +
                    "keKWiOKWiOKWiOKWiOKWiOKWiOKVlOKVneKWiOKWiOKVkQ0KICAg4paI4paI4pWRICAg4paI4paI4pWRICAg4paI4paI" +
                    "4pWR4paI4paI4pWU4pWQ4pWQ4paI4paI4pWX4pWa4pWQ4pWdDQogICDilojilojilZEgICDilZrilojilojilojiloji" +
                    "lojilojilZTilZ3ilojilojilZEgIOKWiOKWiOKVkeKWiOKWiOKVlw0KICAg4pWa4pWQ4pWdICAgIOKVmuKVkOKVkOKV" +
                    "kOKVkOKVkOKVnSDilZrilZDilZ0gIOKVmuKVkOKVneKVmuKVkOKVnQ0K")));
                Console.WriteLine("Das hast du toll gemacht!!1");
                break;
        }
    }

    public async void SubscribeBall(Action<Position> ballHandler)
    {
        var stream = _connection.StreamAsync<GameNotification>("Subscribe");
        await foreach (var notification in stream)
        {
            if (notification is GameNotification.ResultNotification resultNotification)
            {
                switch (resultNotification.Item.Item2)
                {
                    case CommandResult.Moved moved:
                        var ball = moved.Item.OfType<MovedObject.MovedBall>().SingleOrDefault();
                        if (ball != null)
                        {
                            ballHandler(new Position(ball.Item.Item1, ball.Item.Item2));
                        }
                        break;
                }
            }
        }
    }
}