using System.Diagnostics.CodeAnalysis;
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
    
    public async Task Command(GameCommand command)
    {
        await _gameService.Process(command);
    }
    
    public ChannelReader<GameNotification> Subscribe(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<GameNotification>();
        var writer = channel.Writer;
        
        IDisposable? subscription = null;
        CancellationTokenRegistration registration = default;

        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        void CancelSubscription()
        {
            subscription?.Dispose();
            registration.Dispose();
        }
        
        subscription = _gameService.Notifications.Subscribe(notification =>
        {
            if (!writer.TryWrite(notification))
            {
                CancelSubscription();
            }
        });
        
        registration = cancellationToken.Register(CancelSubscription);
        
        return channel.Reader;
    }
}
