using System.Globalization;
using System.Text.RegularExpressions;
namespace AiPromptHelper.Core;

public static partial class Engine
{
    const string CustomPrefix = "__custom_answer__:";
    public static string CustomAnswer(string text) => CustomPrefix + text;
    public static bool IsCustomAnswer(string value) => value.StartsWith(CustomPrefix, StringComparison.Ordinal);
    public static string CustomText(string value) => value[CustomPrefix.Length..];
    public static readonly string[] Types = ["singleSelect", "multiSelect", "text", "textarea", "number", "slider"];
    [GeneratedRegex(@"\{\{([^{}]*)\}\}")] private static partial Regex Tokens();
    public static List<string> Validate(Template t)
    {
        var errors = new List<string>();
        if (t.SchemaVersion != 1)
            errors.Add("schemaVersion: 対応している値は1です。");
        if (!Regex.IsMatch(t.Id ?? "", @"^[a-zA-Z0-9_-]+$"))
            errors.Add("id: 英数字・ハイフン・下線で指定してください。");
        if (string.IsNullOrWhiteSpace(t.Name) || string.IsNullOrWhiteSpace(t.Category) || string.IsNullOrWhiteSpace(t.Prompt))
            errors.Add("name/category/prompt: 必須です。");
        if (t.Questions is null || t.Questions.Count == 0)
        {
            errors.Add("questions: 質問が必要です。");
            return errors;
        }
        var ids = new HashSet<string>();
        foreach (var q in t.Questions)
        {
            if (q is null)
            {
                errors.Add("questions: nullは使えません。");
                continue;
            }
            if (!Regex.IsMatch(q.Id ?? "", @"^[a-zA-Z0-9_-]+$") || !ids.Add(q.Id!))
                errors.Add("questions.id: 不正または重複しています。");
        }
        foreach (var q in t.Questions.Where(x => x != null))
        {
            var p = $"質問 {q.Id}: ";
            if (!Types.Contains(q.Type) || string.IsNullOrWhiteSpace(q.Label))
                errors.Add(p + "種類または質問文が不正です。");
            if (q.Min > q.Max || q.MaxLength < 1 || q.Step <= 0 || new[] { q.Min, q.Max, q.Step }.Any(x => x.HasValue && !double.IsFinite(x.Value)))
                errors.Add(p + "範囲・文字数・刻みが不正です。");
            if (q.Type == "slider" && (q.Min is null || q.Max is null || q.Step is null))
                errors.Add(p + "スライダーにはmin/max/stepが必要です。");
            if (q.Options is null)
                errors.Add(p + "options: nullは使えません。");
            if (q.Type is "singleSelect" or "multiSelect")
            {
                if(q.Options?.Any(o => o != null && o.Value != null && IsCustomAnswer(o.Value)) == true)
                    errors.Add(p + "選択肢のvalueに予約済みの接頭辞は使えません。");
                if (q.Options is null || q.Options.Count == 0 || q.Options.Any(x => x == null || string.IsNullOrWhiteSpace(x.Value) || string.IsNullOrWhiteSpace(x.Label)) || q.Options.Select(x => x?.Value).Distinct().Count() != q.Options.Count)
                    errors.Add(p + "選択肢が空または重複しています。");
            }
            Check(q.VisibleWhen, p, ids, errors);
            if (q.Default is { } d && !errors.Any())
            {
                if (d.ValueKind == System.Text.Json.JsonValueKind.Object || (q.Type != "multiSelect" && d.ValueKind == System.Text.Json.JsonValueKind.Array))
                {
                    errors.Add(p + "default: 回答の型が不正です。");
                    continue;
                }
                var a = DefaultAnswer(q);
                var e = AnswerError(q, a);
                if (e != null)
                    errors.Add(p + "default: " + e);
            }
        }
        var visiting = new HashSet<string>();
        var done = new HashSet<string>();
        bool Cycle(string id)
        {
            if (visiting.Contains(id))
                return true;
            if (!done.Add(id))
                return false;
            visiting.Add(id);
            var q = t.Questions.FirstOrDefault(x => x?.Id == id);
            bool c = q != null && References(q.VisibleWhen).Any(r => ids.Contains(r) && Cycle(r));
            visiting.Remove(id);
            return c;
        }
        if (ids.Any(Cycle))
            errors.Add("visibleWhen: 循環参照があります。");
        var remainder = Tokens().Replace(t.Prompt ?? "", "");
        if (remainder.Contains("{{") || remainder.Contains("}}"))
            errors.Add("prompt: 置換記号のかっこが不正です。");
        foreach (Match m in Tokens().Matches(t.Prompt ?? ""))
        if (!ids.Contains(m.Groups[1].Value))
            errors.Add("prompt: 存在しないID " + m.Groups[1].Value);
        return errors;
    }
    static void Check(Condition? c, string p, HashSet<string> ids, List<string> e)
    {
        if (c == null)
            return;
        int forms = (c.And != null ? 1 : 0) + (c.Or != null ? 1 : 0) + (c.QuestionId != null ? 1 : 0);
        if (forms != 1)
        {
            e.Add(p + "条件は単一条件、AND、ORのいずれかです。");
            return;
        }
        if (c.And != null || c.Or != null)
        {
            var list = c.And ?? c.Or!;
            if (c.Operator != null || c.Value != null)
                e.Add(p + "複合条件にoperator/valueは指定できません。");
            if (list.Count == 0)
                e.Add(p + "複合条件が空です。");
            foreach (var x in list)
            {
                if (x == null)
                    e.Add(p + "条件がnullです。");
                else
                    Check(x, p, ids, e);
            }
            return;
        }
        if (!ids.Contains(c.QuestionId!))
            e.Add(p + "条件の参照先がありません。");
        if (!new[] { "equals", "notEquals", "contains", "in", "isAnswered" }.Contains(c.Operator))
            e.Add(p + "条件演算子が不正です。");
        if (c.Operator != "isAnswered" && c.Value is null)
            e.Add(p + "条件値が必要です。");
        if (c.Value is { } val && c.Operator != "in" && val.ValueKind is System.Text.Json.JsonValueKind.Array or System.Text.Json.JsonValueKind.Object or System.Text.Json.JsonValueKind.Null)
            e.Add(p + "条件値は文字列または数値にしてください。");
        if (c.Operator == "in" && c.Value?.ValueKind != System.Text.Json.JsonValueKind.Array)
            e.Add(p + "inの値には配列が必要です。");
    }
    static IEnumerable<string> References(Condition? c) => c == null ? [] : c.QuestionId != null ? [c.QuestionId] : (c.And ?? c.Or ?? []).SelectMany(References);
    public static string[] DefaultAnswer(Question q) => q.Default is not { } d ? [] : d.ValueKind == System.Text.Json.JsonValueKind.Array ? d.EnumerateArray().Select(x => x.ToString()).ToArray() : [d.ToString()];
    public static bool Evaluate(Condition? c, Func<string, string[]> get)
    {
        if (c == null)
            return true;
        if (c.And != null)
            return c.And.All(x => Evaluate(x, get));
        if (c.Or != null)
            return c.Or.Any(x => Evaluate(x, get));
        var raw = get(c.QuestionId!).Where(x=>x!="__skip__").ToArray();
        var a = raw.Where(x=>!IsCustomAnswer(x)).ToArray();
        string v = c.Value?.ToString() ?? "";
        return c.Operator switch
        {
            "equals" => a.Contains(v),
            "notEquals" => !a.Contains(v),
            "contains" => a.Any(x => x.Contains(v, StringComparison.Ordinal)),
            "in" => c.Value!.Value.EnumerateArray().Any(x => a.Contains(x.ToString())),
            "isAnswered" => raw.Any(x => !string.IsNullOrWhiteSpace(IsCustomAnswer(x) ? CustomText(x) : x)),
            _ => false
        };
    }
    public static List<Question> Visible(Template t, IDictionary<string, string[]> answers)
    {
        var cache = new Dictionary<string, bool>();
        bool Show(string id)
        {
            if (cache.TryGetValue(id, out var b))
                return b;
            cache[id] = false;
            var q = t.Questions.First(x => x.Id == id);
            return cache[id] = Evaluate(q.VisibleWhen, r => Show(r) ? (answers.TryGetValue(r, out var value) ? value : []) : []);
        }
        return t.Questions.Where(q => Show(q.Id)).ToList();
    }
    public static string? AnswerError(Question q, string[] a)
    {
        if (a.Length == 1 && a[0] == "__skip__") return null;
        if(q.Type is "singleSelect" or "multiSelect")
        {
            var custom=a.Where(IsCustomAnswer).ToArray();
            if(custom.Length>1)return "自由入力の回答は1つにしてください。";
            if(custom.Any(x=>string.IsNullOrWhiteSpace(CustomText(x))))return "「自分で入力する」の回答を入力するか、別の選択肢を選んでください。";
            if(custom.Any(x=>CustomText(x).Length>6000))return "自由入力は6000文字以内で入力してください。";
        }
        if (a.Length == 0 || a.All(string.IsNullOrWhiteSpace))
            return q.Required ? "回答を入力してください。" : null;
        if (q.MaxLength is int n && a.Any(x => (IsCustomAnswer(x) ? CustomText(x) : x).Length > n))
            return $"{n}文字以内で入力してください。";
        if (q.Type is "singleSelect" or "multiSelect")
        {
            if ((q.Type == "singleSelect" && a.Length != 1) || a.Any(x => !IsCustomAnswer(x) && !q.Options.Any(o => o.Value == x)))
                return "選択肢から選んでください。";
        }
        if (q.Type is "number" or "slider")
        {
            if (a.Length != 1 || !double.TryParse(a[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || !double.IsFinite(v))
                return "数値を入力してください。";
            if (v < q.Min || v > q.Max)
                return $"{q.Min}〜{q.Max}の範囲で入力してください。";
            if (q.Type == "slider" && q.Step is double s && Math.Abs((v - q.Min!.Value) / s - Math.Round((v - q.Min.Value) / s)) > 0.000001)
                return "指定された刻みで選んでください。";
        }
        return null;
    }
    public static string Generate(Template t, IDictionary<string, string[]> a)
    {
        var visible = Visible(t, a).Select(q => q.Id).ToHashSet();
        return Tokens().Replace(t.Prompt, m => visible.Contains(m.Groups[1].Value) && a.TryGetValue(m.Groups[1].Value, out var v) && v.Any(x => !string.IsNullOrWhiteSpace(x)) ? string.Join("、", v.Select(x=>x=="__skip__"?"スキップ（わからない）":IsCustomAnswer(x)?CustomText(x):x)) : "未回答");
    }
}



