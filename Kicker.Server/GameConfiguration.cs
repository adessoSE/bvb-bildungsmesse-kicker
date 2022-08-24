using Kicker.Domain;

namespace Kicker.Server;

public class GameConfiguration
{
    public PlayerMapping Players { get; set; }
    
    public class PlayerMapping
    {
        public string[]? BVB { get; set; }
        public string[]? Adesso { get; set; }

        public IReadOnlyDictionary<string, Player> ToDictionary()
        {
            IEnumerable<KeyValuePair<string, Player>> Map(string[]? players, Team team)
            {
                return (players ?? Array.Empty<string>())
                    .Select((key, i) => new KeyValuePair<string, Player>(key, new Player(team, i + 1)));
            }
            
            return new Dictionary<string, Player>(Map(BVB, Team.BVB).Concat(Map(Adesso, Team.ADESSO)));
        }
    }
}