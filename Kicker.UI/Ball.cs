using System;
using Godot;

namespace Kicker.UI
{
    [Tool]
    public class Ball : Node2D, ITileNode
    {
        private static readonly Load.Factory<Ball> Factory = Load.Scene<Ball>();

        public static Ball Create() => Factory().Named("ball");
        
        [Export]
        public Vector2 Tile { get; set; }

        public override void _Process(float delta)
        {
            var movement = this.UpdateTransform(delta * 0.5f);
            var distance = movement.Length();
            if (distance < 0.01f) return;
            
            var direction = movement.Angle() + (float)(Math.PI / 2);

            var container = GetNode<Spatial>("ViewportContainer/Viewport/BallContainer");
            var meshInstance = container.GetNode<MeshInstance>("BallMesh");
            container.Transform = Godot.Transform.Identity.Rotated(Vector3.Forward, direction);
            meshInstance.Transform = meshInstance.Transform.Rotated(Vector3.Left, distance / 3);
        }
    }
}