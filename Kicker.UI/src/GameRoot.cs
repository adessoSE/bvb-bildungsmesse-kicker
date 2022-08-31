using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Kicker.Domain;

namespace Kicker.UI
{
	[Tool]
	public class GameRoot : Node2D
	{
		private static readonly Load.Factory<GameRoot> Factory = Load.Scene<GameRoot>();

		public static GameRoot Create(GameState state, IObservable<(GameCommand, CommandResult)> moveObservable) => 
			Factory(g =>
			{
				g._initialState = state;
				g._commandResultObservable = moveObservable;
			});

		private GameRoot()
		{
		}

		private UiSettings _settings;
		private IDisposable _subscription;
		private Spielstand _spielstand;

		public AudioPlayer AudioPlayer => GetNode<AudioPlayer>("AudioPlayer");

		private Root _root => GetParent<Viewport>().GetParent<ViewportContainer>().GetParent<Root>();

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_subscription?.Dispose();
		}

		public override void _Ready()
		{
			base._Ready();

			_root.SetLabelVisibility(false);

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
			
			_spielstand = _initialState.Spielstand;
			_root.UpdateSpielstand(_spielstand);

			_subscription = _commandResultObservable.Subscribe(HandleResult);
		}

		private GameState _initialState;
		private IObservable<(GameCommand, CommandResult)> _commandResultObservable;

		private static int GetY(MovedObject movedObject) =>
			movedObject switch
			{
				MovedObject.MovedBall b => b.Item.Item2,
				MovedObject.MovedPlayer p => p.Item2.Item2,
				_ => 0
			};
		
		private void HandleResult((GameCommand Command, CommandResult Result) outcome)
		{
			void HandleMoved(IEnumerable<MovedObject> moved)
			{
				var z = 0;
				foreach (var movedObject in moved.OrderBy(GetY).ThenBy(x => x.IsMovedPlayer ? 1 : 0))
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
			}
			
			switch (outcome.Result)
			{
				case CommandResult.Goal goal:
					HandleMoved(goal.Item.Item1);
					AudioPlayer.PlayKickHard();
					AudioPlayer.PlayJubel();
					
					_root.SetLabelVisibility(true);
					_root.UpdateSpielstand(goal.Item.Item3);
					break;
				
				case CommandResult.Moved moved:
					var ballMoved = moved.Item.Any(o => o.IsMovedBall);
					HandleMoved(moved.Item);
					if (outcome.Command.IsKick)
					{
						AudioPlayer.PlayKickHard();
					}
					else
					{
						AudioPlayer.PlayRunning(!ballMoved);
					}

					break;
			}
		}
	}
}
