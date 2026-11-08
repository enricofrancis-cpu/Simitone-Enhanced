// Minimal UI doubles let the actual UIMobileAlert layout and event handlers run
// without a graphics device or TS1 assets. This does not verify rendered pixels.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace FSO.Content { }
namespace FSO.Client {
    public static class GameFacade {
        public static GraphicsDevice GraphicsDevice => null;
        public static Strings Strings = new();
    }
    public class Strings { public string GetString(string table, string key) => key; }
}
namespace FSO.Common.Utils {
    public static class TextureGenerator { public static Texture2D GetPxWhite(GraphicsDevice gd) => null; }
}
namespace FSO.Common.Rendering.Framework.Model {
    public class UpdateState { public InputManager InputManager = new(); }
    public class InputManager {
        public object Focus; public int FocusChanges;
        public void SetFocus(object target) { Focus = target; FocusChanges++; }
    }
}
namespace FSO.Client.UI.Framework {
    public class UIElement {
        public float X, Y, Width, Height;
        public bool Visible = true;
        public Vector2 Position { get => new(X,Y); set { X=value.X; Y=value.Y; } }
        public void SetSize(float w, float h) { Width=w; Height=h; }
    }
    public class UISpriteBatch { }
}
namespace FSO.Client.UI.Controls {
    using FSO.Client.UI.Framework;
    [Flags] public enum TextAlignment { Left=1, Top=2, Center=4 }
    public class TextStyle {
        public static TextStyle DefaultLabel = new(); public int Size; public Color Color;
        public TextStyle Clone() => (TextStyle)MemberwiseClone();
    }
    public class TextRendererResult { public Rectangle BoundingBox; public object DrawingCommands; }
    public class TextRendererOptions {
        public TextAlignment Alignment; public float MaxWidth; public Vector2 Position, Scale, TopLeftIconSpace; public TextStyle TextStyle; public bool WordWrap;
    }
    public static class TextRenderer {
        public static TextRendererResult ComputeText(string text, TextRendererOptions options, object owner) => new() { BoundingBox = new Rectangle(0,0,0,int.Parse(text)) };
        public static void DrawText(object commands, object owner, UISpriteBatch batch) { }
    }
    public class UIImage : UIElement { public Texture2D Texture; }
    public delegate void ButtonClickDelegate(UIElement button);
    public class UIButton : UIElement { public bool Disabled; public string Caption; public event ButtonClickDelegate OnButtonClick; public void Click() => OnButtonClick?.Invoke(this); }
    public class UITextBox : UIElement {
        public int MaxChars; public string CurrentText=""; public Rectangle TextMargin;
        public TextAlignment Alignment; public TextStyle TextStyle = new(); public bool BackgroundSet;
        public event Action<UIElement> OnEnterPress;
        public void Enter() => OnEnterPress?.Invoke(this);
        public void SetBackgroundTexture(Texture2D texture, int a, int b, int c, int d) { BackgroundSet=true; }
    }
    public enum UIAlertButtonType { OK, Yes, No, Cancel }
    public class UIAlertButton { public string Text; public UIAlertButtonType Type; public ButtonClickDelegate Handler; }
    public class UIAlertOptions { public string Title, Message; public int Height, MaxChars=32; public bool TextEntry; public UIAlertButton[] Buttons; }
}
namespace Simitone.Client.UI.Model {
    public class UIStyle { public static UIStyle Current = new(); public Color DialogText=Color.White; }
}
namespace Simitone.Client.UI.Controls {
    using FSO.Client.UI.Framework;
    using FSO.Client.UI.Controls;
    using FSO.Common.Rendering.Framework.Model;
    public class UIBigButton : UIButton { public UIBigButton(bool green) { Width=275; Height=100; } }
    public class UIMobileDialog : UIElement {
        public new int Width=1024, Height;
        public string Caption; protected bool Closing;
        protected Vector2 _Scale=Vector2.One;
        public float InterpolatedAnimation=1;
        public List<UIElement> Children = new();
        public void Add(UIElement elem) => Children.Add(elem);
        public void Close() { Closing=true; }
        public void SetHeight(int h) { Height=h; }
        public virtual void GameResized() { }
        public virtual void CalculateMatrix() { }
        public virtual void Update(UpdateState state) { }
        public virtual void Draw(UISpriteBatch batch) { }
    }
}
