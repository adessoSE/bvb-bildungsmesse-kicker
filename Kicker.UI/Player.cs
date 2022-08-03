using System;
using Godot;
using Kicker.Domain;

namespace Kicker.UI
{
    public interface ITileNode
    {
        Vector2 Tile { get; set; }
    }
    
    [Tool]
    public class Player : Node2D, ITileNode
    {
        private static readonly Load.Factory<Player> Factory = Load.Scene<Player>();

        public static string GetName(Game.Team team, int number) => $"{team}-{number}";
        
        public static Player Create(Game.Team team, int number) => Factory().Named(GetName(team, number));

        [Export]
        public Vector2 Tile
        {
            get;
            set;
        }
        
        public override void _Process(float delta)
        {
            this.UpdateTransform(delta);
        }
    }
}
