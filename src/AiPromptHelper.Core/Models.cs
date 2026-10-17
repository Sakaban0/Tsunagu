using System.Text.Json;
using System.Text.Json.Serialization;
namespace AiPromptHelper.Core;

public sealed class Template
{
    public bool UseAnswerStyle { get; set; } = true;
    [JsonRequired] public int SchemaVersion { get; set; } = 1; public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Description { get; set; } = ""; public string Category { get; set; } = ""; public string Origin { get; set; } = "user"; public List<Question> Questions { get; set; } = []; public string Prompt { get; set; } = "";
}
public sealed class Question
{
    public string Id { get; set; } = ""; public string Label { get; set; } = ""; public string Type { get; set; } = "text"; public bool Required
    {
        get; set;
    }
    public string? Placeholder
    {
        get; set;
    }
    public string? HelpText
    {
        get; set;
    }
    public JsonElement? Default
    {
        get; set;
    }
    public double? Min
    {
        get; set;
    }
    public double? Max
    {
        get; set;
    }
    public double? Step
    {
        get; set;
    }
    public int? MaxLength
    {
        get; set;
    }
    public List<Option> Options { get; set; } = []; public Condition? VisibleWhen
    {
        get; set;
    }
}
public sealed class Option
{
    public string Value { get; set; } = ""; public string Label { get; set; } = "";
}
public sealed class Condition
{
    public string? QuestionId
    {
        get; set;
    }
    public string? Operator
    {
        get; set;
    }
    public JsonElement? Value
    {
        get; set;
    }
    public List<Condition>? And
    {
        get; set;
    }
    public List<Condition>? Or
    {
        get; set;
    }
}
public sealed class Settings
{
    public string AccentColor { get; set; } = "#438E83";
    [JsonIgnore] public List<ResponseStyle> ResponseStyles { get; set; } = ResponseStyle.Defaults();
    public Dictionary<string, int> AnswerStyleSelections { get; set; } = [];
    [JsonIgnore] public bool HasLegacyStyles { get; private set; }
    [JsonPropertyName("responseStyles")] public List<ResponseStyle>? LegacyStyles
    { get => null; set { if(value!=null){ResponseStyles=value;HasLegacyStyles=true;} } }
    private double fontSizePt = 11;
    private bool explicitFontSize;
    public double FontSizePt { get=>fontSizePt; set { fontSizePt=value;explicitFontSize=true; } }
    [JsonIgnore] public double FontScale { get=>FontSizePt/12d; set=>FontSizePt=value*12d; }
    [JsonPropertyName("fontScale")] public double? LegacyFontScale
    { get=>null; set { if(value.HasValue&&!explicitFontSize)fontSizePt=value.Value*12d; } }
    [JsonRequired] public int SchemaVersion { get; set; } = 1; public string Theme { get; set; } = "light";
}
public sealed class ResponseStyle
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string LowLabel { get; set; } = "";
    public string HighLabel { get; set; } = "";
    public List<string> Values { get; set; } = [];
    public List<string>? PromptValues { get; set; }
    public int DefaultIndex { get; set; } = 2;
    [JsonIgnore] public int SelectedIndex { get; set; } = 2;
    [JsonPropertyName("selectedIndex")] public int? LegacySelectedIndex { get=>null; set { if(value.HasValue)SelectedIndex=value.Value; } }
    [JsonIgnore] public string PromptText => (PromptValues ?? Values)[SelectedIndex];
    public static List<ResponseStyle> Defaults() =>
    [
        new() { Id="length", Label="文章の長さ", LowLabel="短く", HighLabel="長く", Values=["非常に短く","短く簡潔に","標準的な長さで","やや詳しく","十分詳しく"] },
        new() { Id="difficulty", Label="説明の難しさ", LowLabel="やさしく", HighLabel="専門的", Values=["小学生にも分かる言葉で","初心者向けに","一般的な言葉で","少し専門的に","専門用語を使って"] },
        new() { Id="detail", Label="説明の詳しさ", LowLabel="要点だけ", HighLabel="具体的に", Values=["結論を中心に","重要なポイントだけ","必要な説明を添えて","具体例を交えて","手順や具体例を詳しく"] },
        new() { Id="tone", Label="口調", LowLabel="カジュアル", HighLabel="丁寧", Values=["くだけた口調で","親しみやすく","自然な口調で","丁寧に","非常に丁寧に"] }
    ];
}
public sealed class AnswerStyleFile
{
    [JsonRequired] public int SchemaVersion { get; set; } = 1;
    public List<ResponseStyle> Items { get; set; } = ResponseStyle.Defaults();
}
public sealed class Bookmark
{
    public bool UseAnswerStyle { get; set; } = true;
    public string Id { get; set; } = Guid.NewGuid().ToString(); public string Title { get; set; } = ""; public string PromptText { get; set; } = ""; public string TemplateId { get; set; } = ""; public string TemplateName { get; set; } = ""; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now; public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}
public sealed class BookmarkFile
{
    [JsonRequired] public int SchemaVersion { get; set; } = 1; public List<Bookmark> Items { get; set; } = [];
}
public static class Json
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }; public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options); public static T Read<T>(string text) => JsonSerializer.Deserialize<T>(text, Options) ?? throw new InvalidDataException("JSONが空です。"); public static JsonElement Element(object value) => JsonSerializer.SerializeToElement(value);
}




