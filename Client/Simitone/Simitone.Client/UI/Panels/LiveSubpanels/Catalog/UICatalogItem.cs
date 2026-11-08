using FSO.Client;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Simitone.Client.UI.Controls;
using Simitone.Client.UI.Model;
using Simitone.Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simitone.Client.UI.Panels.LiveSubpanels.Catalog
{
    public class UICatalogItem : UITSContainer
    {
        public static Dictionary<uint, Texture2D> IconCache = new Dictionary<uint, Texture2D>();
        public Texture2D BG;
        public Texture2D Icon;
        public Texture2D Outline;
        public bool Outlined;

        public UILabel PriceLabel;
        private UIBuyBrowsePanel BudgetProvider;
        private readonly List<(Texture2D Texture, Rectangle Bounds, bool Flip)> SpriteLayers =
            new List<(Texture2D, Rectangle, bool)>();
        private Rectangle SpriteBounds;

        public override void Draw(UISpriteBatch SBatch)
        {
            DrawLocalTexture(SBatch, BG, null, new Vector2(BG.Width-90, BG.Height-105) / -2, Vector2.One, new Color(104, 164, 184, 255));
            var iconSize = 55f;
            if (Icon != null)
            {
                
                if (Icon.Width / (float)Icon.Height < 1.1f || Icon.Width == 127 || Icon.Width == 128)
                {
                    iconSize = 77.7f;
                    var scale = iconSize/(float)Math.Sqrt(Icon.Width * Icon.Width + Icon.Height * Icon.Height);
                    DrawLocalTexture(SBatch, Icon, new Rectangle(0, 0, Icon.Width, Icon.Height), new Vector2((Icon.Width*scale-90) / -2, (Icon.Height*scale-105) / -2), new Vector2(scale));
                }
                else DrawLocalTexture(SBatch, Icon, new Rectangle(0, 0, Icon.Width / 2, Icon.Height), new Vector2((iconSize-90) / -2, (iconSize- 105) / -2), new Vector2(iconSize / Icon.Height, iconSize / Icon.Height));
            }

            if (SpriteLayers.Count > 0)
            {
                var scale = 62f / Math.Max(SpriteBounds.Width, SpriteBounds.Height);
                var origin = new Vector2(45, 52.5f) - new Vector2(SpriteBounds.Center.X, SpriteBounds.Center.Y) * scale;
                foreach (var layer in SpriteLayers)
                {
                    var position = origin + new Vector2(layer.Bounds.X, layer.Bounds.Y) * scale;
                    var source = new Rectangle(0, 0, layer.Bounds.Width, layer.Bounds.Height);
                    var effects = layer.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    SBatch.Draw(layer.Texture, FlooredLocalPoint(position), source, _BlendColor, 0f, Vector2.Zero,
                        _Scale * scale, effects, 0f);
                }
            }

            if (Outlined) DrawLocalTexture(SBatch, Outline, null, new Vector2(Outline.Width - 90, Outline.Height - 105) / -2, Vector2.One, UIStyle.Current.ActiveSelection);
            base.Draw(SBatch);
        }

        public UICatalogItem(UICatalogElement elem, UIBuyBrowsePanel budgetProvider)
        {
            BG = Content.Get().CustomUI.Get("pswitch_icon_bg.png").Get(GameFacade.GraphicsDevice);
            Icon = (elem.Special?.Res != null) ? elem.Special.Res.GetIcon(elem.Special.ResID) : GetObjIcon(elem.Item.GUID);
            if (Icon == null && elem.Special == null) LoadSpriteLayers(elem.Item.GUID);
            Outline = Content.Get().CustomUI.Get("pswitch_icon_sel.png").Get(GameFacade.GraphicsDevice);

            PriceLabel = new UILabel();
            PriceLabel.Alignment = TextAlignment.Center | TextAlignment.Middle;
            PriceLabel.Position = new Vector2(0, 110);
            PriceLabel.Size = new Vector2(90, 1);
            PriceLabel.CaptionStyle = PriceLabel.CaptionStyle.Clone();
            PriceLabel.CaptionStyle.Color = UIStyle.Current.Text;
            PriceLabel.CaptionStyle.Size = 14;
            PriceLabel.Caption = "§" + elem.Item.Price.ToString();
            Add(PriceLabel);

            BudgetProvider = budgetProvider;
        }

        public override void Selected()
        {
            if (BudgetProvider.ItemID == ItemID)
            {
                // Re-clicking the already-selected item: deselect it
                Outlined = false;
                BudgetProvider.Deselect();
            }
            else
            {
                Outlined = true;
                BudgetProvider.Selected(ItemID);
            }
        }

        public override void Deselected()
        {
            Outlined = false;
        }

        private void LoadSpriteLayers(uint guid)
        {
            try
            {
                var obj = Content.Get().WorldObjects.Get(guid);
                if (obj == null) return;

                var definition = obj.OBJ;
                var isMaster = definition.MasterID != 0 && definition.SubIndex == -1;
                var parts = isMaster
                    ? obj.Resource.List<OBJD>().Where(part => part.MasterID == definition.MasterID && part.SubIndex != -1)
                    : new[] { definition };

                foreach (var part in parts.OrderBy(part => isMaster ? PartOffset(part).Y : 0))
                {
                    if (part.BaseGraphicID == 0) continue;
                    var partOffset = isMaster ? PartOffset(part) : Vector2.Zero;
                    var group = obj.Resource.Get<DGRP>(part.BaseGraphicID);
                    // Match the lot thumbnail's north-facing, near-zoom isometric view.
                    var image = group?.GetImage(1, 3, 2) ?? group?.Images?.FirstOrDefault(view => view.Zoom == 3);
                    if (image?.Sprites == null) continue;

                    foreach (var sprite in image.Sprites)
                    {
                        if (sprite == null) continue;
                        var texture = sprite.GetTexture(GameFacade.GraphicsDevice);
                        if (texture == null) continue;
                        var size = sprite.GetDimensions();
                        if (size.X <= 0 || size.Y <= 0) continue;
                        var offset = sprite.ObjectOffset;
                        var x = partOffset.X + sprite.SpriteOffset.X + (offset.Y - offset.X) * 4;
                        var y = partOffset.Y + sprite.SpriteOffset.Y - size.Y
                            - (offset.X + offset.Y) * 2 - offset.Z * 15.68f;
                        var bounds = new Rectangle((int)x, (int)y, size.X, size.Y);
                        SpriteBounds = SpriteLayers.Count == 0 ? bounds : Rectangle.Union(SpriteBounds, bounds);
                        SpriteLayers.Add((texture, bounds, sprite.Flip));
                    }
                }
            }
            catch (Exception error)
            {
                SpriteLayers.Clear();
                DiagnosticLog.Error("catalog-thumbnail " + guid.ToString("X8"), error);
            }
        }

        private static Vector2 PartOffset(OBJD part)
        {
            // Same signed X/Y tile offsets as VMMultitileGroup, projected at near zoom.
            var x = (sbyte)((ushort)part.SubIndex >> 8);
            var y = (sbyte)((ushort)part.SubIndex & 0xFF);
            return new Vector2((y - x) * 64, -(x + y) * 32);
        }

        public Texture2D GetObjIcon(uint GUID)
        {
            if (!IconCache.ContainsKey(GUID))
            {
                var obj = Content.Get().WorldObjects.Get(GUID);
                if (obj == null)
                {
                    IconCache[GUID] = null;
                    return null;
                }
                var bmp = obj.Resource.Get<BMP>(obj.OBJ.CatalogStringsID);
                if (bmp != null) IconCache[GUID] = bmp.GetTexture(GameFacade.GraphicsDevice);
                else IconCache[GUID] = null;
            }
            return IconCache[GUID];
        }

        public static void ClearIconCache()
        {
            foreach (var item in IconCache)
            {
                item.Value?.Dispose();
            }
            IconCache.Clear();
        }
    }
}
