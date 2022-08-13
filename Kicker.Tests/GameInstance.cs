using System.Text;
using Kicker.Domain;
using Xunit.Abstractions;

namespace Kicker.Tests;

internal class GameInstance
{
    private readonly ITestOutputHelper _output;
    private readonly Lazy<Game.Game> _lazyGame;
    private GameSettings? _settings;
    private Game.Game Game => _lazyGame.Value;

    public Game.CommandResult? LastResult { get; private set; }
    
    public GameInstance(ITestOutputHelper output)
    {
        _output = output;
        _lazyGame = new Lazy<Game.Game>(() => Domain.Game.create(_settings ?? GameSettings.defaultSettings));
    }

    public GameInstance Configure(GameSettings? settings)
    {
        _settings = settings;
        return this;
    }

    public GameInstance Move(Game.Player player, params Game.Direction[] directions)
    {
        foreach (var direction in directions)
        {
            LastResult = Domain.Game.processCommand(Domain.Game.GameCommand.NewMove(player, direction), Game);
        }

        return this;
    }

    public GameInstance Print()
    {
        var state = Domain.Game.getState(Game);
        var settings = state.Settings;
        var field = new string?[settings.FieldWidth, settings.FieldHeight];
        foreach (var player in state.Players)
        {
            field[player.Position.Item1, player.Position.Item2] = 
                $" {(player.Player.Team == Domain.Game.Team.Team1 ? '1' : '2')}{player.Player.Number} ";
        }

        field[state.BallPosition.Item1, state.BallPosition.Item2] = " [] ";

        void DrawBorder(char left, char seperator, char right)
        {
            var upperBorder = new StringBuilder();
            upperBorder.Append(left);
            for (var col = 0; col < settings.FieldWidth; col++)
            {
                upperBorder.Append('\x2500');
                upperBorder.Append('\x2500');
                upperBorder.Append('\x2500');
                upperBorder.Append('\x2500');
                if (col < settings.FieldWidth - 1)
                    upperBorder.Append(seperator);
            }

            upperBorder.Append(right);

            _output.WriteLine(upperBorder.ToString());
        }

        DrawBorder('\x250C', '\x252C', '\x2510');

        for (var row = 0; row < settings.FieldHeight; row++)
        {
            var b = new StringBuilder();
            b.Append('\x2502');
            for (var col = 0; col < settings.FieldWidth; col++)
            {
                var value = field[col, row];
                b.Append(value ?? "    ");
                b.Append('\x2502');
            }

            _output.WriteLine(b.ToString());
            
            if (row < settings.FieldHeight - 1)
                DrawBorder('\x251C', '\x253C', '\x2524');
        }
        
        DrawBorder('\x2514', '\x2534', '\x2518');

        return this;
    }
}