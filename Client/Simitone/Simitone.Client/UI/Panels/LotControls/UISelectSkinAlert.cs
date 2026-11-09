using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Content;
using FSO.Content.TS1;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Utils;
using Simitone.Client.UI.Screens;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.Vitaboy;
using Microsoft.Xna.Framework;
using Simitone.Client.UI.Controls;
using Simitone.Client.Utils;
using Simitone.Client.UI.Panels.WorldUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simitone.Client.UI.Panels.LotControls
{
    public class UISelectSkinAlert : UIMobileDialog
    {
        public UISelectSkinPanel NPanel;
        /*
        public short SelectedNeighbour
        {
            get
            {
                return NPanel.SelectedNeighbour;
            }
        }*/

        public event Action<short> OnResult;

        public UISelectSkinAlert(VMAvatar target, string type, VM vm)
        {
            var pet = type == "cat" || type == "dog";
            Caption = pet ? "Adopt a Pet" : "Please Select Outfit";
            SetHeight(600);
            NPanel = new UISelectSkinPanel(target, type, vm);
            NPanel.Position = new Microsoft.Xna.Framework.Vector2((Width - 1030) / 2, 110);
            NPanel.OnResult += (res) => { OnResult?.Invoke(res); Close(); };
            Add(NPanel);
        }

        public override void GameResized()
        {
            base.GameResized();
            NPanel.Position = new Microsoft.Xna.Framework.Vector2((Width - 1030) / 2, 110);
        }
    }

    public class UISelectSkinPanel : UIContainer{
        public FSO.SimAntics.VM vm { get; set; }
        public BasicCamera Camera;

        public uint BaseGUID = 0x7FD96B54; //all of our templates should be of this kind.
        public string CurrentCode;
        private readonly List<short> OutfitIndices = new List<short>();
        
        public VMAvatar[] BodyAvatars;
        public List<string> ActiveBodies;
        public List<string> ActiveHandgroupTex;
        public List<string> ActiveBodyTex;

        public float HeadPosition = -9f;
        public float BodyPosition = -9f;

        public float HeadSpeed = 0f;
        public float BodySpeed = 0f;
        public float XLast = -1f;

        public int HeadPositionLast = 0;
        public int BodyPositionLast = 0;
        public bool Pet;

        public _3DTargetScene Scene;

        public event Action<short> OnResult;
        public UIBigButton OKButton;
        public UIBigButton CancelButton;

        private short ResultInd => OutfitIndices.Count == 0 ? (short)-1
            : OutfitIndices[(int)DirectionUtils.PosMod(Math.Round(BodyPosition + 8), OutfitIndices.Count)];

        public UISelectSkinPanel(VMAvatar target, string type, VM vm) {
            this.vm = vm;
            Pet = target == null;
            if (Pet)
            {
                BaseGUID = type == "cat" ? 0x7BEA0977u : 0x4A70DF92u;
            }
            else
            {
                BaseGUID = target.Object.OBJ.GUID;
                var bodyStrings = target.Object.Resource.Get<FSO.Files.Formats.IFF.Chunks.STR>(target.Object.OBJ.BodyStringID);
                var body = bodyStrings?.GetString(1);
                var separator = body?.IndexOf('_', Math.Min(4, body.Length)) ?? -1;
                CurrentCode = separator > 4 ? body.Substring(4, separator - 4).ToLowerInvariant() : null;
            }
            var outfitType = Pet ? (type == "cat" ? -2 : -1)
                : Array.IndexOf(VMTS1PurchasableOutfitHelper.OutfitTypes, type);
            PopulateOutfits(target, (short)outfitType);
            Camera = new BasicCamera(GameFacade.GraphicsDevice, new Vector3(5, 1, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            Camera.NearPlane = 0.001f;
            Scene = new _3DTargetScene(GameFacade.GraphicsDevice, Camera, new Point(1030, 500), (GlobalSettings.Default.AntiAlias > 0)?8:0);
            Scene.Initialize(GameFacade.Scenes);

            if (OutfitIndices.Count > 0)
            {
                InitializeLot();
                PopulateReal();
            }

            OKButton = new UIBigButton(true);
            OKButton.Caption = GameFacade.Strings.GetString("142", "0");
            OKButton.Position = new Microsoft.Xna.Framework.Vector2((515 + 300)-137, 370);
            OKButton.OnButtonClick += (btn) => { OnResult?.Invoke(ResultInd); };
            OKButton.Width = 275;
            OKButton.Disabled = OutfitIndices.Count == 0;
            Add(OKButton);

            CancelButton = new UIBigButton(true);
            CancelButton.Caption = GameFacade.Strings.GetString("142", "1");
            CancelButton.Position = new Microsoft.Xna.Framework.Vector2((515 - 300) - 137, 370);
            CancelButton.OnButtonClick += (btn) => { OnResult?.Invoke(-1); };
            CancelButton.Width = 275;
            Add(CancelButton);
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            base.PreDraw(batch);
            Camera.Position = new Vector3(10f, 4f, 0);
            Camera.Target = new Vector3(4f, Pet?0f:0.8f, 0);
            GameFacade.GraphicsDevice.RasterizerState = Microsoft.Xna.Framework.Graphics.RasterizerState.CullCounterClockwise;
            Scene.Draw(GameFacade.GraphicsDevice);
            GameFacade.GraphicsDevice.RasterizerState = Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone;
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            DrawLocalTexture(batch, Scene.Target, new Vector2(0, 0));
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (OutfitIndices.Count == 0) return;
            UpdateCarousel(state);


            for (int i = 0; i < 18; i++)
            {
                var relPos = BodyPosition + i - 9;
                relPos = (float)DirectionUtils.PosMod(relPos, 18);
                if (relPos > 9) relPos += 6;
                var angle = (relPos / 24) * (Math.PI * 2);
                
                var pos = new Vector3(0 + 4.5f * (float)Math.Cos(angle), 0, 0 + 4.5f * (float)Math.Sin(angle));
                BodyAvatars[i].Avatar.RotationY = -(float)angle + (float)Math.PI / 2;
                var ang2 = (float)Math.Abs(DirectionUtils.PosMod(angle + Math.PI, Math.PI*2) - Math.PI);
                //if (ang2 > Math.PI * 2) ang2 = 255;
                var ava = BodyAvatars[i].Avatar;
                ava.AmbientLight = Vector4.One * Math.Max(0, Math.Min(1, 2-ang2));
                if (i != DirectionUtils.PosMod(-Math.Round(BodyPosition + 9), 18)) ava.AmbientLight *= 0.5f;
                else ava.AmbientLight *= 1.1f;
                ava.Position = pos;
                ava.Scale = Vector3.One * (Pet ? 1 : 1 / 2f);
            }
        }

        private void PopulateOutfits(VMAvatar target, short outfitType)
        {
            ActiveBodies = new List<string>();
            ActiveBodyTex = new List<string>();
            ActiveHandgroupTex = new List<string>();
            if (!Pet && (outfitType < 0 || string.IsNullOrEmpty(CurrentCode) || CurrentCode.Length < 2)) return;

            // The BHAV consumes an index into the VM catalogue. Keep that index
            // when skipping an outfit which cannot be previewed.
            var outfits = VMTS1PurchasableOutfitHelper.GetValidOutfits(target, outfitType);
            for (int i = 0; i < outfits.Length && i <= short.MaxValue; i++)
            {
                var parts = outfits[i].Item1.Split(new[] { ",BODY=" }, 2, StringSplitOptions.None);
                if (parts.Length != 2 || !TS1CASScreen.CanRenderAppearance(parts[0])) continue;
                OutfitIndices.Add((short)i);
                ActiveBodies.Add(parts[0]);
                ActiveBodyTex.Add(parts[1]);
                ActiveHandgroupTex.Add(outfits[i].Item2);
            }
        }

        private void UpdateCarousel(UpdateState state)
        {
            var frac = 60f / FSOEnvironment.RefreshRate;
            var minSpeed = (Math.PI / 240f) * frac;
            var mult = (float)Math.Pow(0.95, frac);

            var moving = 0;

            if (state.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                var pos = GlobalPoint(state.MouseState.Position.ToVector2());
                if (XLast == -1)
                {
                    if (pos.Y > -100)
                        XLast = state.MouseState.X;
                }
                else
                {
                    if (pos.Y > 0 && pos.Y < 375)
                    {
                        BodySpeed = ((XLast - state.MouseState.X) / 200f) * frac;
                        moving = 1;
                    }
                    XLast = state.MouseState.X;
                }
            }
            else
            {
                XLast = -1;
            }

            BodySpeed = BodySpeed * mult;
            if (Math.Abs(BodySpeed) < minSpeed && moving != 1)
            {
                var targInt = (int)Math.Round(BodyPosition);
                BodySpeed = (float)Math.Max(-minSpeed, Math.Min(minSpeed, targInt - BodyPosition));
            }

            HeadSpeed = HeadSpeed * mult;
            if (Math.Abs(HeadSpeed) < minSpeed && moving != 2)
            {
                var targInt = (int)Math.Round(HeadPosition);
                HeadSpeed = (float)Math.Max(-minSpeed, Math.Min(minSpeed, targInt - HeadPosition));
            }

            HeadPosition += HeadSpeed;
            BodyPosition += BodySpeed;

            var room = 0;// vm.Context.GetRoomAt(LotTilePos.FromBigTile(28, 21, 1));
            foreach (var body in BodyAvatars) body.SetRoom(65534);

            var curbody = (int)BodyPosition;
            if (curbody != BodyPositionLast)
            {
                FSO.HIT.HITVM.Get().PlaySoundEvent(FSO.Client.UI.Model.UISounds.Click);
                int replacePos = (int)DirectionUtils.PosMod((-BodyPositionLast), 18);
                int increment = 1;
                if (curbody < BodyPositionLast) //start adding after last position's compliment position    
                {
                    increment = -1;
                    replacePos = (int)DirectionUtils.PosMod((-BodyPositionLast), 18);
                }
                var total = Math.Abs(curbody - BodyPositionLast);
                for (int i = 0; i < total; i++)
                {
                    if (increment == -1)
                    {
                        SetBody(BodyAvatars[replacePos], BodyPositionLast - 1);
                    }
                    else
                    {
                        SetBody(BodyAvatars[replacePos], BodyPositionLast + 17);
                    }
                    BodyPositionLast += increment;
                    replacePos = ((replacePos - increment) + 18) % 18;

                }
                BodyPositionLast = curbody;
            }
        }

        private void SetBody(VMAvatar body, int i)
        {
            i = (int)DirectionUtils.PosMod(i, ActiveBodies.Count);
            var oft = new Outfit() { TS1AppearanceID = ActiveBodies[i] + ".apr", TS1TextureID = ActiveBodyTex[i] };
            
            if (!Pet)
            {
                var code = CurrentCode[0];
                if (CurrentCode[1] != 'a') code = 'u';
                var hg = ActiveHandgroupTex[i];
                oft.LiteralHandgroup = new HandGroup()
                {
                    TS1HandSet = true,
                    LightSkin = new HandSet()
                    {
                        LeftHand = new Hand()
                        {
                            Idle = new Gesture() { Name = "h" + code + "lo.apr", TexName = "huao" + hg },
                            Pointing = new Gesture() { Name = "h" + code + "lp.apr", TexName = "huap" + hg },
                            Fist = new Gesture() { Name = "h" + code + "lc.apr", TexName = "huac" + hg }
                        },
                        RightHand = new Hand()
                        {
                            Idle = new Gesture() { Name = "h" + code + "ro.apr", TexName = "huao" + hg },
                            Pointing = new Gesture() { Name = "h" + code + "rp.apr", TexName = "huap" + hg },
                            Fist = new Gesture() { Name = "h" + code + "rc.apr", TexName = "huac" + hg }
                        }
                    }
                };
            }

            body.BodyOutfit = new FSO.SimAntics.Model.VMOutfitReference(oft);
        }

        private void PopulateReal()
        {
            int i = 0;
            foreach (var body in BodyAvatars)
            {
                SetBody(body, 17 - i);
                i++;
            }
        }

        #region Lot Content Stuff
        public void CleanupLastWorld()
        {
            if (vm == null) return;

            if (BodyAvatars == null) return;
            foreach (var body in BodyAvatars)
            {
                body.Delete(true, vm.Context);
            }
        }

        public void InitializeLot()
        {
            BodyAvatars = new VMAvatar[18];
            for (int i = 0; i < 18; i++)
            {
                BodyAvatars[i] = (VMAvatar)vm.Context.CreateObjectInstance(BaseGUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true).BaseObject;
                BodyAvatars[i].SetValue(FSO.SimAntics.Model.VMStackObjectVariable.Hidden, 1);
                Scene.Add(BodyAvatars[i].Avatar);
            }
        }
        #endregion
    }
}
