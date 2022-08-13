using System;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Kicker.Domain;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
			if (Engine.EditorHint)
			{
				InitializeGame(new GameState(
						GameSettings.defaultSettings,
						new[] {new PlayerState(new Tuple<int, int>(3, 3), new Domain.Player(Team.Team1, 1))},
						new Tuple<int, int>(5, 5), GameStatus.Running),
					Observable.Empty<CommandResult>());

				return;
			}

			var connectionEvents = Connect();
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

		private IObservable<IConnectionEvent> Connect()
		{
			var factory = new HubConnectionBuilder()
				.WithUrl("http://localhost:7014/gamehub")
				.AddNewtonsoftJsonProtocol()
				.ConfigureLogging(l => l
					.SetMinimumLevel(LogLevel.Debug)
					.AddProvider(new DebugLoggerProvider()))
				.WithAutomaticReconnect(new AlwaysReconnectPolicy());

			var observable = Observable.Create<IConnectionEvent>(async observer =>
			{
				var connectionCancellationTokenSource = new CancellationTokenSource();
				var connectionCancellationToken = connectionCancellationTokenSource.Token;

				void Subscribe(HubConnection connection)
				{
					observer.OnNext(new ConnectionEvent.Connected());

					Observable
						.Create<GameNotification>(async o =>
						{
							var cancellationTokenSource = new CancellationTokenSource();
							var token = cancellationTokenSource.Token;
							var channel =
								await connection.StreamAsChannelAsync<GameNotification>("Subscribe",
									connectionCancellationToken);
							_ = Task.Run(async () =>
							{
								while (await channel.WaitToReadAsync(token))
								{
									var next = await channel.ReadAsync(token);
									o.OnNext(next);
								}
							}, token);

							return Disposable.Create(cancellationTokenSource.Cancel);
						})
						.Select(e => new ConnectionEvent.Notification(e))
						.Subscribe(observer.OnNext);
				}

				var connection = factory.Build();

				connection.Reconnected += _ =>
				{
					Subscribe(connection);
					return Task.CompletedTask;
				};

				connection.Reconnecting += _ =>
				{
					observer.OnNext(new ConnectionEvent.Connecting());
					return Task.CompletedTask;
				};

				observer.OnNext(new ConnectionEvent.Connecting());

				while (true)
					try
					{
						await connection.StartAsync(connectionCancellationToken);
						Subscribe(connection);
						return Disposable.Create(() =>
						{
							connectionCancellationTokenSource.Cancel();
							connection.StopAsync(CancellationToken.None);
							connection.DisposeAsync();
						});
					}
					catch (HttpRequestException)
					{
						await Task.Delay(TimeSpan.FromSeconds(1), connectionCancellationToken);
						// Retry
					}
			});

			var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
			return observable.ObserveOn(scheduler).Publish().RefCount();
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

			if (Engine.EditorHint)
			{
			}
			else
			{
				GetTree().SetScreenStretch(SceneTree.StretchMode.Mode2d, SceneTree.StretchAspect.Keep, displaySize);
			}
		}

		private class AlwaysReconnectPolicy : IRetryPolicy
		{
			public TimeSpan? NextRetryDelay(RetryContext retryContext)
			{
				return TimeSpan.FromSeconds(1);
			}
		}

		private interface IConnectionEvent
		{
		}

		private static class ConnectionEvent
		{
			public record Connecting : IConnectionEvent;

			public record Connected : IConnectionEvent;

			public record Notification(GameNotification Payload) : IConnectionEvent;
		}
	}
}