using System.Reactive.Subjects;
using System.Threading.Channels;
using Kicker.Domain;

using static Kicker.Domain.GameModule;

namespace Kicker.Server.GameServer;

public class GameService
{
    private readonly object _syncLock = new();

    private Game _game;
    private GameState _currentState;
    private Subject<GameNotification> _subject;
    private IObservable<GameNotification> _observable;

    public GameService()
    {
        _game = create(GameSettings.defaultSettings);
        _currentState = getState(_game);

        _subject = new Subject<GameNotification>();
    }

    public GameState CurrentState
    {
        get
        {
            lock (_syncLock)
            {
                return _currentState;
            }
        }
    }
    
    private async Task<CommandResult> Update(Func<Game, CommandResult> update)
    {
        CommandResult? result;
        
        lock (_syncLock)
        {
            result = update(_game);
            _currentState = getState(_game);
            Notify(GameNotification.NewMoveNotification(result));
        }

        return result;
    }

    private void Notify(GameNotification notification)
    {
        _subject.OnNext(notification);
    }

    public async Task Process(GameCommand command)
    {
        await Update(game => processCommand(command, game));
    }

    public void Reset()
    {
        lock (_syncLock)
        {
            _game = create(_currentState.Settings);
            _currentState = getState(_game);
            Notify(GameNotification.NewState(_currentState));
        }
    }

    public void Subscribe(ChannelWriter<GameNotification> writer, CancellationToken cancellationToken)
    {
        lock (_syncLock)
        {
             writer.TryWrite(GameNotification.NewState(_currentState));
             IDisposable? subscription = null;
             subscription = _subject.Subscribe(notification =>
             {
                 if (!writer.TryWrite(notification))
                 {
                     subscription?.Dispose();
                 }
             });
             
             var registration = new CancellationTokenRegistration();
             registration = cancellationToken.Register(() =>
             {
                 subscription.Dispose();
                 registration.Dispose();
             });
        }
    }
}
