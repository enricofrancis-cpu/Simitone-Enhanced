using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Simitone.Client.UI.Screens;
using Simitone.Client.UI.Model;
using Simitone.Client.UI.Controls;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client;
using Simitone.Client.Superstar;
using System.Globalization;
using Simitone.Client.UI.Panels;
using FSO.Client.UI.Framework;

namespace Simitone.Client.UI.Panels.LiveSubpanels
{
    public class UIJobSubpanel : UISubpanel
    {
        public UILabel PerformanceTitle;
        public UIMotiveBar PerformanceBar;
        public UILabel JobTitle;
        public UILabel SalaryTitle;
        public UITwoStateButton CareerButton;

        private JobLevel LastJobLevel;

        private UISkillDisplay[] Skills;
        private string[] SkillNames = new string[]
        {
            "Cooking",
            "Mechanical",
            "Charisma",
            "Body",
            "Logic",
            "Creativity"
        };

        private VMPersonDataVariable[] SkillInd = new VMPersonDataVariable[]
        {
            VMPersonDataVariable.CookingSkill,
            VMPersonDataVariable.MechanicalSkill,
            VMPersonDataVariable.CharismaSkill,
            VMPersonDataVariable.BodySkill,
            VMPersonDataVariable.LogicSkill,
            VMPersonDataVariable.CreativitySkill
        };

        public UIJobSubpanel(TS1GameScreen game) : base(game)
        {
            PerformanceTitle = new UILabel();
            PerformanceTitle.Caption = "Performance";
            PerformanceTitle.Position = new Vector2(79, 16);
            InitLabel(PerformanceTitle);

            PerformanceBar = new UIMotiveBar();
            PerformanceBar.Position = new Vector2(79, 41);
            DynamicOverlay.Add(PerformanceBar);

            JobTitle = new UILabel();
            JobTitle.Caption = "Subway Musician";
            JobTitle.Position = new Vector2(18, 71);
            InitLabel(JobTitle);

            SalaryTitle = new UILabel();
            SalaryTitle.Caption = "Salary: §90";
            SalaryTitle.Position = new Vector2(18, 94);
            InitLabel(SalaryTitle);
            SalaryTitle.CaptionStyle.Color = UIStyle.Current.BtnActive;

            CareerButton = new UITwoStateButton(Content.Get().CustomUI.Get("blank_blue.png").Get(GameFacade.GraphicsDevice));
            CareerButton.Position = new Vector2(20, 15);
            CareerButton.Caption = "Test";
            CareerButton.Tooltip = "Superstar preview tools";
            CareerButton.Visible = SuperstarRuntime.PreviewToolsEnabled;
            CareerButton.OnButtonClick += (b) => ShowPreviewTools();
            Add(CareerButton);

            Skills = new UISkillDisplay[6];
            for (int i=0; i<6; i++)
            {
                Skills[i] = new UISkillDisplay();
                Skills[i].Position = new Vector2(334 + (i%3)*140, 35 + 60*(i/3));
                Add(Skills[i]);

                var name = new UILabel();
                name.Caption = SkillNames[i];
                name.Position = new Vector2(332 + (i % 3) * 140, 11 + 60 * (i / 3));
                InitLabel(name);
            }
        }

        public int LastPerformance;

