// SPDX-License-Identifier: MPL-2.0
// Based on pinned FreeSO d8a3c175.
using FSO.Common.Utils;
using FSO.Files.RC;
using System;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace FSO.LotView.Components.Geometry
{
    /// <summary>
    /// A specific handler for pool logic.
    /// </summary>
    public class Modelled3DPool : Modelled3DFloor
    {
        private static Point[] PoolDirections =
{
            new Point(0, -1),
            new Point(1, -1),
            new Point(1, 0),
            new Point(1, 1),
            new Point(0, 1),
            new Point(-1, 1),
            new Point(-1, 0),
            new Point(-1, -1),
        };

        private static Modelled3DFloorTile[] PoolTiles;
        private static Modelled3DFloorTile[] CornerTiles;
        private const int RippleFrameCount = 16;
        private static Texture2D RippleSource;
        private static Color[] RippleSourcePixels;
        private static readonly Texture2D[] RippleFrames = new Texture2D[RippleFrameCount];

        public Modelled3DPool(Blueprint bp) : base(bp)
        {
        }

        public static void EnsureTilesLoaded()
        {
            if (PoolTiles != null) return;
            PoolTiles = new Modelled3DFloorTile[16];
            for (int i=0; i<16; i++)
            {
                using (var file = File.Open($"Content/3D/floor/pool_hq_{i}.obj", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var obj = new OBJ(file);
                    PoolTiles[i] = new Modelled3DFloorTile(obj, "pool.png");
                }
            }

            CornerTiles = new Modelled3DFloorTile[4];
            for (int i = 0; i < 4; i++)
            {
                using (var file = File.Open($"Content/3D/floor/poolcorner_hq_{i}.obj", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var obj = new OBJ(file);
                    CornerTiles[i] = new Modelled3DFloorTile(obj, "pool.png");
                }
            }
        }

        protected override void GenerateGeometry()
        {
            EnsureTilesLoaded();
            var verts = new List<TerrainParallaxVertex>();
            var inds = new List<int>();
            var indices = GeomForOffset.Keys;

            foreach (var index in indices)
            {
                //check surrounding tiles to determine pool tile type

                var x = index % Bp.Width;
                var y = index / Bp.Width;

                int poolAdj = 0;
                for (int i = 0; i < PoolDirections.Length; i++)
                {
                    var testTile = new Point(x, y) + PoolDirections[i];
                    if ((testTile.X <= 0 || testTile.X >= Bp.Width - 1) || (testTile.Y <= 0 || testTile.Y >= Bp.Height - 1)
                        || GeomForOffset.ContainsKey((ushort)(testTile.Y * Bp.Width + testTile.X))) poolAdj |= 1 << i;
                }

                var adj = (PoolSegments)poolAdj;
                ushort spriteNum = 0;
                if ((adj & PoolSegments.TopRight) > 0) spriteNum |= 1;
                if ((adj & PoolSegments.TopLeft) > 0) spriteNum |= 2;
                if ((adj & PoolSegments.BottomLeft) > 0) spriteNum |= 4;
                if ((adj & PoolSegments.BottomRight) > 0) spriteNum |= 8;

                AppendTile(inds, verts, PoolTiles[15], index);
                if (spriteNum != 15) AppendTile(inds, verts, PoolTiles[spriteNum], index);

                if ((adj & (PoolSegments.TopRight | PoolSegments.TopLeft)) == (PoolSegments.TopRight | PoolSegments.TopLeft) && (adj & PoolSegments.Top) == 0)
                    AppendTile(inds, verts, CornerTiles[0], index);
                if ((adj & (PoolSegments.TopLeft | PoolSegments.BottomLeft)) == (PoolSegments.TopLeft | PoolSegments.BottomLeft) && (adj & PoolSegments.Left) == 0)
                    AppendTile(inds, verts, CornerTiles[1], index);
                if ((adj & (PoolSegments.BottomLeft | PoolSegments.BottomRight)) == (PoolSegments.BottomLeft | PoolSegments.BottomRight) && (adj & PoolSegments.Bottom) == 0)
                    AppendTile(inds, verts, CornerTiles[2], index);
                if ((adj & (PoolSegments.BottomRight | PoolSegments.TopRight)) == (PoolSegments.BottomRight | PoolSegments.TopRight) && (adj & PoolSegments.Right) == 0)
                    AppendTile(inds, verts, CornerTiles[3], index);
            }
            Vertices = verts.ToArray();
            Indices = inds.ToArray();
        }

        public override Texture2D GetTexture(GraphicsDevice gd)
        {
            var source = PoolTiles[0].GetTexture(gd);
            if (source == null || !FSO.Content.Content.Get().TS1 || source.Format != SurfaceFormat.Color)
                return source;

            if (RippleSource != source)
            {
                foreach (var old in RippleFrames) old?.Dispose();
                Array.Clear(RippleFrames, 0, RippleFrames.Length);
                RippleSource = source;
                RippleSourcePixels = new Color[source.Width * source.Height];
                source.GetData(RippleSourcePixels);
            }

            // Keep the stock pool mesh, UVs and depth. Only colour in the blue
            // water part of the atlas changes, so the coping remains stationary.
            var frame = (int)(Environment.TickCount64 / 125 % RippleFrameCount);
            if (RippleFrames[frame] == null)
            {
                var phase = frame * (Math.PI * 2 / RippleFrameCount);
                var pixels = (Color[])RippleSourcePixels.Clone();
                for (int i = 0; i < pixels.Length; i++)
                {
                    var color = pixels[i];
                    if (color.A != 255 || color.B - color.R < 25 || color.G - color.R < 20) continue;
                    var x = (double)(i % source.Width) / source.Width;
                    var y = (double)(i / source.Width) / source.Height;
                    var ripple = (int)Math.Round(4 * Math.Sin(61 * x + 39 * y - phase) +
                        2 * Math.Sin(91 * x - 37 * y + 2 * phase));
                    pixels[i] = new Color(Math.Clamp(color.R + ripple, 0, 255),
                        Math.Clamp(color.G + ripple, 0, 255),
                        Math.Clamp(color.B + ripple, 0, 255), 255);
                }
                var texture = new Texture2D(gd, source.Width, source.Height, true, SurfaceFormat.Color);
                TextureUtils.UploadWithAvgMips(texture, gd, pixels);
                RippleFrames[frame] = texture;
            }
            return RippleFrames[frame];
        }
    }
}
