using System;
using Godot;
using Kicker.Domain;

namespace Kicker.UI
{
	[Tool]
	public class Player : Node2D, ITileNode
	{
		private static readonly Load.Factory<Player> Factory = Load.Scene<Player>();

		public static string GetName(Team team, int number) => $"{team}-{number}";
		
		public static Player Create(Team team, int number) => Factory().Named(GetName(team, number));

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
