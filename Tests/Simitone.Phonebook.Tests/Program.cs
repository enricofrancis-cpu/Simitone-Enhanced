using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using Simitone.Client.UI.Controls;
using Simitone.Client.UI.Model;
using Simitone.Client.UI.Panels.LotControls;

static void Check(bool test,string message) { if(!test) throw new Exception(message); }
static Neighbour Sim(short id,uint guid,short family,short gender=0) {
    var data=new short[88]; data[(int)VMPersonDataVariable.TS1FamilyNumber]=family; data[(int)VMPersonDataVariable.Gender]=gender;
    return new Neighbour { NeighbourID=id, GUID=guid, Name="Renamed Sim "+id, PersonData=data, Relationships=new() };
}
static Neighborhood Reset() { Content.Instance=new(); UIIconCache.Failure=null; UIIconCache.Missing=false; UIIconCache.Requests=0; return Content.Get().Neighborhood; }
static void Empty(UICallNeighborPanel panel) {
    Check(panel.NeighborsByFamilyID.Count==0 && panel.SelectedFamily==-1 && panel.SelectedNeighbour==-1 && panel.CallButton.Disabled,"empty phone book must open with no selection");
    panel.FamilyList.Select(-2); panel.NeighbourList.Select(-2); panel.NeighbourList.Select(99);
    Check(panel.CallButton.Disabled,"invalid list selection enabled Call");
    int response=0; panel.OnResult+=value=>response=value;
    panel.Children.OfType<UIBigButton>().Single(b=>b.Caption=="Cancel").Click();
    Check(response==-1,"empty phone book cannot cancel");
}
var nb=Reset();
var caller=Sim(1,10,1); nb.Neighbors[1]=caller;
Empty(new UICallNeighborPanel(1,new VM())); // no known Sims: original -2 crash
nb.Neighbors.Remove(1);
Empty(new UICallNeighborPanel(1,new VM())); // missing caller
nb.Neighbors[1]=caller;
caller.Relationships=null;
Empty(new UICallNeighborPanel(1,new VM()));
nb=Reset(); caller=Sim(1,10,1); nb.Neighbors[1]=caller;
var valid=Sim(2,20,5); var other=Sim(3,30,7);
var pet=Sim(4,40,5,2); var shortData=Sim(5,50,5); shortData.PersonData=new short[62];
var nullData=Sim(6,60,5); nullData.PersonData=null;
var deletedFamily=Sim(7,70,8); var stale=Sim(8,80,5); var unassigned=Sim(9,90,0); var invalidGender=Sim(10,100,5,-1);
foreach(var sim in new[]{valid,other,pet,shortData,nullData,deletedFamily,stale,unassigned,invalidGender}) nb.Neighbors[sim.NeighbourID]=sim;
nb.Families[5]=new FAMI {ChunkID=5,FamilyGUIDs=new uint[]{20,40,50,60,90,100}};
nb.Families[7]=new FAMI {ChunkID=7,FamilyGUIDs=new uint[]{30}};
var familyName=new FAMs();
familyName.LanguageSets[0].Strings=new[]{new STRItem {LanguageCode=1,Value="Goth",Comment=""}};
nb.Names[5]=familyName;
foreach(var id in new[]{2,3,4,5,6,7,8,9,10,999,65538,-1}) caller.Relationships[id]=new(){0};
var vm=new VM(); var panel=new UICallNeighborPanel(1,vm);
Check(panel.NeighborsByFamilyID.Count==2 && panel.NeighborsByFamilyID[5].SequenceEqual(new short[]{2}) && panel.NeighborsByFamilyID[7].SequenceEqual(new short[]{3}),"stale/malformed/pet contacts leaked into book");
Check(panel.FamilyList.BackingList.SequenceEqual(new[]{"Goth","Unknown family"}),"family names or missing-name fallback changed");
panel.FamilyList.Select(999); panel.NeighbourList.Select(0);
Check(panel.SelectedFamily==-1 && panel.CallButton.Disabled,"out-of-range family selection enabled Call");
panel.FamilyList.Select(0);
Check(panel.NeighbourList.BackingList.Single()==valid.Name,"renamed Sim's saved name was not used");
panel.NeighbourList.Select(0);
Check(panel.SelectedNeighbour==2 && !panel.CallButton.Disabled && panel.Icon!=null,"valid contact cannot be selected");
Check(vm.Context.CreatedObjects==0,"portrait created an object in the live VM");
panel.NeighbourList.Select(int.MaxValue);
Check(panel.Icon==null && panel.CallButton.Disabled,"out-of-range contact retained a callable selection");
panel.FamilyList.Select(1);
Check(panel.SelectedNeighbour==-1 && panel.Icon==null && panel.CallButton.Disabled,"changing family retained the old contact");
panel.NeighbourList.Select(0);
var responses=new List<int>(); panel.OnResult+=responses.Add;
panel.CallButton.Click(); panel.CallButton.Click();
Check(responses.SequenceEqual(new[]{3}),"valid call did not send exactly the selected neighbour ID");
// A contact removed after opening/selection must not yield a stale call response.
panel=new UICallNeighborPanel(1,new VM()); panel.FamilyList.Select(0); panel.NeighbourList.Select(0);
panel.OnResult+=responses.Add; nb.Neighbors.Remove(2); panel.CallButton.Click();
Check(responses.Count==1 && panel.CallButton.Disabled,"deleted neighbour dispatched a call");
nb.Neighbors[2]=valid;
panel=new UICallNeighborPanel(1,new VM()); panel.FamilyList.Select(0); panel.NeighbourList.Select(0);
panel.OnResult+=responses.Add; nb.Families.Remove(5); panel.CallButton.Click();
Check(responses.Count==1 && panel.CallButton.Disabled,"deleted family dispatched a call");
nb.Families[5]=new FAMI {ChunkID=5,FamilyGUIDs=new uint[]{20}};
foreach(var missing in new[]{false,true}) {
    UIIconCache.Failure=missing?null:new InvalidDataException("missing custom appearance"); UIIconCache.Missing=missing;
    panel=new UICallNeighborPanel(1,new VM()); panel.FamilyList.Select(0); panel.NeighbourList.Select(0);
    Check(panel.Icon==null && !panel.CallButton.Disabled,"missing portrait blocked a valid call");
    int called=0; panel.OnResult+=value=>called=value; panel.CallButton.Click(); Check(called==2,"portrait failure changed call target");
}
UIIconCache.Failure=null; UIIconCache.Missing=false;
var alert=new UICallNeighborAlert(1,new VM()); int outer=0; alert.OnResult+=value=>outer=value;
alert.NPanel.Children.OfType<UIBigButton>().Single(b=>b.Caption=="Cancel").Click();
Check(outer==-1 && alert.Closed,"cancel did not close outer dialog");
Console.WriteLine("PASS: production phone book opens empty, filters stale/malformed/pet contacts, handles invalid selections, preserves renamed contacts, isolates portrait previews and validates Call/Cancel responses (headless UI doubles).");
