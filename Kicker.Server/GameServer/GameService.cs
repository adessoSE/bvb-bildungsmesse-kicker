using System.Reactive.Linq;
using System.Reactive.Subjects;
using Kicker.Domain;
using Microsoft.Extensions.Options;
using static Kicker.Domain.GameModule;

namespace Kicker.Server.GameServer;

public class GameService
{
    private readonly IOptionsMonitor<GameConfiguration> _configuration;
    private readonly ILogger<GameService> _logger;
    private readonly object _syncLock = new();

    private Game _game;
    private GameState _currentState;
    private readonly Subject<GameNotification> _subject;
    private TimeSpan _waitDuration = TimeSpan.FromSeconds(1);
    private TaskCompletionSource? _pauseTask;
    
    public GameService(IOptionsMonitor<GameConfiguration> configuration, ILogger<GameService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        var mappings = configuration.CurrentValue.Players.ToDictionary();
        var settings = GameSettings.defaultSettings.withPlayerMapping(mappings);
        _game = create(settings);
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

    public TimeSpan WaitDuration
    {
        get
        {
            lock (_syncLock)
            {
                return _waitDuration;
            }
        }
        set
        {
            lock (_syncLock)
            {
                _waitDuration = value;
            }
        }
    }

    public Task WaitForPauseAsync()
    {
        lock (_syncLock)
        {
            return _pauseTask?.Task ?? Task.CompletedTask;
        }
    }

    public IObservable<GameNotification> Notifications => CreateObservable();

    private (CommandResult Result, GameState State) Update(
        string logDescription, 
        Func<Game, (GameCommand Command, CommandResult Result)> update)
    {
        lock (_syncLock)
        {
            var resultTuple = update(_game);
            var result = resultTuple.Result;

            if (result.IsPaused)
            {
                _pauseTask = new TaskCompletionSource();
            }
            else if (result.IsResumed)
            {
                _pauseTask?.TrySetResult();
            }
            
            _logger.LogDebug("Update: {LogDescription} => {Result}", logDescription, resultTuple);

            _currentState = getState(_game);
            
            if (ShouldSend(result))
            {
                Notify(GameNotification.NewResultNotification(resultTuple.ToTuple()));
            }

            return (result, _currentState);
        }
    }

    private static bool ShouldSend(CommandResult result)
    {
        return result switch
        {
            {IsIgnored: true} => false,
            {IsPlayerNotFound: true} => false,
            _ => true
        };
    }

    private void Notify(GameNotification notification)
    {
        _subject.OnNext(notification);
    }

    public CommandResult Process(GameCommand command)
    {
        return Update(command.ToString(), game => (command, processCommand(command, game))).Result;
    }
    
    public (CommandResult Result, GameState State) Process(string key, ClientCommand command)
    {
        return Update($"{key}/{command}", game => processClientCommand(key, command, game).ToValueTuple());
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
        var mappings = _configuration.CurrentValue.Players.ToDictionary();
        settings = settings.withPlayerMapping(mappings);
        
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
