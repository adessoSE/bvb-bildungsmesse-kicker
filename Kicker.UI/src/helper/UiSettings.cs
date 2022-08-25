using Godot;
using Kicker.Domain;

namespace Kicker.UI
{
	public class UiSettings
	{
		public GameSettings GameSettings { get; }
		public const int PixelsPerTile = 24;
		public const int PixelFactor = 8;

		public UiSettings(GameSettings settings)
		{
			GameSettings = settings;
			FieldTiles = new Vector2(settings.FieldWidth, settings.FieldHeight);
			FieldPixels = FieldTiles * PixelsPerTile;
			ScaledFieldPixels = FieldPixels * PixelFactor;
			TileSize = new Vector2(PixelsPerTile, PixelsPerTile);
		}

		public Vector2 TileSize { get; }
		public Vector2 FieldTiles { get; }
		public Vector2 FieldPixels { get; }
		public Vector2 ScaledFieldPixels { get; }
	}
	
	public static class ConfigExtensions
	{
		public static UiSettings ToUiSettings(this GameSettings settings) => new UiSettings(settings);
	}
}
