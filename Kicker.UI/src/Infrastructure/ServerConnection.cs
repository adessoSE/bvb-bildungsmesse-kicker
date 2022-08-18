using System;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kicker.Domain;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kicker.UI.Infrastructure
{
	public class ServerConnection : IObservable<IConnectionEvent>
	{
		private readonly IHubConnectionBuilder connectionBuilder;
		private readonly IObservable<IConnectionEvent> innerObservable;

		public ServerConnection()
		{
			connectionBuilder = new HubConnectionBuilder()
				.WithUrl("http://localhost:7014/gamehub")
				.AddNewtonsoftJsonProtocol()
				.ConfigureLogging(l => l
					.SetMinimumLevel(LogLevel.Debug)
					.AddProvider(new DebugLoggerProvider()))
				.WithAutomaticReconnect(new AlwaysReconnectPolicy());

			innerObservable = Observable.Create<IConnectionEvent>(async observer =>
			{
				var connectionCancellationTokenSource = new CancellationTokenSource();
				var connectionCancellationToken = connectionCancellationTokenSource.Token;

				void OnConnected(HubConnection connection)
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

				var connection = connectionBuilder.Build();

				connection.Reconnected += _ =>
				{
					OnConnected(connection);
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
						OnConnected(connection);
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
		}

		public IDisposable Subscribe(IObserver<IConnectionEvent> observer) => innerObservable.Subscribe(observer);

		private class AlwaysReconnectPolicy : IRetryPolicy
		{
			public TimeSpan? NextRetryDelay(RetryContext retryContext)
			{
				return TimeSpan.FromSeconds(1);
			}
		}
	}
}
