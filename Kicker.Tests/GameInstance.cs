using System.Text;
using Kicker.Domain;
using static Kicker.Domain.GameModule;

namespace Kicker.Tests;

internal class GameInstance
{
    private readonly ITestOutputHelper _output;
    private readonly Lazy<Game> _lazyGame;
    private GameSettings? _settings;
    private Game Game => _lazyGame.Value;

    public CommandResult? LastResult { get; private set; }
    
    public GameInstance(ITestOutputHelper output)
    {
        _output = output;
        _lazyGame = new Lazy<Game>(() => create(_settings ?? GameSettings.defaultSettings));
    }

    public GameInstance Configure(GameSettings? settings)
    {
        _settings = settings;
        return this;
    }

    public GameInstance Move(Player player, params Direction[] directions)
    {
        foreach (var direction in directions)
        {
            LastResult = processCommand(GameCommand.NewMove(player, direction), Game);
        }

        return this;
    }
    
    public GameInstance Kick(Player player)
    { 
        LastResult = processCommand(GameCommand.NewKick(player), Game);
        return this;
    }

    public GameInstance Print()
    {
        var state = getState(Game);
        var settings = state.Settings;
        
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
                var value = get(col, row, Game);
                var s = value switch
                {
                    TileValue.PlayerTile player => $" {(player.Item.Team == Team.BVB ? '1' : '2')}{player.Item.Number} ",
                    var ball when ball.IsBallTile => " [] ",
                    var x when x.IsOutTile => " XX ",
                    _ => "    "
                };
                
                b.Append(s);
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