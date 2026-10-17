namespace AiPromptHelper.Core;

public sealed class Storage
{
    public bool AnswerStylesWritable { get; private set; } = true;
    public string Root
    {
        get;
    }
    public List<string> Errors { get; } = []; public bool BookmarksWritable { get; private set; } = true; public bool SettingsWritable { get; private set; } = true;
    public Storage(string root)
    {
        Root = root;
        Directory.CreateDirectory(Path.Combine(root, "templates"));
    }
    public static void Atomic<T>(string path, T value, Action<T> validate)
    {
        validate(value);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(Json.Write(value));
                stream.Write(bytes);
                stream.Flush(true);
            }
            validate(Json.Read<T>(File.ReadAllText(temp)));
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static void ValidateTemplate(Template t)
    {
        var e = Engine.Validate(t);
        if (e.Count > 0)
            throw new InvalidDataException(string.Join("\n", e));
    }
    public static void ValidateSettings(Settings s)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(s.AccentColor ?? "", "^#[0-9A-Fa-f]{6}$"))
            throw new InvalidDataException("アクセント色は #438E83 のような6桁の色で設定してください。");
        ValidateResponseStyles(s.ResponseStyles);
        if (s.SchemaVersion != 1 || s.AnswerStyleSelections==null || !new[] { "system", "light", "dark" }.Contains(s.Theme) || !double.IsFinite(s.FontSizePt) || s.FontSizePt < 8 || s.FontSizePt > 24)
            throw new InvalidDataException("設定の値またはバージョンが不正です。");
    }
    public static void ValidateResponseStyles(List<ResponseStyle> styles)
    {
        if (styles == null || styles.Count == 0 || styles.Any(s => s == null || string.IsNullOrWhiteSpace(s.Id) || string.IsNullOrWhiteSpace(s.Label) || s.Values == null || s.Values.Count < 2 || s.Values.Any(string.IsNullOrWhiteSpace) || s.SelectedIndex < 0 || s.SelectedIndex >= s.Values.Count) || styles.Select(s => s.Id).Distinct().Count() != styles.Count)
            throw new InvalidDataException("回答スタイルには一意のID、質問名、2件以上の候補、範囲内のselectedIndexが必要です。");
        if(styles.Any(s=>s.PromptValues!=null&&(s.PromptValues.Count!=s.Values.Count||s.PromptValues.Any(string.IsNullOrWhiteSpace))))throw new InvalidDataException("promptValuesはvaluesと同じ件数の文章を指定してください。");
    }
    public static void ValidateAnswerStyles(AnswerStyleFile file)
    {
        if(file.SchemaVersion!=1||file.Items==null||file.Items.Count==0)throw new InvalidDataException("answer_styles.jsonのバージョンまたはitemsが不正です。");
        foreach(var s in file.Items)
        {
            if(s==null||s.Values==null||s.DefaultIndex<0||s.DefaultIndex>=s.Values.Count)throw new InvalidDataException("defaultIndexは候補の範囲内（0から）で指定してください。");
            s.SelectedIndex=s.DefaultIndex;
        }
        ValidateResponseStyles(file.Items);
    }
    public AnswerStyleFile LoadAnswerStyles(Settings settings)
    {
        AnswerStylesWritable=true;
        var path=Path.Combine(Root,"answer_styles.json");
        if(settings.HasLegacyStyles)
            foreach(var s in settings.ResponseStyles)settings.AnswerStyleSelections.TryAdd(s.Id,s.SelectedIndex);
        AnswerStyleFile file;
        if(File.Exists(path))
            file=Read("answer_styles.json",()=>new AnswerStyleFile(),ValidateAnswerStyles,()=>AnswerStylesWritable=false);
        else
        {
            file=new AnswerStyleFile {Items=settings.ResponseStyles};
            foreach(var s in file.Items)s.DefaultIndex=Math.Clamp(s.SelectedIndex,0,s.Values.Count-1);
            try{Atomic(path,file,ValidateAnswerStyles);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidDataException){AnswerStylesWritable=false;Errors.Add("answer_styles.json: "+ex.Message);file=new();}
        }
        if(AnswerStylesWritable && File.Exists(path))
        {
            try { NormalizeJson(path, text => ValidateAnswerStyles(Json.Read<AnswerStyleFile>(text))); }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
            { Errors.Add("answer_styles.json: 読みやすい表記への変換を見送りました。" + ex.Message); }
        }
        ApplySelections(file,settings);
        return file;
    }
    static void ApplySelections(AnswerStyleFile file,Settings settings)
    {
        foreach(var s in file.Items)s.SelectedIndex=settings.AnswerStyleSelections.TryGetValue(s.Id,out var index)?Math.Clamp(index,0,s.Values.Count-1):s.DefaultIndex;
        settings.ResponseStyles=file.Items;
    }
    public void SaveAnswerStyles(AnswerStyleFile file,Settings settings)
    {
        if(!AnswerStylesWritable)throw new IOException("answer_styles.jsonは破損または読み取り不可のため保持しています。ファイルを修正して再起動してください。");
        var staged=Json.Read<AnswerStyleFile>(Json.Write(file));
        ValidateAnswerStyles(staged);
        Atomic(Path.Combine(Root,"answer_styles.json"),staged,ValidateAnswerStyles);
        foreach(var s in settings.ResponseStyles)settings.AnswerStyleSelections[s.Id]=s.SelectedIndex;
        ApplySelections(staged,settings);
    }
    public void UpgradeBundled(IEnumerable<Template> previous, IEnumerable<Template> current)
    {
        var old = previous.ToDictionary(t => t.Id);
        foreach (var t in current)
        {
            var path = Path.Combine(Root, "templates", t.Id + ".json");
            if (!File.Exists(path) || !old.TryGetValue(t.Id, out var original)) continue;
            try
            {
                var existing = Json.Read<Template>(File.ReadAllText(path));
                if (Json.Write(existing) == Json.Write(original)) Atomic(path, t, ValidateTemplate);
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
            { Errors.Add(Path.GetFileName(path) + ": 更新を見送りました。" + ex.Message); }
        }
    }
    public static void ValidateBookmarks(BookmarkFile b)
    {
        if (b.SchemaVersion != 1 || b.Items == null || b.Items.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id) || x.Title == null || x.PromptText == null) || b.Items.Select(x => x.Id).Distinct().Count() != b.Items.Count)
            throw new InvalidDataException("保存したプロンプトの形式が不正です。");
    }
    T Read<T>(string name, Func<T> fallback, Action<T> validate, Action damaged)
    {
        var path = Path.Combine(Root, name);
        if (!File.Exists(path))
            return fallback();
        try
        {
            var v = Json.Read<T>(File.ReadAllText(path));
            validate(v);
            return v;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or UnauthorizedAccessException or InvalidDataException) { Errors.Add(name + ": " + ex.Message + " 元ファイルを保持し、今回の上書きを停止しました。"); damaged(); return fallback(); }
    }
    public Settings LoadSettings()
    {
        var settings=Read("settings.json",()=>new Settings(),ValidateSettings,()=>SettingsWritable=false);
        LoadAnswerStyles(settings);
        return settings;
    }
    public BookmarkFile LoadBookmarks() => Read("bookmarks.json", () => new BookmarkFile(), ValidateBookmarks, () => BookmarksWritable = false);
    public void SaveSettings(Settings s)
    {
        if (!SettingsWritable)
            throw new IOException("設定ファイルが破損しています。データの場所から元ファイルを別名へ移して再起動してください。今回の設定は画面にだけ反映されます。");
        ValidateSettings(s);
        if(!File.Exists(Path.Combine(Root,"answer_styles.json")))LoadAnswerStyles(s);
        if(AnswerStylesWritable)foreach(var style in s.ResponseStyles)s.AnswerStyleSelections[style.Id]=style.SelectedIndex;
        Atomic(Path.Combine(Root, "settings.json"), s, ValidateSettings);
    }
    public void SaveBookmarks(BookmarkFile b)
    {
        if (!BookmarksWritable)
            throw new IOException("保存データが破損しています。元ファイルを別名へ移して再起動してください。現在の文章はコピーして保管できます。");
        Atomic(Path.Combine(Root, "bookmarks.json"), b, ValidateBookmarks);
    }
    public Dictionary<string, (Template Template, string Path)> LoadTemplates()
    {
        var result = new Dictionary<string, (Template, string)>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Root, "templates"), "*.json").Order())
            try
            {
                var original = File.ReadAllText(file);
                var t = Json.Read<Template>(original);
                ValidateTemplate(t);
                if (result.ContainsKey(t.Id))
                    throw new InvalidDataException("idが重複しています: " + t.Id);
                result.Add(t.Id, (t, file));
                NormalizeJson(file, text => ValidateTemplate(Json.Read<Template>(text)));
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException or IOException or UnauthorizedAccessException) { Errors.Add(Path.GetFileName(file) + ": " + ex.Message); }
        return result;
    }
    // Parse and reserialize rather than replacing escape sequences; retain unknown properties.
    static void NormalizeJson(string path, Action<string> validate)
    {
        var original=File.ReadAllText(path);
        if(!original.Contains((char)92+"u",StringComparison.Ordinal))return;
        validate(original);
        var document=Json.Read<System.Text.Json.JsonElement>(original);
        if(Json.Write(document)==original)return;
        var backup=path+".unicode.bak";
        if(!File.Exists(backup))File.Copy(path,backup);
        Atomic(path,document,value=>validate(value.GetRawText()));
    }
    public string SaveTemplate(Template t, string? path = null)
    {
        ValidateTemplate(t);
        var existing = LoadTemplates();
        if (existing.TryGetValue(t.Id, out var other) && !string.Equals(other.Path, path, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("同じIDのテンプレートがあります。");
        if (path == null && File.Exists(Path.Combine(Root, "templates", t.Id + ".json")))
            throw new IOException("同名のファイルがあります。別のIDを指定してください。破損ファイルは上書きしません。");
        path ??= Path.Combine(Root, "templates", t.Id + ".json");
        Atomic(path, t, ValidateTemplate);
        return path;
    }
    public void Seed(IEnumerable<(string Name, string Text)> templates)
    {
        var marker = Path.Combine(Root, "templates.initialized");
        if (File.Exists(marker))
            return;
        foreach (var (name, text) in templates)
        {
            var path = Path.Combine(Root, "templates", name);
            if (!File.Exists(path))
                Atomic(path, Json.Read<Template>(text), ValidateTemplate);
        }
        File.WriteAllText(marker, "1");
    }
}



