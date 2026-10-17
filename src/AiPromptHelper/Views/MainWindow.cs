using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using AiPromptHelper.Core;
using AiPromptHelper.ViewModels;
namespace AiPromptHelper.Views;
public sealed class MainWindow : Window
{
    readonly MainViewModel vm;
    readonly TabControl tabs = new();
    readonly TabItem home = new() { Header = "ホーム (_H)" };
    readonly TabItem bookmarks = new() { Header = "ブックマーク (_B)" };
    public MainWindow(MainViewModel model)
    {
        vm = model; Title = "TsunaguChat"; Width = 1100; Height = 850; MinWidth = 720; MinHeight = 540;
        ThemeManager.Setup(this, vm.Settings); ThemeManager.Apply(vm.Settings);
        var root = new DockPanel(); var menu = new Menu();
        var file = new MenuItem { Header = "ファイル (_F)" };
        file.Items.Add(Item("データの場所を開く", () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", vm.Storage.Root) { UseShellExecute = true })));
        file.Items.Add(new Separator()); file.Items.Add(Item("終了 (_X)", Close));
        menu.Items.Add(file); menu.Items.Add(Item("設定 (_S)", Settings)); menu.Items.Add(Item("ヘルプ (_E)", Help));
        DockPanel.SetDock(menu, Dock.Top); root.Children.Add(menu);
        var heading = new StackPanel { Margin = new Thickness(32,24,32,16) };
        var brand = Ui.Text("TsunaguChat",30); brand.FontWeight=FontWeights.SemiBold; heading.Children.Add(brand);
        heading.Children.Add(Ui.Muted("考えを整理して、AIに伝わる言葉へ。")); DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        tabs.Margin=new Thickness(28,0,28,24);tabs.Items.Add(home);tabs.Items.Add(bookmarks);root.Children.Add(tabs);Content=root;
        tabs.SelectionChanged+=(_,e)=>{if(e.Source==tabs&&tabs.SelectedItem==bookmarks)RenderBookmarks();};
        SystemEvents.UserPreferenceChanged+=PreferenceChanged; Closed+=(_,_)=>SystemEvents.UserPreferenceChanged-=PreferenceChanged;
        RenderHome();RenderBookmarks();
        Loaded+=(_,_)=>{if(vm.Storage.Errors.Count>0)MessageBox.Show(this,"一部の保存データを読み込めませんでした。設定の開発タブで詳細を確認してください。","データの確認",MessageBoxButton.OK,MessageBoxImage.Warning);};
    }
    MenuItem Item(string text, Action action) { var item=new MenuItem{Header=text};item.Click+=(_,_)=>Ui.Safe(action);return item; }
    void PreferenceChanged(object sender,UserPreferenceChangedEventArgs e){if(!Dispatcher.HasShutdownStarted)Dispatcher.Invoke(()=>Ui.Safe(()=>ThemeManager.Apply(vm.Settings)));}
    void RenderHome()
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("どんなことを相談しますか？",23));p.Children.Add(Ui.Muted("目的に近いカードを選んでください。答えに迷う質問はスキップできます。"));
        var grid=new Grid { Margin=new Thickness(0,16,0,0) };grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new());
        int i=0; foreach(var t in vm.Templates.Values.Select(x=>x.Template))
        {
            if(i%2==0)grid.RowDefinitions.Add(new(){Height=GridLength.Auto});
            var card=new Button { HorizontalContentAlignment=HorizontalAlignment.Stretch, Margin=new Thickness(6),Padding=new Thickness(18),MinHeight=126 };
            var text=new StackPanel();text.Children.Add(CardIcon.Create(t.Id));var name=Ui.Text(t.Name,21);name.FontWeight=FontWeights.SemiBold;text.Children.Add(name);text.Children.Add(Ui.Muted(t.Description));
            card.Content=text;card.Click+=(_,_)=>Ui.Safe(()=>{vm.Start(t);new WizardWindow(vm,this).ShowDialog();RenderBookmarks();});
            Grid.SetColumn(card,i%2);Grid.SetRow(card,i/2);grid.Children.Add(card);i++;
        }
        if(i==0)p.Children.Add(Ui.Text("テンプレートがありません。設定 → 開発から追加できます。"));p.Children.Add(grid);home.Content=Ui.Scroll(p);
    }
    void RenderBookmarks()
    {
        var p=new StackPanel();p.Children.Add(Ui.Text("また使いたい言葉を、ここに。",23));
        foreach(var b in vm.Bookmarks.Items.OrderByDescending(x=>x.UpdatedAt))
        {
            var inside=new StackPanel();inside.Children.Add(Ui.Text(b.Title,21));inside.Children.Add(Ui.Muted($"{b.TemplateName}  ·  作成 {b.CreatedAt.LocalDateTime:g}"));
            var row=new WrapPanel();row.Children.Add(Ui.Button("開く",()=>{vm.OpenBookmark(b);new WizardWindow(vm,this,b).ShowDialog();RenderBookmarks();},true));
            row.Children.Add(Ui.Button("名前を変更",()=>{var title=Ui.AskTitle(this,b.Title);if(title==null)return;vm.OpenBookmark(b);vm.SaveBookmark(title,b);RenderBookmarks();}));
            row.Children.Add(Ui.Button("削除",()=>{if(Ui.Confirm($"「{b.Title}」のブックマークを解除しますか？")){vm.DeleteBookmark(b);RenderBookmarks();}}));inside.Children.Add(row);p.Children.Add(Ui.Card(inside));
        }
        if(vm.Bookmarks.Items.Count==0)p.Children.Add(Ui.Muted("まだブックマークはありません。プロンプトを作成した後、ブックマークアイコンで保存できます。"));bookmarks.Content=Ui.Scroll(p);
    }
    void Settings(){new SettingsWindow(vm,this).ShowDialog();vm.Reload();RenderHome();RenderBookmarks();}
    void Help()
    {
        var p=new StackPanel{Margin=new Thickness(28)};p.Children.Add(Ui.Text("TsunaguChatの使い方",26));
        p.Children.Add(Ui.Text("1. ホームのカードを選びます。\n2. 最初に回答スタイルを選びます。次回も同じ設定を使えます。\n3. 質問に1つずつ回答し、最後に任意の補足を入力します。迷ったときは「スキップ（わからない）」を選べます。\n4. 「プロンプトを作成」で文章を確認し、必要なら編集します。\n5. 「コピー」を押し、AIの入力欄にCtrl+Vで貼り付けます。\n\n回答スタイル\n確認画面の「回答スタイルを変更」を開くと調整できます。本文を手直ししていても、回答スタイルの部分だけがすぐに変わります。\n\nブックマーク\nしおりの形のボタンで保存します。塗りつぶしは保存済みです。解除時は確認が出ます。保存済み文章の編集は終了時に保存するか確認します。ホームのブックマークタブから名前の変更もできます。\n\nAIで使う\nコピーした文章を、お使いのAIの入力欄へ貼り付けて利用してください。\n\nキーボード\nTab / Shift+Tab：項目を移動。矢印：選択肢・スライダー。Space：選択・実行。Ctrl+Enter：次の質問／プロンプト作成。Alt+Left：前の質問。Esc：ダイアログを閉じる。Ctrl+C：選択した文章をコピー。Ctrl+V：貼り付け。Ctrl+Enterで送信はしません。\n\n用語\nエクスプローラー：ファイルを探すWindowsのアプリ。\nフォルダー：ファイルをまとめる入れ物。\nファイル：文章や画像などを保存したもの。\nクリップボード：コピーした内容を一時的に置く場所。"));
        var w=Ui.Dialog(this,"ヘルプ",Ui.Scroll(p));p.Children.Add(Ui.Button("閉じる",()=>w.Close()));w.ShowDialog();
    }
    internal static Func<string[]> AnswerControl(Panel p,Question q,string[] a)=>AnswerFields.Create(p,q,a);
}



