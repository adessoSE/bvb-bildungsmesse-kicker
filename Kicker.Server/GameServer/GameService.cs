using System.Reactive.Linq;
using System.Reactive.Subjects;
using Kicker.Domain;
using static Kicker.Domain.GameModule;

namespace Kicker.Server.GameServer;

public class GameService
{
    private readonly object _syncLock = new();

    private Game _game;
    private GameState _currentState;
    private readonly Subject<GameNotification> _subject;

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

    public IObservable<GameNotification> Notifications => CreateObservable();

    private async Task<CommandResult> Update(Func<Game, CommandResult> update)
    {
        CommandResult? result;
        
        lock (_syncLock)
        {
            result = update(_game);
            _currentState = getState(_game);
            Notify(GameNotification.NewResultNotification(result));
        }

        return await Task.FromResult(result);
    }

    private void Notify(GameNotification notification)
    {
        _subject.OnNext(notification);
    }

    public Task<CommandResult> Process(GameCommand command)
    {
        return Update(game => processCommand(command, game));
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

    public void Reset(GameSettings settings)
    {
        lock (_syncLock)
        {
            _game = create(settings);
            _currentState = getState(_game);
            Notify(GameNotification.NewState(_currentState));
        }
    }

    private IObservable<GameNotification> CreateObservable()
    {
        return Observable.Create<GameNotification>(subscriber =>
        {
            lock (_syncLock)
            {
                var current = GameNotification.NewState(_currentState);
                var subscription = _subject.StartWith(current).Subscribe(subscriber);
                return subscription;
            }
        });
    }
}
