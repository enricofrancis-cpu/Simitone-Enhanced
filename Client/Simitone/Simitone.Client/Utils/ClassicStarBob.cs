using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Common.Utils;
using FSO.Content;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Simitone.Client.Utils
{
    internal static class ClassicStarBob
    {
        private const string MeshName = "xskin-head-star-root-star";
        private const string CacheKey = "simitone:installed-star-bob";

        public static DGRP3DMesh Get(Content content)
        {
            var renderer = content.RCMeshes;
            if (renderer.NameCache.TryGetValue(CacheKey, out var cached)) return cached;

            DGRP3DMesh result = null;
            DGRP3DGeometry geometry = null;
            Texture2D gold = null;
            try
            {
                var stock = content.AvatarMeshes.Get(MeshName);
                if (stock == null || stock.VertexBuffer.Length == 0 || stock.IndexBuffer.Length == 0)
                    throw new InvalidDataException("The installed Superstar StarBob mesh is missing or empty.");

                var positions = stock.VertexBuffer.Select(vertex => vertex.Position).ToArray();
                var bounds = BoundingBox.CreateFromPoints(positions);
                var center = (bounds.Min + bounds.Max) * 0.5f;
                var orientation = Matrix.CreateRotationY(MathHelper.PiOver2);

                // The original mesh is offset for the TS1 prop skeleton. The head renderer
                // supplies its own attachment point. Give the stock geometry a gold material.
                gold = TextureUtils.TextureFromColor(renderer.GD, new Color(200, 175, 42));
                geometry = new DGRP3DGeometry
                {
                    Pixel = gold,
                    SVerts = stock.VertexBuffer.Select(vertex => new DGRP3DVert(
                        Vector3.Transform(vertex.Position - center, orientation),
                        Vector3.TransformNormal(vertex.Normal, orientation), vertex.TextureCoordinate)).ToList(),
                    SIndices = stock.IndexBuffer.ToList()
                };
                geometry.SComplete(renderer.GD);

                result = new DGRP3DMesh(null, new OBJ(Stream.Null), renderer.GD);
                result.Geoms.Add(new Dictionary<Texture2D, DGRP3DGeometry> { [gold] = geometry });
                // The head effect divides specular strength across at least two material groups.
                result.Geoms.Add(new Dictionary<Texture2D, DGRP3DGeometry>());
                DiagnosticLog.Event("star-bob", "loaded the installed Superstar mesh");
            }
            catch (Exception error)
            {
                geometry?.Verts?.Dispose();
                geometry?.Indices?.Dispose();
                gold?.Dispose();
                DiagnosticLog.Error("star-bob-load-failed", error);
                result = renderer.Get("star.fsom");
            }

            renderer.NameCache[CacheKey] = result;
            return result;
        }
    }
}
