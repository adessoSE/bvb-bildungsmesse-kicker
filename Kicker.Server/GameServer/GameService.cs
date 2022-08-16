using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Kicker.Domain;

namespace Kicker.Server.GameServer;

public class GameService
{
    private readonly object _syncLock = new();

    private Game.Game _game;
    private Game.GameState _currentState;
    private Subject<Game.GameNotification> _subject;

    public GameService()
    {
        _game = Game.create(GameSettings.defaultSettings);
        _currentState = Game.getState(_game);

        _subject = new Subject<Game.GameNotification>();
    }

    public Game.GameState CurrentState
    {
        get
        {
            lock (_syncLock)
            {
                return _currentState;
            }
        }
    }
    
    private async Task<Game.MoveResult> Update(Func<Game.Game, Game.MoveResult> update)
    {
        Game.MoveResult? result;
        
        lock (_syncLock)
        {
            result = update(_game);
            _currentState = Game.getState(_game);
            Notify(Game.GameNotification.NewMoveNotification(result));
        }

        return await Task.FromResult(result);
    }

    private void Notify(Game.GameNotification notification)
    {
        _subject.OnNext(notification);
    }

    public async Task Process(Game.GameCommand command)
    {
        await Update(game => Game.processCommand(command).Invoke(game));
    }

    public void Reset()
    {
        lock (_syncLock)
        {
            _game = Game.create(_currentState.Settings);
            _currentState = Game.getState(_game);
            Notify(Game.GameNotification.NewState(_currentState));
        }
    }

    public void Reset(GameSettings settings)
    {
        lock (_syncLock)
        {
            _game = Game.create(settings);
            _currentState = Game.getState(_game);
            Notify(Game.GameNotification.NewState(_currentState));
        }
    }

    public void Subscribe(ChannelWriter<Game.GameNotification> writer, CancellationToken cancellationToken)
    {
        lock (_syncLock)
        {
             writer.TryWrite(Game.GameNotification.NewState(_currentState));
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
