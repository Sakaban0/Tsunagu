using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using AiPromptHelper.Core;
namespace AiPromptHelper.Views;
public static class AnswerFields
{
    public static Func<string[]> Create(Panel panel,Question q,string[] answer)
    {
        if(q.Type=="singleSelect")
        {
            var group=Guid.NewGuid().ToString();var buttons=new List<RadioButton>();
            foreach(var o in q.Options){var b=new RadioButton { Content=Ui.Text(o.Label),Tag=o.Value,IsChecked=answer.Contains(o.Value),GroupName=group };buttons.Add(b);panel.Children.Add(b);}
            var custom=new RadioButton{Content="自分で入力する",GroupName=group,IsChecked=answer.Any(Engine.IsCustomAnswer)};panel.Children.Add(custom);
            var input=CustomInput(panel,q,answer);
            custom.Checked+=(_,_)=>input.Focus();
            input.PreviewMouseLeftButtonDown+=(_,_)=>custom.IsChecked=true;
            input.TextChanged+=(_,_)=>custom.IsChecked=true;
            return ()=>custom.IsChecked==true ? [Engine.CustomAnswer(input.Text)] : buttons.Where(b=>b.IsChecked==true).Select(b=>(string)b.Tag).ToArray();
        }
        if(q.Type=="multiSelect")
        {
            var list=new List<CheckBox>();foreach(var o in q.Options){var b=new CheckBox {Content=Ui.Text(o.Label),Tag=o.Value,IsChecked=answer.Contains(o.Value)};list.Add(b);panel.Children.Add(b);}
            var custom=new CheckBox{Content="自分で入力する",IsChecked=answer.Any(Engine.IsCustomAnswer)};panel.Children.Add(custom);
            var input=CustomInput(panel,q,answer);
            custom.Checked+=(_,_)=>input.Focus();
            input.PreviewMouseLeftButtonDown+=(_,_)=>custom.IsChecked=true;
            input.TextChanged+=(_,_)=>custom.IsChecked=true;
            return ()=>list.Where(b=>b.IsChecked==true).Select(b=>(string)b.Tag).Concat(custom.IsChecked==true ? [Engine.CustomAnswer(input.Text)] : Array.Empty<string>()).ToArray();
        }
        if(q.Type=="slider")
        {
            var enabled=new CheckBox{Content="この値を回答する",IsChecked=answer.Length>0};panel.Children.Add(enabled);
            var slider=new Slider {Minimum=q.Min!.Value,Maximum=q.Max!.Value,TickFrequency=q.Step!.Value,SmallChange=q.Step.Value,LargeChange=q.Step.Value,IsSnapToTickEnabled=true,Value=answer.Length>0&&double.TryParse(answer[0],out var n)?n:q.Min.Value};
            AutomationProperties.SetName(slider,q.Label);var value=Ui.Text(slider.Value.ToString());slider.ValueChanged+=(_,_)=>{enabled.IsChecked=true;value.Text=slider.Value.ToString();};panel.Children.Add(slider);panel.Children.Add(value);
            return ()=>enabled.IsChecked==true?[slider.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)]:[];
        }
        var text=Ui.Box(answer.FirstOrDefault()??"",q.Label,q.Type=="textarea");text.Tag=q.Placeholder;panel.Children.Add(text);return ()=>string.IsNullOrWhiteSpace(text.Text)?[]:[text.Text];
    }
    static TextBox CustomInput(Panel panel,Question q,string[] answer)
    {
        var custom=answer.FirstOrDefault(Engine.IsCustomAnswer);
        var input=Ui.Box(custom==null?"":Engine.CustomText(custom),q.Label+"：自分で入力する",true);
        input.MinHeight=76;input.MaxHeight=180;input.MaxLength=Math.Min(q.MaxLength??6000,6000);
        input.Tag="選択肢に当てはまらない回答を入力してください。";panel.Children.Add(input);return input;
    }
    public static StackPanel Styles(Settings settings,Action changed)
    {
        var p=new StackPanel();
        foreach(var style in settings.ResponseStyles)
        {
            var block=new StackPanel{Margin=new Thickness(0,8,0,6)};
            var heading=new Grid();heading.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});heading.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
            var title=Ui.Text(style.Label);title.FontWeight=FontWeights.SemiBold;heading.Children.Add(title);
            var value=Ui.Text(style.Values[style.SelectedIndex]);value.TextAlignment=TextAlignment.Right;Grid.SetColumn(value,1);heading.Children.Add(value);block.Children.Add(heading);
            var slider=new Slider{Minimum=0,Maximum=style.Values.Count-1,TickFrequency=1,SmallChange=1,LargeChange=1,IsSnapToTickEnabled=true,IsMoveToPointEnabled=true,Value=style.SelectedIndex,MinHeight=32,Margin=new Thickness(0),ToolTip="クリック・ドラッグ、または左右の矢印キーで調整"};AutomationProperties.SetName(slider,style.Label);
            slider.ValueChanged+=(_,_)=>Ui.Safe(()=>{style.SelectedIndex=(int)Math.Round(slider.Value);value.Text=style.Values[style.SelectedIndex];changed();});block.Children.Add(slider);
            var ends=new DockPanel();var high=Ui.Muted(style.HighLabel);high.HorizontalAlignment=HorizontalAlignment.Right;DockPanel.SetDock(high,Dock.Right);ends.Children.Add(high);ends.Children.Add(Ui.Muted(style.LowLabel));block.Children.Add(ends);p.Children.Add(block);
        }
        return p;
    }
}

