using System;
using System.Collections.Generic;
using System.IO;
using FSO.Content;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Simitone.Client.Utils
{
    internal static class MoodPlumbob
    {
        private const int MoodSteps = 10;

        public static DGRP3DMesh Get(Content content, DGRP3DMesh original, int mood)
        {
            if (original == null || original.Geoms.Count == 0) return original;
            var step = (int)Math.Round(MathHelper.Clamp(mood, -100, 100) * (MoodSteps / 100f));
            if (step == MoodSteps) return original;

            var renderer = content.RCMeshes;
            var key = "simitone:plumbob-mood:" + step;
            if (renderer.NameCache.TryGetValue(key, out var cached)) return cached;

            // Share the original vertex and index buffers across all mood colours.
            var mesh = new DGRP3DMesh(null, new OBJ(Stream.Null), renderer.GD);
            mesh.Bounds = original.Bounds;
            var materials = new Dictionary<Texture2D, Texture2D>();
            foreach (var group in original.Geoms)
            {
                var tintedGroup = new Dictionary<Texture2D, DGRP3DGeometry>();
                foreach (var item in group)
                {
                    if (!materials.TryGetValue(item.Key, out var texture))
                    {
                        var source = item.Key;
                        if (source.Format != SurfaceFormat.Color) return original;
                        var pixels = new Color[source.Width * source.Height];
                        source.GetData(pixels);
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            var color = pixels[i];
                            var neutral = new Color(color.G, color.G * 3 / 4, color.G / 8, color.A);
                            var red = new Color((int)color.G, color.G / 8, color.G / 8, color.A);
                            pixels[i] = step < 0
                                ? Color.Lerp(neutral, red, -step / (float)MoodSteps)
                                : Color.Lerp(neutral, color, step / (float)MoodSteps);
                        }
                        texture = new Texture2D(renderer.GD, source.Width, source.Height);
                        texture.SetData(pixels);
                        materials.Add(source, texture);
                    }
                    tintedGroup.Add(texture, item.Value);
                }
                mesh.Geoms.Add(tintedGroup);
            }
            renderer.NameCache.Add(key, mesh);
            return mesh;
        }
    }
}
