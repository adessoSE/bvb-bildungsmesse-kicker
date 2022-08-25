using Kicker.Domain;

namespace Kicker.Client;

public static class KickerClient
{
    private static readonly Lazy<KickerHubClient> LazyClient = new(KickerHubClient.CreateAndConnect);
    private static KickerHubClient Client => LazyClient.Value;

    private static void Move(Direction direction) => Client.SendCommand(ClientCommand.NewClientMove(direction));

    public static void MoveLeft() => Move(Direction.Left);
    public static void MoveRight() => Move(Direction.Right);
    public static void MoveUp() => Move(Direction.Up);
    public static void MoveDown() => Move(Direction.Down);
}