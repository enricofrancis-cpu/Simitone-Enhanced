// UI/provider doubles run the production phone-book constructor and callbacks
// without a graphics device. Neighbour and family records use pinned FreeSO types.
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
namespace FSO.Client.UI.Framework {
    public class UIElement {
        public float Width; public Vector2 Position, Size;
        public bool Disabled;
    }
    public class UIContainer : UIElement {
        public List<UIElement> Children=new();
        public void Add(UIElement item) => Children.Add(item);
        public void Remove(UIElement item) => Children.Remove(item);
    }
}
namespace Simitone.Client.UI.Controls {
    using FSO.Client.UI.Framework;
    public class UIMobileDialog : UIContainer {
        public string Caption; public bool Closed;
        public void SetHeight(int height) { }
        public void Close() { Closed=true; }
    }
    public class UITouchStringList : UIElement {
        public List<string> BackingList=new(); public event Action<int> OnSelectionChange;
        public void Refresh() { }
        public void Select(int index) => OnSelectionChange?.Invoke(index);
    }
    public class UIBigButton : UIElement {
        public string Caption; public event Action<UIElement> OnButtonClick;
        public UIBigButton(bool green) { }
        public void Click() { if (!Disabled) OnButtonClick?.Invoke(this); }
    }
    public class UIAvatarSelectButton : UIElement { public UIAvatarSelectButton(object texture) { } }
}
namespace Simitone.Client.UI.Model {
    public static class UIIconCache {
        public static Exception Failure; public static int Requests; public static bool Missing;
        public static object GetNeighbor(uint guid) { Requests++; if(Failure!=null) throw Failure; return Missing?null:new object(); }
        public static object GetObject(object obj) => new object();
    }
}
namespace FSO.Content {
    public class Content {
        public static Content Instance=new();
        public static Content Get() => Instance;
        public Neighborhood Neighborhood=new();
        public Objects WorldObjects=new();
    }
    public class Neighborhood {
        public Dictionary<short,Neighbour> Neighbors=new();
        public Dictionary<ushort,FAMI> Families=new();
        public Dictionary<ushort,FAMs> Names=new();
        public Neighbour GetNeighborByID(short id) => Neighbors.GetValueOrDefault(id);
        public FAMI GetFamily(ushort id) => Families.GetValueOrDefault(id);
        public FAMs GetFamilyString(ushort id) => Names.GetValueOrDefault(id);
    }
    // Baseline-only seams let the same regression compile against the old code.
    public class Objects { public ObjectDefinition Get(uint guid) => new(); }
    public class ObjectDefinition { public Resource Resource=new(); public OBJD OBJ=new(); }
    public class Resource { public T Get<T>(ushort id) where T:class => null; }
}
namespace FSO.LotView.Model {
    public struct LotTilePos { public static LotTilePos OUT_OF_WORLD; }
    public enum Direction { NORTH }
}
namespace FSO.SimAntics {
    public class VM { public Context Context=new(); }
    public class Context {
        public int CreatedObjects;
        public Group CreateObjectInstance(uint guid, FSO.LotView.Model.LotTilePos pos, FSO.LotView.Model.Direction dir, bool ghost) { CreatedObjects++; return new(); }
    }
    public class Group { public object BaseObject=new(); public void Delete(Context context) { } }
}
