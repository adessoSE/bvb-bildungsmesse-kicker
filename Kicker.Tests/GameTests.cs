using Kicker.Domain;

namespace Kicker.Tests;

public class GameTests
{
    [Fact]
    public void Tor()
    {
        var player1 = new Game.Player(Game.Team.Team1, 1);
        
        var settings = GameSettings.create(9, 3);
        var game = Game.create(settings);
        //Game.processCommand(Game.GameCommand.NewMove())

        var ballPosition = Game.getState(game).BallPosition;

        var result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Down)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        result = Game.processCommand(Game.GameCommand.NewMove(player1, Game.Direction.Right)).Invoke(game);
        var position = Game.getState(game).Players.Single(p => p.Player.Equals(player1)).Position;
        
        ballPosition = Game.getState(game).BallPosition;
    }
}