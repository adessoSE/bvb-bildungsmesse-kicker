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

		public static GameRoot Create(GameState state, IObservable<CommandResult> moveObservable) => Factory(g =>
		{
			g._initialState = state;
			g._commandResultObservable = moveObservable;
		});
		
		private GameRoot(){}

		private UiSettings _settings;
		private IDisposable _subscription;

		[Signal]
		public delegate void goal();

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
			
			_subscription = _commandResultObservable.Subscribe(HandleResult);

			
		}

		private GameState _initialState;
		private IObservable<CommandResult> _commandResultObservable;

		private static int GetY(MovedObject movedObject) =>
			movedObject switch
			{
				MovedObject.MovedBall b => b.Item.Item2,
				MovedObject.MovedPlayer p => p.Item2.Item2,
				_ => 0
			};
		
		private void HandleResult(CommandResult result)
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
			
			switch (result)
			{
				case CommandResult.Goal goal:
					HandleMoved(goal.Item.Item1);
					break;
				
				case CommandResult.Moved moved:
					HandleMoved(moved.Item);
					break;
			}
		}
	}
}
