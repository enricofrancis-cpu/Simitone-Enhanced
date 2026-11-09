using FSO.Client;
using FSO.Client.UI.Controls;
using FSO.Content;
using Simitone.Client.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simitone.Client.UI.Controls
{
    public class UIBigButton : UIButton
    {
        public UIBigButton(bool green) : base()
        {
            CaptionStyle = CaptionStyle.Clone();
            CaptionStyle.Size = 30;
            CaptionStyle.Color = (green)?UIStyle.Current.GreenBtnTxt:UIStyle.Current.BtnTxt;
            CaptionStyle.DisabledColor = UIStyle.Current.BtnDisable;
            Texture = green
                ? UIStyle.GetBlueChrome("greenbutton.png", GameFacade.GraphicsDevice)
                : Content.Get().CustomUI.Get("button.png").Get(GameFacade.GraphicsDevice);
        }
    }
}
