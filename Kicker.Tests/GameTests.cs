using Kicker.Domain;
using static Kicker.Domain.Game;

namespace Kicker.Tests;

public class GameTests
{
    private readonly GameInstance _game;

    public GameTests(ITestOutputHelper output)
    {
        _game = new GameInstance(output);
    }

    [Fact]
    public void Go()
    {
        Client.KickerClient.MoveRight();
        Client.KickerClient.MoveRight();
        Client.KickerClient.MoveRight();
        Client.KickerClient.MoveRight();

    }
    
    [Fact]
    public void Tor()
    {
        var player1 = new Player(Team., 1);

        _game
            .Configure(GameSettings.create(9, 3))
            .Move(player1, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right)
            .Move(player1, Direction.Down, Direction.Down)
            .Move(player1, Direction.Left, Direction.Down)
            .Move(player1, Direction.Right, Direction.Right)
            .Kick(player1)
            .Print();

        _game.LastResult?.IsGoal.Should().BeTrue();

        _game.Move(player1, Direction.Left);

        _game.LastResult?.IsIgnored.Should().BeTrue();
    }
}

