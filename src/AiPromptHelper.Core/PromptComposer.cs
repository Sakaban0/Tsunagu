namespace AiPromptHelper.Core;

public static class PromptComposer
{
    public const string StyleHeading = "【回答スタイル】";
    public const string ConsultationHeading = "【相談内容】";
    public const string Closing = "指示が競合した場合は、初心者にとっての分かりやすさを優先するように。";
    public static string StyleText(IEnumerable<ResponseStyle> styles) => StyleHeading + "\n" + string.Join("\n", styles.Select(s => $"{s.Label}：{s.PromptText}"));
    public static string Compose(Template template, IDictionary<string,string[]> answers, Settings settings)
    {
        var lines = Engine.Visible(template, answers).Select(q =>
        {
            var values = answers.TryGetValue(q.Id, out var a) ? a : [];
            var text = values.Length == 0 ? "スキップ（わからない）" : string.Join("、", values.Select(v => v=="__skip__" ? "スキップ（わからない）" : Engine.IsCustomAnswer(v) ? Engine.CustomText(v) : q.Options.FirstOrDefault(o => o.Value == v)?.Label ?? v));
            return $"{q.Label}\n回答：{text}";
        });
        var instruction = Engine.Generate(template, answers).Replace("必要な情報が不足している場合は確認の質問をしてください。未回答の項目は推測で埋めないでください。", "").Trim();
        var body = $"{ConsultationHeading}\n{string.Join("\n\n", lines)}\n\n{instruction}";
        return template.UseAnswerStyle ? $"{StyleText(settings.ResponseStyles)}\n\n{body}\n\n{Closing}" : body;
    }
    // Replace only the generated prefix; preserve the user's consultation/body edits.
    public static string UpdateStyle(string prompt, Settings settings)
    {
        if (prompt.StartsWith(StyleHeading, StringComparison.Ordinal))
        {
            int end = prompt.IndexOf(ConsultationHeading, StringComparison.Ordinal);
            if (end >= 0) return StyleText(settings.ResponseStyles) + "\n\n" + prompt[end..];
        }
        return StyleText(settings.ResponseStyles) + "\n\n" + ConsultationHeading + "\n" + prompt;
    }
}



