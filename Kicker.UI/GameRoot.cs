using System;
using System.Linq;
using Godot;
using static Kicker.Domain.Game;

namespace Kicker.UI
{
    [Tool]
    public class GameRoot : Node2D
    {
        private static readonly Load.Factory<GameRoot> Factory = Load.Scene<GameRoot>();

        public static GameRoot Create(GameState state, IObservable<MoveResult> moveObservable) => Factory(g =>
        {
            g._initialState = state;
            g._moveObservable = moveObservable;
        });

        private void OnMove(MoveResult moveResult)
        {
            ProcessResult(moveResult);
        }
        
        private GameRoot(){}

        private UiSettings _settings;
        private IDisposable _subscription;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _subscription?.Dispose();
        }

        public override void _Ready()
        {
            base._Ready();

            _settings = _initialState.Settings.ToUiSettings();

            var background = GetNode<FieldBackground>("FieldBackground");
            background.Init(_settings);

            foreach (var player in _initialState.Players)
            {
                var node = Player.Create(player.Player.Team, player.Player.Number);
                node.Tile = player.Position.ToVector2();
                AddChild(node, true);
            }

            var ball = Ball.Create();
            ball.Tile = _initialState.BallPosition.ToVector2();
            AddChild(ball);
            
            _subscription = _moveObservable.Subscribe(OnMove);
        }

        private readonly (string, Direction)[] _directionMap = 
        {
            ("ui_up", Direction.Up),
            ("ui_down", Direction.Down),
            ("ui_right", Direction.Right),
            ("ui_left", Direction.Left),
        };

        private GameState _initialState;
        private IObservable<MoveResult> _moveObservable;

        private static int GetY(MovedObject movedObject) =>
            movedObject switch
            {
                MovedObject.MovedBall b => b.Item.Item2,
                MovedObject.MovedPlayer p => p.Item2.Item2,
                _ => 0
            };
        
        public override void _Input(InputEvent @event)
        {
            // var direction = _directionMap
            //     .Where(x => @event.IsActionPressed(x.Item1))
            //     .Select(x => (Direction?) x.Item2)
            //     .FirstOrDefault();
            //
            // if (direction != null)
            // {
            //     var result = move(new Domain.Game.Player(Team.Team1, 1), direction.Value, _game);
            //     ProcessResult(result);
            // }
            //
            // if (@event.IsActionPressed("ui_select"))
            // {
            //     var result = kick(new Domain.Game.Player(Team.Team1, 1), _game);
            //     ProcessResult(result);
            // }
        }

        private void ProcessResult(MoveResult result)
        {
            switch (result)
            {
                case MoveResult.Moved moved:
                    var objects = moved.Item;
                    var z = 0;
                    foreach (var movedObject in objects.OrderBy(GetY).ThenBy(x => x.IsMovedPlayer ? 1 : 0))
                    {
                        switch (movedObject)
                        {
                            case MovedObject.MovedBall ball:
                            {
                                var node = GetNode<Ball>("ball");
                                node.Tile = ball.Item.ToVector2();
                                node.ZIndex = z++;
                                break;
                            }
                            case MovedObject.MovedPlayer player:
                            {
                                var node = GetNode<Player>(Player.GetName(player.Item1.Team, player.Item1.Number));
                                node.Tile = new Vector2(player.Item2.Item1, player.Item2.Item2);
                                node.ZIndex = z++;
                                break;
                            }
                        }
                    }

                    break;
            }
        }
    }
}