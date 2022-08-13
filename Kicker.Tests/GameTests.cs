using FluentAssertions;
using Kicker.Domain;
using Xunit.Abstractions;
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
    public void Tor()
    {
        var player1 = new Player(Team.Team1, 1);

        _game
            .Configure(GameSettings.create(9, 3))
            .Move(player1, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right)
            .Move(player1, Direction.Down, Direction.Down)
            .Move(player1, Direction.Left, Direction.Down)
            .Move(player1, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right, Direction.Right)
            .Print();

        _game.LastResult?.IsGoal.Should().BeTrue();
    }
}

