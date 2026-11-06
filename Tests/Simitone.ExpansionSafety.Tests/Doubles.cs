// Headless VM/content context only; tested primitives/helper and inventory serialization are production source.
using FSO.Files.Formats.IFF.Chunks;
namespace FSO.Files.Formats.IFF { public class IffFile {} public abstract class IffChunk { public abstract void Read(IffFile iff,Stream stream); public virtual bool Write(IffFile iff,Stream stream)=>true; } }
namespace FSO.Files.Formats.IFF.Chunks {
 public enum STRLangCode { EnglishUS }
 public class STR { public Dictionary<int,string> Values=new(); public string GetString(int id)=>Values.GetValueOrDefault(id,""); public string GetString(int id,STRLangCode language)=>GetString(id); public void SetString(int id,string value,STRLangCode language)=>Values[id]=value; }
}
namespace FSO.Content.TS1 {
 public class TS1AvatarTextureProvider { public string[] Names=Array.Empty<string>(); public IEnumerable<string> GetAllNames()=>Names; }
 public class Collection { public Dictionary<string,List<string>> ClothesByAvatarType=new(); }
 public class Catalogue { public Dictionary<string,Collection> CollectionsByName=new(); }
}
namespace FSO.SimAntics.Model { public enum VMPersonDataVariable { NeighborId } }
namespace FSO.SimAntics.Engine {
 public interface VMPrimitiveOperand { void Read(byte[] data); void Write(byte[] data); }
 public abstract class VMPrimitiveHandler { public abstract VMPrimitiveExitCode Execute(VMStackFrame context,VMPrimitiveOperand args); }
 public enum VMPrimitiveExitCode { GOTO_TRUE,GOTO_FALSE }
 public class VMStackFrame { public FSO.SimAntics.VM VM=new(); public ThreadState Thread=new(); public FSO.SimAntics.VMAvatar Caller=new(); public FSO.SimAntics.VMEntity StackObject=new(); }
 public class ThreadState { public short[] TempRegisters=new short[20]; }
}
namespace FSO.SimAntics {
 public class VM { public VMEntity Target; public VMEntity GetObjectById(short id)=>Target; }
 public class VMEntity { public Obj Object=new(); }
 public class VMAvatar:VMEntity { public bool IsCat,IsDog; public short GetPersonData(Model.VMPersonDataVariable field)=>1; }
 public class Obj { public Resource Resource=new(); public Definition OBJ=new(); public uint GUID=123; }
 public class Definition { public ushort BodyStringID=1; public uint GUID=123; }
 public class Resource { public STR Strings=new(); public T Get<T>(ushort id)=>(T)(object)Strings; }
}
namespace FSO.Content {
 public class Content { static Content instance=new(); public static Content Get()=>instance; public TS1.Catalogue BCFGlobal=new(); public object AvatarTextures=new TS1.TS1AvatarTextureProvider(); public Neighborhood Neighborhood=new(); }
 public class Neighborhood { public Dictionary<short,List<InventoryItem>> Inventories=new(); public int Changed; public void AvatarChanged(uint id)=>Changed++; public List<InventoryItem> GetInventoryByNID(short id)=>Inventories.GetValueOrDefault(id); public void SetInventoryForNID(short id,List<InventoryItem> values)=>Inventories[id]=values; }
}
