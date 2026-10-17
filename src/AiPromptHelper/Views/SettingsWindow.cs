using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AiPromptHelper.Core;
using AiPromptHelper.ViewModels;
namespace AiPromptHelper.Views;
public sealed class SettingsWindow:Window
{
    readonly MainViewModel vm;readonly TabControl tabs=new();readonly TextBlock notice=Ui.Muted("");
    public SettingsWindow(MainViewModel model,Window owner)
    {
        vm=model;Title="設定 — TsunaguChat";Width=920;Height=760;MinWidth=680;MinHeight=540;ThemeManager.Setup(this,vm.Settings,owner);
        var root=new DockPanel{Margin=new Thickness(24)};var bottom=new DockPanel{Margin=new Thickness(0,14,0,0)};var close=Ui.Button("閉じる",Close);DockPanel.SetDock(close,Dock.Right);bottom.Children.Add(close);bottom.Children.Add(notice);DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);root.Children.Add(tabs);Content=root;
        tabs.Items.Add(new TabItem{Header="一般",Content=General()});
        var styles=new StackPanel{Margin=new Thickness(8,12,8,0)};styles.Children.Add(Ui.Text("回答スタイルの定義",22));styles.Children.Add(Ui.Muted("answer_styles.json を編集します。valuesは表示文言、promptValuesは出力文章、defaultIndexは初期位置（0から）です。選択位置は設定ファイルに別途保存します。"));
        var json=Ui.Box(Json.Write(new AnswerStyleFile{Items=vm.Settings.ResponseStyles}),"回答スタイルのJSON",true);json.MinHeight=320;var baseline=json.Text;styles.Children.Add(json);
        styles.Children.Add(Ui.Button("検証してファイルに適用",()=>{var value=Json.Read<AnswerStyleFile>(json.Text);vm.Storage.SaveAnswerStyles(value,vm.Settings);Save();json.Text=Json.Write(new AnswerStyleFile{Items=vm.Settings.ResponseStyles});baseline=json.Text;},true));
        styles.Children.Add(Ui.Button("ファイルから再読み込み",()=>{if(json.Text!=baseline&&!Ui.Confirm("未適用の変更を破棄して読み直しますか？"))return;vm.Storage.LoadAnswerStyles(vm.Settings);if(!vm.Storage.AnswerStylesWritable)throw new InvalidDataException("answer_styles.jsonを読み込めません。元ファイルを保持し、標準定義を使用します。修正後に再起動してください。");json.Text=Json.Write(new AnswerStyleFile{Items=vm.Settings.ResponseStyles});baseline=json.Text;notice.Text="回答スタイルを読み込みました。";}));
        tabs.Items.Add(new TabItem{Header="回答スタイル",Content=Ui.Scroll(styles)});
        var editor=new TemplateEditor(vm,vm.Reload);tabs.Items.Add(new TabItem{Header="開発",Content=editor});
        int last=0;bool reverting=false;
        bool LeaveStyles(){if(json.Text==baseline)return true;if(!Ui.Confirm("回答スタイルJSONの未適用の変更を破棄しますか？"))return false;json.Text=baseline;return true;}
        tabs.SelectionChanged+=(_,e)=>{if(e.Source!=tabs||reverting)return;var selected=tabs.SelectedIndex;if((last==2&&!editor.CanLeave())||(last==1&&!LeaveStyles())){reverting=true;tabs.SelectedIndex=last;reverting=false;}else last=selected;};
        Closing+=(_,e)=>{if(!LeaveStyles()||!editor.CanLeave())e.Cancel=true;};PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){Close();e.Handled=true;}};
    }
    void Save(){vm.Storage.SaveSettings(vm.Settings);ThemeManager.Apply(vm.Settings);notice.Text="設定を保存しました。";}
    static void Row(Grid grid,string label,UIElement input)
    {
        int row=grid.RowDefinitions.Count;grid.RowDefinitions.Add(new(){Height=GridLength.Auto});var text=Ui.Text(label);text.Margin=new Thickness(0,14,24,14);Grid.SetRow(text,row);grid.Children.Add(text);Grid.SetRow(input,row);Grid.SetColumn(input,1);grid.Children.Add(input);
    }
    static Grid Form(){var g=new Grid{Margin=new Thickness(8,16,8,0)};g.ColumnDefinitions.Add(new(){Width=new GridLength(190)});g.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});return g;}
    UIElement General()
    {
        var form=Form();var theme=Ui.Combo(["ライト","ダーク","システムに合わせる"],vm.Settings.Theme switch{"dark"=>"ダーク","system"=>"システムに合わせる",_=>"ライト"},"テーマ");Row(form,"テーマ",theme);theme.SelectionChanged+=(_,_)=>Ui.Safe(()=>{vm.Settings.Theme=theme.SelectedIndex switch{1=>"dark",2=>"system",_=>"light"};Save();});
        Row(form,"アクセントカラー",new AccentPicker(vm.Settings.AccentColor,color=>{var previous=vm.Settings.AccentColor;vm.Settings.AccentColor=color;try{Save();}catch{vm.Settings.AccentColor=previous;throw;}}));
        var fontPanel=new StackPanel();var font=Ui.Box(vm.Settings.FontSizePt.ToString("0.##"),"文字サイズ（pt）");fontPanel.Children.Add(font);fontPanel.Children.Add(Ui.Muted("8〜24 pt。標準は11 pt。見出しも比率を維持して調整します。"));
        fontPanel.Children.Add(Ui.Button("文字サイズを適用",()=>{if(!double.TryParse(font.Text,out var pt)||!double.IsFinite(pt)||pt<8||pt>24)throw new InvalidDataException("文字サイズは8〜24 ptで入力してください。");var previous=vm.Settings.FontSizePt;vm.Settings.FontSizePt=pt;try{Save();}catch{vm.Settings.FontSizePt=previous;throw;}}));Row(form,"文字サイズ（pt）",fontPanel);
        Row(form,"保存データ",Ui.Button("データの場所を開く",()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe",vm.Storage.Root){UseShellExecute=true})));return Ui.Scroll(form);
    }
}
