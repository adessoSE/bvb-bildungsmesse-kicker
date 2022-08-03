using System;
using Godot;
using Kicker.Domain;

namespace Kicker.UI
{
    public static class Load
    {
        public delegate T Factory<out T>(Action<T> initAction = null);
        
        public static Factory<T> Scene<T>() where T : class
        {
            var lazy = new Lazy<PackedScene>(() => ResourceLoader.Load<PackedScene>($"res://{typeof(T).Name}.tscn"));
            return initAction =>
            {
                var instance = lazy.Value.Instance<T>(); 
                initAction?.Invoke(instance);
                return instance;
            };
        }
    }
    
    public static class SharedExtensions
    {
        public static Vector2 UpdateTransform<T>(this T node, float delta = 0)
            where T : Node2D, ITileNode
        {
            var target = node.Tile * UiSettings.PixelsPerTile;
            var current = node.Transform.origin;
            node.Transform = delta == 0 
                ? new Transform2D(0, target)
                : new Transform2D(0, current.LinearInterpolate(target, delta * 15));
            var final = node.Transform.origin;
            var movement = final - current;
            return movement;
        }

        public static T Named<T>(this T node, string name) where T : Node
        {
            node.Name = name;
            return node;
        }

        public static Vector2 ToVector2(this Tuple<int, int> tuple) => new Vector2(tuple.Item1, tuple.Item2);
    }
}