using System;
using Godot;
using Kicker.Domain;

namespace Kicker.UI
{
	[Tool]
	public class FieldBackground : Node2D
	{
		private static readonly Color LightGreen = new Color(0, 0.65f, 0);
		private static readonly Color DarkGreen = new Color(0, 0.6f, 0);
		private static readonly Color LineWhite = new Color(1, 1, 1, 0.6f);
		
		private UiSettings _settings;

		public void Init(UiSettings settings)
		{
			_settings = settings;
			Update();
		}
		
		public override void _Draw()
		{
			_settings ??= GameSettings.defaultSettings.ToUiSettings();
			
			DrawTiles();
			DrawCenterCircle();
			DrawLines();
		}

		private void DrawLines()
		{
			void DrawInnerRect(Rect2 rect)
			{
				DrawRect(rect.Grow(-0.5f), LineWhite, filled: false, antialiased: false);
			}

			var goalTop = _settings.GameSettings.goalTop;
			var goalHeight = _settings.GameSettings.GoalHeight;

			void DrawGoal(int column) => DrawInnerRect(
				new Rect2(new Vector2(column, goalTop) * UiSettings.PixelsPerTile,
					new Vector2(1, goalHeight) * UiSettings.PixelsPerTile));
			
			DrawGoal(0);
			DrawGoal(_settings.GameSettings.FieldWidth - 1);
			
			DrawInnerRect(new Rect2(Vector2.Right * UiSettings.PixelsPerTile, 
				(_settings.FieldTiles - Vector2.Right * 2) * UiSettings.PixelsPerTile));
			DrawLine(_settings.FieldPixels * new Vector2(0.5f, 0) + new Vector2(0.5f, 0.5f), 
				_settings.FieldPixels * new Vector2(0.5f, 1) + new Vector2(0.5f, 0.5f), LineWhite, antialiased: true);
		}
	
		private void DrawCenterCircle()
		{
			DrawArc(_settings.FieldPixels / 2, 
				0.08f * _settings.FieldPixels.x, 0, 2*(float)Math.PI, 100, LineWhite);
		}
	
		private void DrawTiles()
		{
			var tileSize = Vector2.One * UiSettings.PixelsPerTile;
		
			for (var y = 0; y < _settings.FieldTiles.y; y++)
			{
				for (var x = 0; x < _settings.FieldTiles.x; x++)
				{
					DrawRect(new Rect2(new Vector2(x, y) * UiSettings.PixelsPerTile, tileSize), 
						(y + x) % 2 == 0 ? LightGreen : DarkGreen);
				}
			}
		}
	}
}
