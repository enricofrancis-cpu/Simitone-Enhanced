using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.Utils;
using FSO.Content.TS1;
using FSO.Files.Formats.IFF.Chunks;
static void Check(bool value,string reason) { if(!value) throw new Exception(reason); }
var content=FSO.Content.Content.Get(); var catalog=new Collection(); content.BCFGlobal.CollectionsByName["b"]=catalog;
var bodies=new List<string>{"b001mafit_light","b002mafit_dark"}; catalog.ClothesByAvatarType["mafit"]=bodies;
((TS1AvatarTextureProvider)content.AvatarTextures).Names=new[]{"b001mafitlgt_light.bmp","b002mafitdrk_dark.bmp","u001dog_brown.bmp"};
var avatar=new VMAvatar(); var strings=avatar.Object.Resource.Strings;
strings.Values[1]="b001mafit_light,BODY=b001mafitlgt_light";strings.Values[14]="lgt";
Check(VMTS1PurchasableOutfitHelper.GetValidOutfits(avatar,0).Length==1,"light outfit missing");
strings.Values[14]="drk"; Check(VMTS1PurchasableOutfitHelper.GetValidOutfits(avatar,0).Single().Item1.StartsWith("b002"),"dark-only outfit lost after light query");
strings.Values[14]="lgt"; Check(VMTS1PurchasableOutfitHelper.GetSuitIndex(avatar,0)==0,"exact current outfit not found");
strings.Values[1]=strings.Values[1].ToUpperInvariant(); Check(VMTS1PurchasableOutfitHelper.GetSuitIndex(avatar,0)==0,"case-insensitive current outfit not found");
var before=strings.Values[1];var changed=content.Neighborhood.Changed;
foreach(short invalid in new short[]{-1,99}) Check(VMTS1PurchasableOutfitHelper.SetSuit(avatar,0,invalid)==-1,"invalid selection accepted");
Check(strings.Values[1]==before && content.Neighborhood.Changed==changed,"invalid selection wrote appearance state");
Check(VMTS1PurchasableOutfitHelper.SetSuit(avatar,0,0)==0,"valid outfit replacement failed");
Check(bodies.Count==2,"native helper mutated catalogue");
Check(VMTS1PurchasableOutfitHelper.GetValidOutfits(avatar,99).Length==0 && VMTS1PurchasableOutfitHelper.GetSuitIndex(avatar,99)==-1,"invalid outfit category not rejected");
strings.Values[1]="bad"; Check(VMTS1PurchasableOutfitHelper.GetValidOutfits(avatar,0).Length==0,"malformed body string accepted");
catalog.ClothesByAvatarType["dog"]=new(){"u001dog_brown","u002dog_missing"}; avatar.IsDog=true;
Check(VMTS1PurchasableOutfitHelper.GetValidOutfits(avatar,0).Length==1 && catalog.ClothesByAvatarType["dog"].Count==2,"dog query lost shared breed catalogue");
var neighborhood=content.Neighborhood;var primitive=new VMTS1InventoryOperations();var frame=new VMStackFrame();
var operand=new VMTS1InventoryOperationsOperand { GUID=123,TokenType=6,Flags=2,Flags2=1 };
frame.Thread.TempRegisters[1]=0;
List<InventoryItem> Stack(ushort count) { var list=new List<InventoryItem>{new(){GUID=123,Type=6,Count=count}}; neighborhood.Inventories[1]=list;return list; }
foreach(var mode in new[]{VMTS1InventoryMode.RemoveToken,VMTS1InventoryMode.RemoveTokenAtIndex}) {
 operand.Mode=mode;var items=Stack(1);frame.Thread.TempRegisters[0]=2;
 Check(primitive.Execute(frame,operand)==VMPrimitiveExitCode.GOTO_FALSE && items.Single().Count==1,"excessive removal changed inventory");
 frame.Thread.TempRegisters[0]=-2;Check(primitive.Execute(frame,operand)==VMPrimitiveExitCode.GOTO_FALSE && items[0].Count==1,"negative removal changed inventory");
 frame.Thread.TempRegisters[0]=-1;Check(primitive.Execute(frame,operand)==VMPrimitiveExitCode.GOTO_TRUE && items.Count==0,"remove-all sentinel changed");
 items=Stack(3);frame.Thread.TempRegisters[0]=1;Check(primitive.Execute(frame,operand)==VMPrimitiveExitCode.GOTO_TRUE && items[0].Count==2,"valid partial removal failed");
}
operand.Mode=VMTS1InventoryMode.AddToken;var maximum=Stack(65535);frame.Thread.TempRegisters[0]=1;
Check(primitive.Execute(frame,operand)==VMPrimitiveExitCode.GOTO_FALSE && maximum[0].Count==65535,"addition overflowed");
frame.Thread.TempRegisters[0]=-1;Check(primitive.Execute(frame,operand)==VMPrimitiveExitCode.GOTO_FALSE,"negative addition accepted");
foreach(var (mode,id) in new[]{(VMTS1InventoryMode.Temp0NeighborAsAutofollow,10u),(VMTS1InventoryMode.Temp0NeighborAsFollowHome,11u)}) {
 foreach(byte flags in new byte[]{0,2}) {
  var items=new List<InventoryItem>{new(){GUID=0,Type=2,Count=60},new(){GUID=1,Type=2,Count=70}};neighborhood.Inventories[1]=items;
  operand.Mode=mode;operand.Flags=flags;frame.Thread.TempRegisters[0]=201;primitive.Execute(frame,operand);
  frame.Thread.TempRegisters[0]=202;primitive.Execute(frame,operand);
  Check(items.Single(x=>x.GUID==id).Count==202 && items.Count==3,"follower upsert duplicated or used count instead of neighbour ID");
  Check(items.Single(x=>x.GUID==0).Count==60 && items.Single(x=>x.GUID==1).Count==70,"follower changed motives");
  var chunk=new NGBH();chunk.InventoryByID[1]=items;byte[] data;
  using(var stream=new MemoryStream()){chunk.Write(null,stream);data=stream.ToArray();}
  var reloaded=new NGBH();using(var stream=new MemoryStream(data)) reloaded.Read(null,stream);
  Check(reloaded.InventoryByID[1].Single(x=>x.GUID==id).Count==202 && reloaded.InventoryByID[1][0].Count==60,"inventory roundtrip lost follower/motives");
 }
}
Console.WriteLine("PASS: native outfit catalogue/tone/breed preservation and lookup; invalid selections; inventory underflow/overflow/negative counts; follower upserts and motive preservation with real NGBH roundtrips (headless VM/content doubles).");
