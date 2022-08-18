using Kicker.Domain;

namespace Kicker.UI.Infrastructure
{
	public static class ConnectionEvent
	{
		public record Connecting : IConnectionEvent;

		public record Connected : IConnectionEvent;

		public record Notification(GameNotification Payload) : IConnectionEvent;
	}
}
