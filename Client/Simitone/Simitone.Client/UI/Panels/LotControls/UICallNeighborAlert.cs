using FSO.Client.UI.Framework;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using Simitone.Client.UI.Controls;
using Simitone.Client.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simitone.Client.UI.Panels.LotControls
{
    public class UICallNeighborAlert : UIMobileDialog
    {
        public UICallNeighborPanel NPanel;
        public short SelectedNeighbour
        {
            get
            {
                return NPanel.SelectedNeighbour;
            }
        }
        
        public event Action<int> OnResult;

        public UICallNeighborAlert(short callerNID, VM vm)
        {
            Caption = "Call Neighbour";
            SetHeight(490);
            NPanel = new UICallNeighborPanel(callerNID, vm);
            NPanel.Position = new Microsoft.Xna.Framework.Vector2((Width - 1030) / 2, 110);
            NPanel.OnResult += (res) => { OnResult?.Invoke(res); Close(); };
            Add(NPanel);
        }
    }

    public class UICallNeighborPanel : UIContainer
    {
        public Dictionary<short, List<short>> NeighborsByFamilyID = new Dictionary<short, List<short>>();
        public UITouchStringList FamilyList;
        public UITouchStringList NeighbourList;
        public UIAvatarSelectButton Icon;
        public UIBigButton CallButton;
        public int SelectedFamily = -1;
        public short SelectedNeighbour = -1;
        public VM VM;

        public event Action<int> OnResult;

        public UICallNeighborPanel(short callerNID, VM vm)
        {
            VM = vm;
            var nb = Content.Get().Neighborhood;
            var neigh = nb.GetNeighborByID(callerNID);
            var rels = neigh?.Relationships?.Keys.AsEnumerable() ?? Enumerable.Empty<int>();
            //var rels = nb.Neighbors.NeighbourByID.Keys;

            foreach (var to in rels)
            {
                // Relationship keys can outlive a deleted neighbour. Never wrap an
                // invalid key into another Sim's short neighbour ID.
                if (to <= 0 || to > short.MaxValue) continue;
                var tn = nb.GetNeighborByID((short)to);
                var family = GetCallableFamily(tn);
                if (family != 0)
                {
                    List<short> famList = null;
                    if (!NeighborsByFamilyID.TryGetValue(family, out famList))
                    {
                        famList = new List<short>();
                        NeighborsByFamilyID[family] = famList;
                    }

                    famList.Add((short)to);
                }
            }

            FamilyList = new UITouchStringList();
            FamilyList.Size = new Microsoft.Xna.Framework.Vector2(320, 350);
            FamilyList.BackingList = NeighborsByFamilyID.Select(x =>
                nb.GetFamilyString((ushort)x.Key)?.GetString(0) ?? "Unknown family").ToList();
            FamilyList.Refresh();
            FamilyList.OnSelectionChange += FamilyList_OnSelectionChange;
            Add(FamilyList);

            NeighbourList = new UITouchStringList();
            NeighbourList.Size = new Microsoft.Xna.Framework.Vector2(320, 350);
            NeighbourList.Position = new Microsoft.Xna.Framework.Vector2(370, 0);
            NeighbourList.OnSelectionChange += NeighbourList_OnSelectionChange;
            Add(NeighbourList);

            var cancelButton = new UIBigButton(false);
            cancelButton.Caption = "Cancel";
            cancelButton.Position = new Microsoft.Xna.Framework.Vector2(370 + 385, 135);
            cancelButton.OnButtonClick += (btn) => { OnResult?.Invoke(-1); };
            cancelButton.Width = 275;
            Add(cancelButton);

            CallButton = new UIBigButton(true);
            CallButton.Caption = "Call";
            CallButton.Position = new Microsoft.Xna.Framework.Vector2(370 + 385, 255);
            CallButton.OnButtonClick += (btn) =>
            {
                if (SelectedFamily >= 0 && SelectedFamily < NeighborsByFamilyID.Count &&
                    SelectedNeighbour > 0 && GetCallableFamily(nb.GetNeighborByID(SelectedNeighbour)) ==
                    NeighborsByFamilyID.ElementAt(SelectedFamily).Key)
                    OnResult?.Invoke(SelectedNeighbour);
                else NeighbourList_OnSelectionChange(-1);
            };
            CallButton.Width = 275;
            Add(CallButton);

            NeighbourList_OnSelectionChange(-1);

            OnResult += (res) => { CallButton.Disabled = true; cancelButton.Disabled = true; };
        }

        private static short GetCallableFamily(Neighbour neighbor)
        {
            var data = neighbor?.PersonData;
            var familyIndex = (int)VMPersonDataVariable.TS1FamilyNumber;
            var genderIndex = (int)VMPersonDataVariable.Gender;
            if (data == null || data.Length <= Math.Max(familyIndex, genderIndex) ||
                data[familyIndex] <= 0 || data[genderIndex] < 0 || data[genderIndex] >= 2) return 0;
            var family = Content.Get().Neighborhood.GetFamily((ushort)data[familyIndex]);
            // Reject stale links to deleted/reused families, as well as pets.
            return family?.FamilyGUIDs?.Contains(neighbor.GUID) == true ? data[familyIndex] : (short)0;
        }

        private void NeighbourList_OnSelectionChange(int obj)
        {
            if (Icon != null) { Remove(Icon); Icon = null; }
            SelectedNeighbour = -1;
            CallButton.Disabled = true;
            if (SelectedFamily < 0 || SelectedFamily >= NeighborsByFamilyID.Count) return;
            var selected = NeighborsByFamilyID.ElementAt(SelectedFamily);
            if (obj < 0 || obj >= selected.Value.Count) return;
            var neighbor = Content.Get().Neighborhood.GetNeighborByID(selected.Value[obj]);
            if (GetCallableFamily(neighbor) != selected.Key) return;

            SelectedNeighbour = selected.Value[obj];
            CallButton.Disabled = false;
            try
            {
                // Render off-lot contacts outside the live simulation. A missing
                // custom appearance must not prevent calling an otherwise valid Sim.
                var head = UIIconCache.GetNeighbor(neighbor.GUID);
                if (head == null) return;
                Icon = new UIAvatarSelectButton(head);
                Icon.Position = new Microsoft.Xna.Framework.Vector2(892, 60);
                Add(Icon);
            }
            catch (Exception error) when (!(error is OutOfMemoryException))
            {
                Console.Error.WriteLine("Phone-book portrait unavailable for " + neighbor.GUID + ": " + error.Message);
            }
        }

        private static string GetContactName(Neighbour neighbor)
        {
            if (neighbor == null) return "Unknown";
            var stored = neighbor.Name;
            if (!string.IsNullOrWhiteSpace(stored) &&
                !(stored.StartsWith("user", StringComparison.OrdinalIgnoreCase) &&
                  stored.Length > 4 && stored.Skip(4).All(char.IsDigit))) return stored;

            var obj = Content.Get().WorldObjects.Get(neighbor.GUID);
            if (obj == null) return "Unknown";
            var title = obj.Resource.Get<CTSS>(obj.OBJ.CatalogStringsID)?.GetString(0);
            return !string.IsNullOrWhiteSpace(title) ? title : obj.OBJ.ChunkLabel ?? "Unknown";
        }

        private void FamilyList_OnSelectionChange(int obj)
        {
            var nb = Content.Get().Neighborhood;
            //populate the rightmost list with the selected family
            SelectedFamily = (obj >= 0 && obj < NeighborsByFamilyID.Count) ? obj : -1;
            NeighbourList_OnSelectionChange(-1);
            NeighbourList.BackingList = new List<string>();
            if (SelectedFamily >= 0)
            {
                var people = NeighborsByFamilyID.ElementAt(SelectedFamily).Value;
                NeighbourList.BackingList = 
                    people.Select(x => GetContactName(nb.GetNeighborByID(x))).ToList();
            }
            NeighbourList.Refresh();
            NeighbourList_OnSelectionChange(-1);
        }
    }
}
