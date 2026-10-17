using System.Windows;
using System.Windows.Controls;
using AiPromptHelper.Core;
using AiPromptHelper.ViewModels;
using Microsoft.Win32;
namespace AiPromptHelper.Views;

public sealed class TemplateEditor : ContentControl
{
    string notification = ""; readonly MainViewModel vm; readonly Action reload; Template? draft; string? path; string baseline = ""; string raw = ""; bool rawMode; StackPanel body = new();
    public TemplateEditor(MainViewModel model, Action reloaded)
    {
        vm = model;
        reload = reloaded;
        List();
    }
    bool Dirty
    {
        get
        {
            if (draft == null) return false;
            try { return (rawMode ? raw : Json.Write(draft)) != baseline; }
            catch (ArgumentException) { return true; }
        }
    }
    public bool CanLeave()
    {
        if (!Dirty)
            return true;
        if (!Ui.Confirm("編集内容が未保存です。変更を破棄しますか？"))
            return false;
        draft = null;
        List();
        return true;
    }
    void List()
    {
        body = new StackPanel();
        body.Children.Add(Ui.Text("テンプレートを管理", 24));
        body.Children.Add(Ui.Button("新規作成", () => { draft = new Template { Id = "template-" + Guid.NewGuid().ToString("N")[..8], Name = "新しいテンプレート", Category = "自分のテンプレート", Questions = [new Question { Id = "question1", Label = "何を依頼しますか？", Required = true }], Prompt = "{{question1}}\n不足する情報があれば質問してください。" }; path = null; baseline = ""; rawMode = false; Edit(); }));
        body.Children.Add(Ui.Button("JSONをインポート", Import));
        foreach (var entry in vm.Templates.Values)
        {
            var row = new WrapPanel();
            row.Children.Add(Ui.Button(entry.Template.Name + "（" + (entry.Template.Origin == "bundled" ? "同梱" : "ユーザー") + "）", () => { draft = Json.Read<Template>(Json.Write(entry.Template)); path = entry.Path; baseline = Json.Write(draft); rawMode = false; Edit(); }));
            row.Children.Add(Ui.Button("複製", () => { draft = Json.Read<Template>(Json.Write(entry.Template)); draft.Id += "-" + Guid.NewGuid().ToString("N")[..6]; draft.Name += " のコピー"; draft.Origin = "user"; path = null; baseline = ""; rawMode = false; Edit(); }));
            row.Children.Add(Ui.Button("削除", () => { if (Ui.Confirm(entry.Template.Name + "を削除しますか？")) { File.Delete(entry.Path); Refresh(); } }));
            row.Children.Add(Ui.Button("エクスポート", () => Export(entry.Template)));
            body.Children.Add(row);
        }
        body.Children.Add(Ui.Text("読み込み時の問題（ファイル名と原因）", 20));
        foreach (var e in vm.Storage.Errors.Distinct())
            body.Children.Add(Ui.Text(e));
        body.Children.Add(Ui.Button("再読み込み", Refresh));
        Content = Ui.Scroll(body);
    }
    void Refresh()
    {
        reload();
        List();
    }
    void Import()
    {
        var dialog = new OpenFileDialog { Filter = "テンプレートJSON|*.json" };
        if (dialog.ShowDialog() != true)
            return;
        var t = Json.Read<Template>(File.ReadAllText(dialog.FileName));
        Storage.ValidateTemplate(t);
        string? target = null;
        if (vm.Templates.TryGetValue(t.Id, out var existing))
        {
            var choice = MessageBox.Show("同じIDがあります。\nはい：上書き\nいいえ：別IDで追加\nキャンセル：取り消し", "IDの重複", MessageBoxButton.YesNoCancel);
            if (choice == MessageBoxResult.Cancel)
                return;
            if (choice == MessageBoxResult.Yes)
                target = existing.Path;
            else
                t.Id += "-" + Guid.NewGuid().ToString("N")[..8];
        }
        t.Origin = "user";
        vm.Storage.SaveTemplate(t, target);
        Refresh();
    }
    void Export(Template t)
    {
        Storage.ValidateTemplate(t);
        var d = new SaveFileDialog { Filter = "テンプレートJSON|*.json", FileName = t.Id + ".json" };
        if (d.ShowDialog() == true)
            Storage.Atomic(d.FileName, t, Storage.ValidateTemplate);
    }
    TextBox Field(Panel p, string label, string value, Action<string> change, bool multi = false)
    {
        var b = Ui.Box(value, label, multi);
        b.TextChanged += (_, _) => change(b.Text);
        Ui.Field(p, label, b);
        return b;
    }
    void Edit()
    {
        body = new StackPanel();
        var t = draft!;
        body.Children.Add(Ui.Text("テンプレート編集", 24)); body.Children.Add(Ui.Muted(notification));
        var actions = new WrapPanel();
        actions.Children.Add(Ui.Button("保存", () => { var value = Current(); path = vm.Storage.SaveTemplate(value, path); draft = value; baseline = Json.Write(value); raw = baseline; reload(); notification="保存しました。"; Edit(); }));
        actions.Children.Add(Ui.Button(rawMode ? "GUI編集へ" : "JSON直接編集へ", () => { if (rawMode) { draft = Current(); rawMode = false; } else { raw = Json.Write(draft); rawMode = true; } Edit(); }));
        actions.Children.Add(Ui.Button("置換結果をテスト", Test));
        actions.Children.Add(Ui.Button("エクスポート", () => Export(Current())));
        actions.Children.Add(Ui.Button("一覧に戻る", () => { if (CanLeave()) { draft = null; List(); } }));
        body.Children.Add(actions);
        if (rawMode)
        {
            body.Children.Add(Ui.Text("JSONの行・バイト位置と原因は、保存またはGUI切替時に表示します。"));
            var text = Ui.Box(raw, "テンプレートJSON", true);
            text.MinHeight = 500;
            text.TextChanged += (_, _) => raw = text.Text;
            body.Children.Add(text);
            Content = Ui.Scroll(body);
            return;
        }
        Field(body, "ID（英数字・ハイフン・下線）", t.Id, v => t.Id = v);
        Field(body, "名称", t.Name, v => t.Name = v);
        Field(body, "カテゴリ", t.Category, v => t.Category = v);
        var useStyle=new CheckBox{Content="共通の回答スタイルを使う",IsChecked=t.UseAnswerStyle};useStyle.Click+=(_,_)=>t.UseAnswerStyle=useStyle.IsChecked==true;body.Children.Add(useStyle);
        Field(body, "説明", t.Description, v => t.Description = v);
        Field(body, "プロンプト本文（{{質問ID}}で回答を挿入）", t.Prompt, v => t.Prompt = v, true);
        foreach (var q in t.Questions.ToArray())
        {
            var panel = new StackPanel { Margin = new Thickness(8, 20, 8, 8) };
            panel.Children.Add(Ui.Text("質問 " + (t.Questions.IndexOf(q) + 1), 22));
            Field(panel, "質問ID", q.Id, v => q.Id = v);
            Field(panel, "質問文", q.Label, v => q.Label = v);
            var type = Ui.Combo(Engine.Types, q.Type, "入力型");
            Ui.Field(panel, "入力型：singleSelect=単一／multiSelect=複数／text=1行／textarea=複数行／number=数値／slider=スライダー", type);
            type.SelectionChanged += (_, _) => { q.Type = (string)type.SelectedItem; if (q.Type == "slider") { q.Min ??= 0; q.Max ??= 10; q.Step ??= 1; } Edit(); };
            var required = new CheckBox { Content = "必須", IsChecked = q.Required };
            required.Click += (_, _) => q.Required = required.IsChecked == true;
            panel.Children.Add(required);
            Field(panel, "説明", q.HelpText ?? "", v => q.HelpText = v);
            Field(panel, "入力例", q.Placeholder ?? "", v => q.Placeholder = v);
            Field(panel, "既定の回答（複数選択は1行1値・空欄は未回答）", string.Join("\n", Engine.DefaultAnswer(q)), v => q.Default = string.IsNullOrEmpty(v) ? null : Json.Element(q.Type == "multiSelect" ? (object)v.Split('\n').Select(x => x.TrimEnd('\r')).ToArray() : v), q.Type == "multiSelect");
            Numeric(panel, "最小値", q.Min, v => q.Min = v);
            Numeric(panel, "最大値", q.Max, v => q.Max = v);
            Numeric(panel, "スライダーの刻み", q.Step, v => q.Step = v);
            var length = Field(panel, "最大文字数（空欄は制限なし）", q.MaxLength?.ToString() ?? "", v => q.MaxLength = string.IsNullOrWhiteSpace(v) ? null : int.TryParse(v, out var n) ? n : -1);
            if (q.Type is "singleSelect" or "multiSelect")
            {
                panel.Children.Add(Ui.Text("選択肢（「分からない」もここで追加できます）"));
                foreach (var o in q.Options.ToArray())
                {
                    var r = new StackPanel();
                    Field(r, "表示ラベル", o.Label, v => o.Label = v);
                    Field(r, "値", o.Value, v => o.Value = v);
                    r.Children.Add(Ui.Button("選択肢を削除", () => { q.Options.Remove(o); Edit(); }));
                    panel.Children.Add(r);
                }
                panel.Children.Add(Ui.Button("選択肢を追加", () => { q.Options.Add(new Option { Label = "新しい選択肢", Value = "option" + (q.Options.Count + 1) }); Edit(); }));
                panel.Children.Add(Ui.Button("分からないを追加", () => { if (!q.Options.Any(x => x.Value == "unknown")) q.Options.Add(new Option { Label = "分からない", Value = "unknown" }); Edit(); }));
            }
            panel.Children.Add(Ui.Text("表示する条件"));
            ConditionEditor(panel, q.VisibleWhen, c => q.VisibleWhen = c);
            var move = new WrapPanel();
            move.Children.Add(Ui.Button("上へ", () => { int i = t.Questions.IndexOf(q); if (i > 0) { t.Questions.RemoveAt(i); t.Questions.Insert(i - 1, q); Edit(); } }));
            move.Children.Add(Ui.Button("下へ", () => { int i = t.Questions.IndexOf(q); if (i < t.Questions.Count - 1) { t.Questions.RemoveAt(i); t.Questions.Insert(i + 1, q); Edit(); } }));
            move.Children.Add(Ui.Button("質問を削除", () => { if (Ui.Confirm("この質問を削除しますか？")) { t.Questions.Remove(q); Edit(); } }));
            panel.Children.Add(move);
            body.Children.Add(panel);
        }
        body.Children.Add(Ui.Button("質問を追加", () => { var n = 1; while (t.Questions.Any(x => x.Id == "question" + n)) n++; t.Questions.Add(new Question { Id = "question" + n, Label = "新しい質問" }); Edit(); }));
        Content = Ui.Scroll(body);
    }
    void Numeric(Panel p, string label, double? value, Action<double?> change) => Field(p, label, value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", v => change(string.IsNullOrWhiteSpace(v) ? null : double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : double.NaN));
    void ConditionEditor(Panel p, Condition? c, Action<Condition?> set)
    {
        var kind = Ui.Combo(["常に表示", "単一条件", "AND（すべて）", "OR（いずれか）"], c == null ? "常に表示" : c.And != null ? "AND（すべて）" : c.Or != null ? "OR（いずれか）" : "単一条件", "条件の種類");
        p.Children.Add(kind);
        kind.SelectionChanged += (_, _) => { set(kind.SelectedIndex switch { 0 => null, 1 => new Condition { QuestionId = draft!.Questions.First().Id, Operator = "isAnswered" }, 2 => new Condition { And = [new Condition { QuestionId = draft!.Questions.First().Id, Operator = "isAnswered" }] }, _ => new Condition { Or = [new Condition { QuestionId = draft!.Questions.First().Id, Operator = "isAnswered" }] } }); Edit(); };
        if (c == null)
            return;
        if (c.And != null || c.Or != null)
        {
            var list = c.And ?? c.Or!;
            foreach (var child in list.ToArray())
            {
                var nested = new StackPanel { Margin = new Thickness(22, 6, 0, 6) };
                ConditionEditor(nested, child, x => { var i = list.IndexOf(child); if (x == null) list.RemoveAt(i); else list[i] = x; });
                nested.Children.Add(Ui.Button("この条件を削除", () => { list.Remove(child); Edit(); }));
                p.Children.Add(nested);
            }
            p.Children.Add(Ui.Button("条件を追加", () => { list.Add(new Condition { QuestionId = draft!.Questions.First().Id, Operator = "isAnswered" }); Edit(); }));
            return;
        }
        var id = Ui.Combo(draft!.Questions.Select(x => x.Id), c.QuestionId ?? "", "参照する質問ID");
        Ui.Field(p, "参照する質問ID", id);
        id.SelectionChanged += (_, _) => c.QuestionId = (string?)id.SelectedItem;
        var op = Ui.Combo(["equals", "notEquals", "contains", "in", "isAnswered"], c.Operator ?? "isAnswered", "条件演算子");
        Ui.Field(p, "equals=等しい／notEquals=異なる／contains=含む／in=候補内／isAnswered=回答あり", op);
        op.SelectionChanged += (_, _) => { c.Operator = (string)op.SelectedItem; c.Value = c.Operator == "in" ? Json.Element(Array.Empty<string>()) : Json.Element(""); Edit(); };
        if (c.Operator != "isAnswered")
            Field(p, "比較する値（inは1行1候補）", c.Value is { } v && v.ValueKind == System.Text.Json.JsonValueKind.Array ? string.Join("\n", v.EnumerateArray().Select(x => x.ToString())) : c.Value?.ToString() ?? "", v => c.Value = Json.Element(c.Operator == "in" ? (object)v.Split('\n').Select(x => x.TrimEnd('\r')).ToArray() : v), c.Operator == "in");
    }
    Template Current()
    {
        Template t;
        try
        {
            t = rawMode ? Json.Read<Template>(raw) : draft!;
        }
        catch (System.Text.Json.JsonException e) { throw new InvalidDataException($"JSONの{e.LineNumber + 1}行目、位置{e.BytePositionInLine + 1}: {e.Message}"); }
        Storage.ValidateTemplate(t);
        return t;
    }
    void Test()
    {
        var t = Current();
        var w = new Window { Title = "置換結果のテスト", Width = 780, Height = 720, Owner = Window.GetWindow(this) };
        var p = new StackPanel { Margin = new Thickness(16) };
        var answers = t.Questions.ToDictionary(x => x.Id, Engine.DefaultAnswer);
        var output = Ui.Box("", "テスト結果", true);
        var controls = new Dictionary<string, Func<string[]>>();
        var host = new StackPanel();
        void Render()
        {
            host.Children.Clear();
            controls.Clear();
            foreach (var q in Engine.Visible(t, answers))
            {
                host.Children.Add(Ui.Text(q.Label));
                controls[q.Id] = MainWindow.AnswerControl(host, q, answers.GetValueOrDefault(q.Id, []));
            }
        }
        void Update()
        {
            foreach (var pair in controls)
                answers[pair.Key] = pair.Value();
            Render();
        }
        p.Children.Add(Ui.Text("回答を入力して条件を再評価します。非表示の回答は生成文に含みません。"));
        p.Children.Add(host);
        p.Children.Add(Ui.Button("条件を再評価", Update));
        p.Children.Add(Ui.Button("プロンプトを生成", () => { Update(); foreach (var q in Engine.Visible(t, answers)) { var e = Engine.AnswerError(q, answers[q.Id]); if (e != null) throw new InvalidDataException(q.Label + "：" + e); } output.Text = PromptComposer.Compose(t, answers, vm.Settings); }));
        p.Children.Add(output);
        Render();
        w.Content = Ui.Scroll(p);
        w.ShowDialog();
    }
}



