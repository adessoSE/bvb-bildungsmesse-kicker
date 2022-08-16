using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Godot;
using Kicker.Domain;
using Kicker.UI.Infrastructure;

namespace Kicker.UI
{
	[Tool]
	public class Root : Node2D
	{
		private IDisposable _subscription;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_subscription?.Dispose();
		}

		public override void _Ready()
		{
			var connection = CreateConnection();
			
			var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
			var connectionEvents = connection.ObserveOn(scheduler).Publish().RefCount();

			var moveNotifications =
				connectionEvents
					.OfType<ConnectionEvent.Notification>()
					.Select(n => n.Payload)
					.OfType<GameNotification.MoveNotification>()
					.Select(m => m.Item);

			void OnNext(IConnectionEvent x)
			{
				switch (x)
				{
					case ConnectionEvent.Notification {Payload: GameNotification.State state}:
					{
						RemoveExistingGame();
						InitializeGame(state.Item, moveNotifications);
						break;
					}
					case ConnectionEvent.Connecting:
					{
						RemoveExistingGame();
						break;
					}
				}
			}

			_subscription =
				connectionEvents
					.Do(OnNext)
					.Subscribe();
		}
		
		private IObservable<IConnectionEvent> CreateConnection()
		{
			IObservable<IConnectionEvent> connection =
				Engine.EditorHint
					? new MockConnection()
					: new ServerConnection();

			if (connection is Node node)
			{
				AddChild(node);
			}
			
			return connection;
		}

		private void RemoveExistingGame()
		{
			var game = GetNodeOrNull("ViewportContainer/Viewport/GameRoot");
			game?.QueueFree();
		}

		private void InitializeGame(GameState state, IObservable<CommandResult> observable)
		{
			var uiSettings = state.Settings.ToUiSettings();

			var viewportSize = uiSettings.FieldPixels;
			var displaySize = uiSettings.ScaledFieldPixels;

			var container = GetNode<ViewportContainer>("ViewportContainer");
			var viewport = container.GetNode<Viewport>("Viewport");

			foreach (Node child in viewport.GetChildren())
			{
				child.QueueFree();
			}

			viewport.Size = viewportSize;
			container.RectSize = viewportSize;
			container.RectScale = Vector2.One * UiSettings.PixelFactor;

			var game = GameRoot.Create(state, observable).Named("GameRoot");
			viewport.AddChild(game);

			if (!Engine.EditorHint)
			{
				GetTree().SetScreenStretch(SceneTree.StretchMode.Mode2d, SceneTree.StretchAspect.Keep, displaySize);
			}
		}
	}
}
