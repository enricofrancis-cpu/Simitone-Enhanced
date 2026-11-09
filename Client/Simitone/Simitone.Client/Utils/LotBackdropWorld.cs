using FSO.Common;
using FSO.Common.Utils;
using FSO.LotView;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Simitone.Client.Utils
{
    /// <summary>Paints the visible space beyond the lot without creating extra tiles.</summary>
    internal sealed class LotBackdropWorld : World
    {
        private readonly Func<int> GetTerrainType;
        private SpriteBatch BackdropBatch;
        private Texture2D GrassGrain;

        public LotBackdropWorld(GraphicsDevice device, Func<int> getTerrainType) : base(device)
        {
            GetTerrainType = getTerrainType;
        }

        protected override void InternalDraw(GraphicsDevice device)
        {
            if (State.CameraMode == CameraRenderMode._2D && Blueprint.Terrain != null)
            {
                DrawBackdrop(device);
            }
            base.InternalDraw(device);
        }

        private void DrawBackdrop(GraphicsDevice device)
        {
            if (BackdropBatch == null) BackdropBatch = new SpriteBatch(device);
            if (GrassGrain == null)
            {
                const int size = 256;
                var random = new Random(8046);
                var pixels = new Color[size * size];
                for (int x = 0; x < size; x++)
                {
                    var blade = random.Next(228, 256);
                    for (int y = 0; y < size; y++)
                    {
                        var grain = (byte)(blade - random.Next(0, 20));
                        pixels[x + y * size] = new Color(grain, grain, grain, (byte)255);
                    }
                }
                GrassGrain = new Texture2D(device, size, size, true, SurfaceFormat.Color);
                TextureUtils.UploadWithMips(GrassGrain, device, pixels);
            }

            var type = GetTerrainType();
            var palette = LotTypeGrassInfo.LightGreen;
            var ground = type >= 0 && type < palette.Length ? palette[type] : palette[0];
            var tint = new Color(ground.ToVector3() * State.OutsideColor.ToVector3());
            var viewport = device.Viewport;
            var scale = Math.Max(0.1f, State.PreciseZoom) * Math.Max(1, PPXDepthEngine.SSAA);
            var offset = State.WorldSpace.GetScreenFromTile(State.CenterTile);
            var source = new Rectangle(
                (int)(offset.X - viewport.Width / (2 * scale)),
                (int)(offset.Y - viewport.Height / (2 * scale)),
                (int)Math.Ceiling(viewport.Width / scale),
                (int)Math.Ceiling(viewport.Height / scale));

            // The backdrop writes colour only, including when PPX binds a separate
            // depth texture as a second render target.
            var targets = FSOEnvironment.UseMRT ? device.GetRenderTargets() : null;
            if (targets?.Length > 1) device.SetRenderTarget((RenderTarget2D)targets[0].RenderTarget);
            BackdropBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.LinearWrap);
            BackdropBatch.Draw(GrassGrain, new Rectangle(0, 0, viewport.Width, viewport.Height), source, tint);
            BackdropBatch.End();
            if (targets?.Length > 1) device.SetRenderTargets(targets);
        }

        public override void Dispose()
        {
            GrassGrain?.Dispose();
            BackdropBatch?.Dispose();
            base.Dispose();
        }
    }
}
