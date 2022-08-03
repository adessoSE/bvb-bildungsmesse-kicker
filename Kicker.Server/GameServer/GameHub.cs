using System.Threading.Channels;
using Kicker.Domain;
using Microsoft.AspNetCore.SignalR;

namespace Kicker.Server.GameServer;

public class GameHub : Hub
{
    private readonly GameService _gameService;

    public GameHub(GameService gameService)
    {
        _gameService = gameService;
    }
    
    public async Task Command(Game.GameCommand command)
    {
        await _gameService.Process(command);
    }
    
    public ChannelReader<Game.GameNotification> Subscribe(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<Game.GameNotification>();
        var writer = channel.Writer;
        
        _gameService.Subscribe(writer, cancellationToken);
        return channel.Reader;
    }
}
