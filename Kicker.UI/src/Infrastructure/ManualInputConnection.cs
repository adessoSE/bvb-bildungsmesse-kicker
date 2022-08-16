using System;
using System.Reactive.Subjects;
using Godot;
using Kicker.Domain;

namespace Kicker.UI.Infrastructure
{
    public class ManualInputConnection : Node, IObservable<IConnectionEvent>
    {
        private Game _game;
        private readonly Subject<IConnectionEvent> _events = new();
        private readonly Domain.Player _playerTeam1 = new(Team.Team1, 1);
        private readonly Domain.Player _playerTeam2 = new(Team.Team2, 1);

        public override void _UnhandledInput(InputEvent @event)
        {
            var key = (@event as InputEventKey)?.Shift ?? false;
            var player = key ? _playerTeam2 : _playerTeam1;
            
            if (@event.IsActionPressed("ui_up"))
            {
                Handle(GameCommand.NewMove(player, Direction.Up));
            }
            if (@event.IsActionPressed("ui_down"))
            {
                Handle(GameCommand.NewMove(player, Direction.Down));
            }
            if (@event.IsActionPressed("ui_left"))
            {
                Handle(GameCommand.NewMove(player, Direction.Left));
            }
            if (@event.IsActionPressed("ui_right"))
            {
                Handle(GameCommand.NewMove(player, Direction.Right));
            }
            if (@event.IsActionPressed("ui_select"))
            {
                Handle(GameCommand.NewKick(player));
            }
        }

        private void Handle(GameCommand gameCommand)
        {
            var result = GameModule.processCommand(gameCommand, _game);
            _events.OnNext(new ConnectionEvent.Notification(GameNotification.NewMoveNotification(result)));
        }

        private void Reset()
        {
            _events.OnNext(new ConnectionEvent.Connected());
            
            var settings = GameSettings.defaultSettings;
            _game = GameModule.create(settings);
            var state = GameModule.getState(_game);
            
            _events.OnNext(new ConnectionEvent.Notification(GameNotification.NewState(state)));
        }

        public IDisposable Subscribe(IObserver<IConnectionEvent> observer)
        {
            var subscription = _events.Subscribe(observer);
            Reset();
            return subscription;
        }
    }
}