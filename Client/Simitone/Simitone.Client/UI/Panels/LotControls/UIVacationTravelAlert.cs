using Microsoft.Xna.Framework;
using Simitone.Client.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simitone.Client.UI.Panels.LotControls
{
    internal sealed class UIVacationTravelAlert : UIMobileDialog
    {
        private readonly UITouchStringList Destinations;
        private readonly UIBigButton Travel;
        private readonly UIBigButton Cancel;

        public UIVacationTravelAlert(IReadOnlyList<(int Lot, string Name)> destinations, Action<int> onTravel)
        {
            Caption = "Vacation Pay Phone";
            SetHeight(460);

            Destinations = new UITouchStringList
            {
                Size = new Vector2(400, 320),
                BackingList = destinations.Select(destination => destination.Name).ToList()
            };
            Destinations.Refresh();
            Add(Destinations);

            Travel = new UIBigButton(true) { Caption = "Travel", Width = 220, Disabled = true };
            Cancel = new UIBigButton(false) { Caption = "Cancel", Width = 220 };
            Add(Travel);
            Add(Cancel);
            int selected = -1;
            Destinations.OnSelectionChange += index =>
            {
                selected = index;
                Travel.Disabled = index < 0 || index >= destinations.Count;
            };
            Travel.OnButtonClick += button =>
            {
                if (Closing || selected < 0 || selected >= destinations.Count) return;
                Close();
                onTravel(destinations[selected].Lot);
            };
            Cancel.OnButtonClick += button => Close();
            Layout();
        }

        public override void GameResized()
        {
            base.GameResized();
            Layout();
        }

        private void Layout()
        {
            if (Destinations == null) return;
            var listWidth = Math.Max(180, Math.Min(400, Width - 275));
            var left = (Width - listWidth - 245) / 2;
            Destinations.Size = new Vector2(listWidth, 320);
            Destinations.Position = new Vector2(left, 100);
            Travel.Position = new Vector2(left + listWidth + 25, 160);
            Cancel.Position = new Vector2(Travel.X, 280);
        }
    }
}
