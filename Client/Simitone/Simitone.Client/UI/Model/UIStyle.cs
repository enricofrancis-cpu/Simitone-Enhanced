using Microsoft.Xna.Framework;
using FSO.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Simitone.Client.UI.Model
{
    public class UIStyle
    {
        public static UIStyle DARK = new UIStyle();
        
        public static UIStyle Current = DARK;


        private static readonly Dictionary<string, Texture2D> BlueChrome = new Dictionary<string, Texture2D>();

        // Recolour only the chrome. Sim portraits, status colours, and map art keep
        // their original palettes, and the existing button sprite sheets keep their states.
        internal static Texture2D GetBlueChrome(string name, GraphicsDevice device)
        {
            if (BlueChrome.TryGetValue(name, out var cached) && !cached.IsDisposed && cached.GraphicsDevice == device)
                return cached;

            var source = Content.Get().CustomUI.Get(name).Get(device);
            if (source.Format != SurfaceFormat.Color) return source;
            var pixels = new Color[source.Width * source.Height];
            source.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.G <= pixel.R || pixel.G < pixel.B) continue;

                // The channels stay at or below the input peak, preserving premultiplied alpha.
                var saturation = pixel.G - pixel.R;
                pixel.R = (byte)(pixel.R + saturation * 18 / 100);
                pixel.B = pixel.G;
                pixel.G = (byte)(pixel.G - saturation * 29 / 100);
                pixels[i] = pixel;
            }
            var texture = new Texture2D(device, source.Width, source.Height);
            texture.SetData(pixels);
            cached?.Dispose();
            BlueChrome[name] = texture;
            return texture;
        }


        public Color Bg = new Color(12, 27, 60) * 0.85f;
        public Color TitleBg = new Color(9, 21, 52) * 0.90f;

        public Color BtnNormal = Color.White;
        public Color BtnActive = new Color(94, 174, 255, 255);
        public Color BtnDisable = new Color(128, 128, 128, 255);

        public Color ActiveSelection = Color.Yellow;

        public Color Text = Color.White;
        public Color SecondaryText = new Color(94, 174, 255, 255);

        public Color DialogBg = new Color(12, 27, 60) * 0.88f;
        public Color DialogText = Color.White;
        public Color DialogTitle = new Color(8, 25, 56);

        public Color BtnTxt = new Color(0, 31, 63);
        public Color GreenBtnTxt = new Color(0, 37, 83);
        public Color BtnTxtShadow = Color.White * 0.5f;

        public Color PosMoney = new Color(0, 255, 128, 255);
        public Color NegMoney = new Color(255, 128, 0, 255);

        public Color SkillInactive = new Color(99, 109, 242, 255);
        public Color SkillActive = new Color(0, 255, 255, 255);
        public Color SkillNeeded = new Color(255, 191, 0, 255);

        public Color TransColor = new Color(0, 41, 69, 255);
    }
}
