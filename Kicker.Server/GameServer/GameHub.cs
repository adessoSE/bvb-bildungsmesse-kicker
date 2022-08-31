using System.Diagnostics.CodeAnalysis;
using System.Net;
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
    
    public async Task Command(Guid id, ClientCommand command)
    {
        await _gameService.WaitForPauseAsync();
        
        var key = Context.GetHttpContext()?.Connection.RemoteIpAddress ?? IPAddress.None;
        var (result, state) = _gameService.Process(key.ToString(), command);
        await Clients.Caller.SendAsync("CommandHandled", id, result);

        if (state.Status.IsRunning)
        {
            await Task.Delay(_gameService.WaitDuration);
        }
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
