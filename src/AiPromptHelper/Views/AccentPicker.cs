using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;
using AiPromptHelper.Core;
namespace AiPromptHelper.Views;
public sealed class AccentPicker:StackPanel
{
    readonly Slider hue=new(){Minimum=0,Maximum=359,SmallChange=1,LargeChange=10};
    readonly Slider saturation=new(){Minimum=0,Maximum=100,SmallChange=1,LargeChange=10};
    readonly Slider brightness=new(){Minimum=0,Maximum=100,SmallChange=1,LargeChange=10};
    readonly TextBox hex;readonly Border sample=new(){Height=62,CornerRadius=new(7),Margin=new(0,8,0,8)};
    readonly TextBlock sampleText=Ui.Text("選択中の色  ·  Aa あいう");
    readonly Border saturationRamp=new(){Height=8,CornerRadius=new(4)},valueRamp=new(){Height=8,CornerRadius=new(4)};
    bool updating;
    public AccentPicker(string color,Action<string> apply)
    {
        sampleText.HorizontalAlignment=HorizontalAlignment.Center;sampleText.VerticalAlignment=VerticalAlignment.Center;sample.Child=sampleText;Children.Add(sample);
        Add("色相",hue,new LinearGradientBrush(new GradientStopCollection{new(Colors.Red,0),new(Colors.Yellow,.167),new(Colors.Lime,.333),new(Colors.Cyan,.5),new(Colors.Blue,.667),new(Colors.Magenta,.833),new(Colors.Red,1)},0));
        Add("彩度",saturation,null,saturationRamp);Add("明度",brightness,null,valueRamp);
        hex=Ui.Box(color,"HEXカラーコード");hex.Tag="#438E83";Ui.Field(this,"HEX（直接入力もできます）",hex);
        var swatches=new WrapPanel();foreach(var preset in new[]{"#438E83","#587D9A","#827399","#9B735C"}){var b=Ui.Button(preset,()=>FromHex(preset));b.Content=new Border{Width=30,Height=18,CornerRadius=new(3),Background=Brush(preset)};b.Padding=new(10);b.ToolTip=preset+"を選択";AutomationProperties.SetName(b,preset+"を選択");swatches.Children.Add(b);}Children.Add(swatches);
        Children.Add(Ui.Button("この色を適用",()=>{if(!Valid(hex.Text))throw new InvalidDataException("HEXは #438E83 のように6桁で入力してください。");apply(hex.Text.ToUpperInvariant());},true));
        hue.ValueChanged+=(_,_)=>FromSliders();saturation.ValueChanged+=(_,_)=>FromSliders();brightness.ValueChanged+=(_,_)=>FromSliders();hex.TextChanged+=(_,_)=>{if(!updating&&Valid(hex.Text))FromHex(hex.Text);};FromHex(color);
    }
    void Add(string label,Slider slider,Brush? brush,Border? ramp=null){var p=new DockPanel();var value=Ui.Muted("");DockPanel.SetDock(value,Dock.Right);p.Children.Add(value);p.Children.Add(Ui.Text(label));Children.Add(p);slider.ValueChanged+=(_,_)=>value.Text=$"{slider.Value:0}";AutomationProperties.SetName(slider,label);Children.Add(ramp??new Border{Height=8,CornerRadius=new(4),Background=brush});Children.Add(slider);}
    static bool Valid(string text)=>System.Text.RegularExpressions.Regex.IsMatch(text,"^#[0-9A-Fa-f]{6}$");
    static SolidColorBrush Brush(string text)=>new((Color)ColorConverter.ConvertFromString(text));
    void FromHex(string text){updating=true;var c=(Color)ColorConverter.ConvertFromString(text);var hsv=Appearance.RgbToHsv(c.R,c.G,c.B);hue.Value=hsv.H;saturation.Value=hsv.S*100;brightness.Value=hsv.V*100;hex.Text=text.ToUpperInvariant();updating=false;Preview(c);}
    void FromSliders(){if(updating)return;var rgb=Appearance.HsvToRgb(hue.Value,saturation.Value/100,brightness.Value/100);var c=Color.FromRgb(rgb.R,rgb.G,rgb.B);updating=true;hex.Text=$"#{c.R:X2}{c.G:X2}{c.B:X2}";updating=false;Preview(c);}
    void Preview(Color c){sample.Background=new SolidColorBrush(c);sampleText.Foreground=Brush(Appearance.ContrastText(c.R,c.G,c.B));var pure=Appearance.HsvToRgb(hue.Value,1,1);saturationRamp.Background=new LinearGradientBrush(Colors.White,Color.FromRgb(pure.R,pure.G,pure.B),0);var bright=Appearance.HsvToRgb(hue.Value,saturation.Value/100,1);valueRamp.Background=new LinearGradientBrush(Colors.Black,Color.FromRgb(bright.R,bright.G,bright.B),0);}
}
