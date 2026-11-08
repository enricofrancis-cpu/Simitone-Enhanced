using FSO.Client.UI.Controls;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Simitone.Client.UI.Panels;

static void Check(bool test, string message) { if (!test) throw new Exception(message); }
foreach (var width in new[] { 640, 1024, 1920 })
foreach (var messageHeight in new[] { 20, 180, 500 })
foreach (var buttons in new[] { 0, 1, 3 })
foreach (var iconHeight in new[] { 0, 256 }) {
    var options = new UIAlertOptions { Message=messageHeight.ToString(), TextEntry=true,
        Buttons=Enumerable.Range(0, buttons).Select(i => new UIAlertButton { Type=(UIAlertButtonType)i }).ToArray() };
    var dialog = new UIMobileAlert(options) { Width=width };
    typeof(UIMobileAlert).GetField("IconSpace", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(dialog, new Vector2(iconHeight, iconHeight));
    dialog.RefreshSize();
    var input = dialog.Children.OfType<UITextBox>().Single();
    Check(input.Width > 0 && input.X >= 0 && input.X + input.Width <= width, "input outside dialog width");
    Check(input.Y >= 105 + messageHeight && input.Y >= 110 + iconHeight, "input overlaps message/icon");
    Check(input.Y + input.Height <= dialog.Height, "input outside dialog height");
    foreach (var button in dialog.Children.OfType<UIButton>())
        Check(button.Y >= input.Y + input.Height, "input overlaps a button row");
    Check(input.BackgroundSet, "TS1-independent background missing");
    var prior=(input.Position, input.Width, dialog.Height);
    dialog.RefreshSize();
    Check(prior == (input.Position, input.Width, dialog.Height), "layout accumulates height on refresh");
}
UIMobileAlert alert=null!; string response=""; int confirms=0;
alert = new UIMobileAlert(new UIAlertOptions { Message="20", TextEntry=true, MaxChars=32,
    Buttons=new[] {new UIAlertButton { Type=UIAlertButtonType.OK, Handler=_=>{response=alert.ResponseText; confirms++; alert.Close();} }} });
var box=alert.Children.OfType<UITextBox>().Single();
var state=new UpdateState();
alert.Update(state);
Check(state.InputManager.Focus==box && box.MaxChars==32, "focus/name length missing");
state.InputManager.SetFocus(new object());
alert.Update(state);
Check(state.InputManager.Focus!=box, "dialog steals focus repeatedly");
alert.ResponseText="Biscuit";
var confirmButton=alert.Children.OfType<UIButton>().Single();
confirmButton.Disabled=true; box.Enter();
Check(confirms==0, "Enter bypassed disabled confirmation");
confirmButton.Disabled=false; confirmButton.Visible=false; box.Enter();
Check(confirms==0, "Enter bypassed hidden confirmation");
confirmButton.Visible=true;
box.Enter(); box.Enter();
Check(response=="Biscuit" && confirms==1, "Enter must submit current text once while closing");
var noInput = new UIMobileAlert(new UIAlertOptions { Message="20", Buttons=Array.Empty<UIAlertButton>() });
Check(!noInput.Children.OfType<UITextBox>().Any() && noInput.ResponseText==null, "message dialog grew an input");
var buttonless=new UIMobileAlert(new UIAlertOptions { Message="20", TextEntry=true, Buttons=Array.Empty<UIAlertButton>() });
buttonless.Children.OfType<UITextBox>().Single().Enter();
Check(buttonless.ResponseText=="", "buttonless Enter changed the response");
Console.WriteLine("PASS: naming-dialog layout, resizing, focus, background, and Enter response (headless UI doubles; no visual playthrough).");

var clicked = new List<string>();
var actions = new[] { "Next half-star", "Max skills", "Reset to enrolled", "Close" };
var previewTools = new UIMobileAlert(new UIAlertOptions
{
    Message = "20",
    Buttons = actions.Select((text, i) => new UIAlertButton
    {
        Type = i == actions.Length - 1 ? UIAlertButtonType.Cancel : UIAlertButtonType.OK,
        Text = text,
        Handler = _ => clicked.Add(text)
    }).ToArray()
});
var toolButtons = previewTools.Children.OfType<UIButton>().ToArray();
Check(toolButtons.Select(b => b.Caption).SequenceEqual(actions), "Preview tools lost a labelled action");
Check(ReferenceEquals(previewTools.ButtonMap[UIAlertButtonType.OK], toolButtons[0]), "Type lookup must keep the first button");
foreach (var button in toolButtons) button.Click();
Check(clicked.SequenceEqual(actions), "Each preview tool must dispatch its own handler");
Console.WriteLine("PASS: repeated alert button types retain separate labels and click handlers.");