        private void ShowPreviewTools()
        {
            var avatar = Game.SelectedAvatar;
            if (avatar == null || !SuperstarRuntime.PreviewToolsEnabled) return;
            UIMobileAlert alert = null;
            alert = new UIMobileAlert(new UIAlertOptions
            {
                Title = "Superstar preview tools",
                Message = "Changes only the selected Sim in this test profile. Next half-star bypasses normal requirements and ends any ordinary career. Max skills raises all six skills. For normal promotion testing, earn fame through gigs instead.",
                Buttons = new[] {
                    new UIAlertButton(UIAlertButtonType.OK, (b) => { SuperstarRuntime.PreviewAdvance(avatar); alert.Close(); }, "Next half-star"),
                    new UIAlertButton(UIAlertButtonType.OK, (b) => { SuperstarRuntime.PreviewSkills(avatar); alert.Close(); }, "Max skills"),
                    new UIAlertButton(UIAlertButtonType.OK, (b) => { SuperstarRuntime.PreviewReset(avatar); alert.Close(); }, "Reset to enrolled"),
                    new UIAlertButton(UIAlertButtonType.Cancel, (b) => alert.Close(), "Close")
                }
            });
            UIScreen.GlobalShowDialog(alert, true);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (Opacity < 1)
            {
                if (DynamicOverlay.GetChildren().Count > 0)
                {
                    DynamicOverlay.Remove(PerformanceBar);
                    Add(PerformanceBar);
                }
            } else
            {
                if (DynamicOverlay.GetChildren().Count == 0)
                {
                    Remove(PerformanceBar);
                    DynamicOverlay.Add(PerformanceBar);
                }
            }
            var sel = Game.SelectedAvatar;
            if (sel == null) return;
            var type = sel.GetPersonData(FSO.SimAntics.Model.VMPersonDataVariable.JobType);
            var level = sel.GetPersonData(FSO.SimAntics.Model.VMPersonDataVariable.JobPromotionLevel);
            var performance = sel.GetPersonData(FSO.SimAntics.Model.VMPersonDataVariable.JobPerformance);

            for (int i = 0; i < 6; i++)
            {
                Skills[i].Value = sel.GetPersonData(SkillInd[i]) / 100;
                Skills[i].Needed = 0;
            }
            if (type == 0 && sel.GetPersonData(VMPersonDataVariable.TS1FameScore) > 0)
            {
                var rules = SuperstarRuntime.Rules;
                int rank = Math.Max(0, Math.Min(FameRules.MaximumRank, (int)sel.GetPersonData(VMPersonDataVariable.TS1FameStarPower)));
                int score = sel.GetPersonData(VMPersonDataVariable.TS1FameScore);
                PerformanceTitle.Caption = "Fame (preview)";
                PerformanceTitle.Visible = true;
                PerformanceBar.Visible = rules != null;
                JobTitle.Caption = rules == null ? "Superstar unavailable" : rules.Titles[rank] + " (" +
                    (rank / 2.0).ToString("0.#", CultureInfo.InvariantCulture) + " stars)";
                if (rules == null) SalaryTitle.Caption = "Superstar game data could not be loaded.";
                else
                {
                    var next = rank < FameRules.MaximumRank ? rules.Requirements[rank] : null;
                    int friends = SuperstarRuntime.FriendHalfStars(sel, Game.vm);
                    SalaryTitle.Caption = "Friends: " + (friends / 2.0).ToString("0.#", CultureInfo.InvariantCulture) +
                        (next == null ? " stars" : " / " + (next.FriendHalfStars / 2.0).ToString("0.#", CultureInfo.InvariantCulture) + " stars");
                    int low = rank == 0 ? 1 : rules.Requirements[rank - 1].Score;
                    double fraction = next == null ? 1 : Math.Max(0, Math.Min(1, (score - low) / (double)Math.Max(1, next.Score - low)));
                    PerformanceBar.MotiveValue = (int)(fraction * 200 - 100);
                    for (int i = 0; i < 6; i++) Skills[i].Needed = next?.Skills[i] ?? 0;
                }
                LastJobLevel = null;
                LastPerformance = int.MinValue;
                return;
            }
            PerformanceTitle.Caption = "Performance";

            var job = Content.Get().Jobs.GetJob((ushort)type);
            if (job?.JobLevels == null || job.JobLevels.Length == 0)
            {
                if (LastPerformance != -200)
                {
                    JobTitle.Caption = "Unemployed";
                    SalaryTitle.Caption = "";
                    PerformanceBar.Visible = false;
                    PerformanceTitle.Visible = false;

                    LastJobLevel = null;
                    LastPerformance = -200;
                }
            }
            else
            {
                var myLevel = job.JobLevels[Math.Max(0, Math.Min(job.JobLevels.Length - 1, level))];
                PerformanceBar.Visible = true;
                PerformanceTitle.Visible = true;

                if (myLevel != LastJobLevel)
                {
                    JobTitle.Caption = myLevel.JobName;
                    SalaryTitle.Caption = "Salary: §" + myLevel.Salary + " (" + ToTime(myLevel.StartTime) + "-" + ToTime(myLevel.EndTime) + ")";

                    LastJobLevel = myLevel;
                }

                if (LastPerformance != performance)
                {
                    PerformanceBar.Visible = true;
                    PerformanceTitle.Visible = true;
                    PerformanceBar.MotiveValue = performance;
                    LastPerformance = performance;
                }
                for (int i = 0; i < 6; i++)
                    Skills[i].Needed = myLevel.MinRequired[i + 1] / 100;
            }

            for (int i = 0; i < 6; i++)
            {
                Skills[i].Value = sel.GetPersonData(SkillInd[i]) / 100;
            }
        }

        private string ToTime(int time)
        {
            return ((time > 12) ? (time - 12) : time) + ((time >= 12) ? "pm" : "am");
        }

        private void InitLabel(UILabel label)
        {
            label.CaptionStyle = label.CaptionStyle.Clone();
            label.CaptionStyle.Color = UIStyle.Current.Text;
            label.CaptionStyle.Size = 15;
            Add(label);
        }
    }
}
