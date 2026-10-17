using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
namespace AiPromptHelper.Views;

public static class Ui
{
    public static TextBlock Text(string text, double? size = null)
    {
        var t = new TextBlock { Text = text };
        if (size.HasValue)
            t.SetBinding(TextBlock.FontSizeProperty,new System.Windows.Data.Binding("FontSize") { RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor,typeof(Window),1),Converter=new FontRatioConverter(),ConverterParameter=size.Value/16d });
        return t;
    }
    public static Button Button(string text, Action action, bool primary = false)
    {
        var b = new Button { Content = text };
        if (primary) b.SetResourceReference(FrameworkElement.StyleProperty, "PrimaryButton");
        b.Click += (_, _) => Safe(action);
        return b;
    }
    public static void Safe(Action a)
    {
        try
        {
            a();
        }
        catch (Exception ex) { MessageBox.Show("操作を完了できませんでした。内容は画面に保持されています。\n" + ex.Message, "確認", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    public static bool Confirm(string text) => MessageBox.Show(text, "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    public static TextBox Box(string value, string name, bool multiline = false)
    {
        var b = new TextBox { Text = value, AcceptsReturn = multiline, TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        AutomationProperties.SetName(b, name);
        if (multiline)
            b.MinHeight = 110;
        return b;
    }
    public static ComboBox Combo(IEnumerable<string> items, string selected, string name)
    {
        var c = new ComboBox { ItemsSource = items.ToArray(), SelectedItem = selected };
        AutomationProperties.SetName(c, name);
        return c;
    }
    public static void Field(Panel p, string name, UIElement element)
    {
        p.Children.Add(Text(name));
        p.Children.Add(element);
    }
    public static ScrollViewer Scroll(UIElement c) => new() { Content = c, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    public static Border Card(UIElement child) { var b = new Border { Child = child, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(24), Margin = new Thickness(4,8,4,8) }; b.SetResourceReference(Border.BackgroundProperty,"CardBrush"); b.SetResourceReference(Border.BorderBrushProperty,"LineBrush"); return b; }
    public static TextBlock Muted(string text) { var t=Text(text); t.SetResourceReference(TextBlock.ForegroundProperty,"MutedBrush"); return t; }
    public static Window Dialog(Window owner, string title, UIElement body, double width = 800, double height = 700)
    { var w=new Window { Title=title+" — TsunaguChat", Width=width, Height=height, MinWidth=560, MinHeight=420, Content=body }; w.Owner=owner; w.Icon=owner.Icon; w.FontSize=owner.FontSize; w.ShowInTaskbar=false; w.WindowStartupLocation=WindowStartupLocation.CenterOwner; return w; }
    public static string? AskTitle(Window owner, string initial)
    {
        var p=new StackPanel { Margin=new Thickness(28) }; p.Children.Add(Text("ブックマークの名前",22)); var box=Box(initial,"ブックマークの名前"); p.Children.Add(box);
        var w=Dialog(owner,"ブックマーク",p,580,300); w.MinHeight=260; string? result=null;
        var row=new WrapPanel { HorizontalAlignment=HorizontalAlignment.Right }; row.Children.Add(Button("キャンセル",()=>w.Close())); row.Children.Add(Button("保存",()=>{if(string.IsNullOrWhiteSpace(box.Text))throw new InvalidDataException("名前を入力してください。");result=box.Text.Trim();w.Close();},true));p.Children.Add(row);w.Loaded+=(_,_)=>{box.Focus();box.SelectAll();}; w.ShowDialog();return result;
    }
}

