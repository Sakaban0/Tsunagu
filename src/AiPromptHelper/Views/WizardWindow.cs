using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Automation;
using AiPromptHelper.Core;
using AiPromptHelper.ViewModels;

namespace AiPromptHelper.Views;
public sealed class WizardWindow : Window
{
    readonly MainViewModel vm;readonly ContentControl host=new();
    Bookmark? bookmark;Func<string[]>? read;Action? next;Action? previous;string savedText="";
    public WizardWindow(MainViewModel model,Window owner,Bookmark? existing=null)
    {
        vm=model;bookmark=existing;savedText=existing?.PromptText??"";Title=(existing?.Title??vm.Active?.Name??"プロンプト")+" — TsunaguChat";
        Width=850;Height=820;MinWidth=640;MinHeight=540;ThemeManager.Setup(this,vm.Settings,owner);host.Margin=new Thickness(28);Content=host;
        PreviewKeyDown+=(_,e)=>
        {
            if(e.Key==Key.Escape){Close();e.Handled=true;}
            else if(e.Key==Key.Enter&&Keyboard.Modifiers==ModifierKeys.Control&&next!=null){Ui.Safe(next);e.Handled=true;}
            else if(e.Key==Key.Left&&Keyboard.Modifiers==ModifierKeys.Alt&&previous!=null){Ui.Safe(previous);e.Handled=true;}
        };
        Closing+=(_,e)=>
        {

            if(bookmark!=null&&vm.Prompt!=savedText)
            {
                var result=MessageBox.Show(this,"ブックマークの変更を保存しますか？","変更の保存",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);
                if(result==MessageBoxResult.Cancel){e.Cancel=true;return;}
                if(result==MessageBoxResult.Yes)try{bookmark=vm.SaveBookmark(bookmark.Title,bookmark);savedText=vm.Prompt;}catch(Exception ex){e.Cancel=true;MessageBox.Show(this,ex.Message,"保存できませんでした",MessageBoxButton.OK,MessageBoxImage.Error);}
            }
        };
        if(existing==null)StylePage();else Preview();
    }
    void Page(StackPanel panel, UIElement? footer=null)
    {
        var layout=new DockPanel();if(footer!=null){DockPanel.SetDock(footer,Dock.Bottom);layout.Children.Add(footer);}layout.Children.Add(Ui.Scroll(panel));host.Content=layout;
        Dispatcher.BeginInvoke(()=>host.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
    }
    void PersistStyles(){vm.Storage.SaveSettings(vm.Settings);}
    void StylePage()
    {
        if(!vm.UseAnswerStyle){Question();return;}
        previous=null;var p=new StackPanel();p.Children.Add(Ui.Muted("STEP 1  /  回答の受け取り方"));p.Children.Add(Ui.Text("【回答スタイル】",26));p.Children.Add(Ui.Muted("好みの位置に調整してください。設定は次回も使用します。"));p.Children.Add(AnswerFields.Styles(vm.Settings,PersistStyles));
        next=()=>{PersistStyles();vm.Index=0;Question();};var row=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};row.Children.Add(Ui.Button("キャンセル",Close));row.Children.Add(Ui.Button("質問をはじめる",()=>next(),true));Page(p,row);
    }
    void Question()
    {
        var list=vm.Visible;if(list.Count==0){vm.Generate();Preview();return;}vm.Index=Math.Clamp(vm.Index,0,list.Count-1);var q=list[vm.Index];
        var p=new StackPanel();p.Children.Add(Ui.Muted($"質問 {vm.Index+1} / {list.Count}"));var title=Ui.Text(q.Label,25);title.FontWeight=FontWeights.SemiBold;if(q.Required)title.SetResourceReference(TextBlock.ForegroundProperty,"ErrorBrush");p.Children.Add(title);
        if(q.Required){var label=Ui.Text("必須  ·  わからない場合はスキップできます");label.SetResourceReference(TextBlock.ForegroundProperty,"ErrorBrush");p.Children.Add(label);}else p.Children.Add(Ui.Muted("任意"));
        if(!string.IsNullOrWhiteSpace(q.HelpText))p.Children.Add(Ui.Muted(q.HelpText));var fields=new StackPanel();var old=vm.Answers.GetValueOrDefault(q.Id,[]);read=AnswerFields.Create(fields,q,old.Where(x=>x!="__skip__").ToArray());p.Children.Add(Ui.Card(fields));
        if(old.Contains("__skip__"))p.Children.Add(Ui.Muted("この質問はスキップしています。回答する場合は選択・入力してください。"));
        void Move(string[] values){vm.Answers[q.Id]=values;var visible=vm.Visible;var index=visible.FindIndex(x=>x.Id==q.Id);if(index+1<visible.Count){vm.Index=index+1;Question();}else{var invalid=visible.FirstOrDefault(x=>Engine.AnswerError(x,vm.Answers.GetValueOrDefault(x.Id,[]))!=null);if(invalid!=null){vm.Index=visible.IndexOf(invalid);Question();return;}vm.Generate();Preview();}}
        next=()=>{var values=read();if(values.Length==0&&old.Contains("__skip__"))values=["__skip__"];var error=Engine.AnswerError(q,values);if(error!=null)throw new InvalidDataException(error);Move(values);};
        previous=vm.Index==0&&!vm.UseAnswerStyle ? null : ()=>{var values=read();vm.Answers[q.Id]=values.Length==0&&old.Contains("__skip__")?["__skip__"]:values;if(vm.Index==0)StylePage();else{vm.Index--;Question();}};
        var row=new WrapPanel();if(previous!=null)row.Children.Add(Ui.Button("前へ",()=>previous()));row.Children.Add(Ui.Button("スキップ（わからない）",()=>Move(["__skip__"])));row.Children.Add(Ui.Button(vm.Index==list.Count-1?"プロンプトを作成":"次へ",()=>next(),true));var footer=new StackPanel();footer.Children.Add(row);
        footer.Children.Add(Ui.Button("最初からやり直す",()=>{if(Ui.Confirm("これまでの回答を消して、やり直しますか？")){vm.Start(vm.Active!);StylePage();}}));Page(p,footer);
    }
    void Preview()
    {
        next=null;previous=null;var p=new StackPanel();p.Children.Add(Ui.Text("プロンプト",26));p.Children.Add(Ui.Muted("内容を確認し、必要なら文章を整えてからコピーしてください。"));
        var box=Ui.Box(vm.Prompt,"プロンプト全文",true);box.MinHeight=310;box.MaxHeight=520;box.TextChanged+=(_,_)=>vm.Prompt=box.Text;
        if(vm.UseAnswerStyle){
        var styleExpander=new Expander {Header="回答スタイルを変更",IsExpanded=false};
        styleExpander.Content=AnswerFields.Styles(vm.Settings,()=>{vm.RefreshResponseStyle();box.Text=vm.Prompt;PersistStyles();});p.Children.Add(styleExpander);}p.Children.Add(box);
        previous=()=>
        {
            if(vm.Active==null||vm.Visible.Count==0){Close();return;}
            if(vm.Prompt!=PromptComposer.Compose(vm.Active,vm.Answers,vm.Settings)
                &&!Ui.Confirm("質問に戻りますか？ プロンプトを作成し直すと、ここで手直しした文章は回答から作成した内容に置き換わります。"))return;
            vm.Index=vm.Visible.Count-1;Question();
        };
        var notification=Ui.Muted("");var actions=new WrapPanel();
        var back=Ui.Button("戻る",()=>previous());back.ToolTip=vm.Active==null?"ホームへ戻る":"回答を保持して最後の質問へ戻る";actions.Children.Add(back);
        actions.Children.Add(Ui.Button("コピー",()=>{Clipboard.SetText(vm.Prompt);notification.Text="コピーしました。AIの入力欄に Ctrl+V で貼り付けられます。";},true));
        var toggle=new ToggleButton{IsChecked=bookmark!=null,Width=48,Height=46,Padding=new Thickness(9),HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center,ToolTip=bookmark==null?"ブックマークに保存":"ブックマークを解除"};
        var icon=new System.Windows.Shapes.Path{Data=Geometry.Parse("M 3,1 L 19,1 L 19,23 L 11,17 L 3,23 Z"),Width=18,Height=24,Stretch=Stretch.Uniform,StrokeThickness=1.7};icon.SetBinding(Shape.StrokeProperty,new System.Windows.Data.Binding("Foreground"){Source=toggle});
        void UpdateIcon(){icon.Fill=bookmark==null?Brushes.Transparent:toggle.Foreground;toggle.IsChecked=bookmark!=null;toggle.ToolTip=bookmark==null?"ブックマークに保存":"ブックマークを解除";AutomationProperties.SetName(toggle,(string)toggle.ToolTip);}
        var iconCanvas=new Grid{Width=26,Height=28,Margin=new Thickness(2)};icon.HorizontalAlignment=HorizontalAlignment.Center;icon.VerticalAlignment=VerticalAlignment.Center;iconCanvas.Children.Add(icon);toggle.Content=new Viewbox{Width=24,Height=26,Stretch=Stretch.Uniform,Child=iconCanvas};toggle.Click+=(_,_)=>
        {
            try
            {
                if(bookmark!=null){if(Ui.Confirm("このプロンプトのブックマークを解除しますか？")){vm.DeleteBookmark(bookmark);bookmark=null;savedText="";notification.Text="ブックマークを解除しました。";}}
                else{var title=Ui.AskTitle(this,vm.Active?.Name??"プロンプト");if(title!=null){bookmark=vm.SaveBookmark(title);savedText=vm.Prompt;MessageBox.Show(this,"プロンプトを保存しました。","ブックマーク",MessageBoxButton.OK,MessageBoxImage.Information);}}
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message,"保存できませんでした",MessageBoxButton.OK,MessageBoxImage.Error);}
            finally{UpdateIcon();}
        };UpdateIcon();actions.Children.Add(toggle);
        actions.Children.Add(Ui.Button("終了",Close));p.Children.Add(actions);p.Children.Add(notification);Page(p);
    }
}








