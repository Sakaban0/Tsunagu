namespace AiPromptHelper.Core;

/// <summary>Keep writable data below the executable folder. Never modify the legacy source.</summary>
public static class AppDataLocation
{
    public static Storage Open(string applicationDirectory, string localApplicationData, string? overrideDirectory = null)
    {
        var root = string.IsNullOrWhiteSpace(overrideDirectory)
            ? Path.Combine(applicationDirectory, "data") : Path.GetFullPath(overrideDirectory);
        var storage = new Storage(root);
        if (string.IsNullOrWhiteSpace(overrideDirectory))
        {
            var marker = Path.Combine(root, ".legacy-imported");
            if (!File.Exists(marker))
            {
                var legacy = Path.Combine(localApplicationData, "AiPromptHelper");
                if (Directory.Exists(legacy))
                {
                    foreach (var name in new[] { "settings.json", "bookmarks.json", "answer_styles.json", "templates.initialized" })
                        CopyMissing(Path.Combine(legacy, name), Path.Combine(root, name));
                    var templates = Path.Combine(legacy, "templates");
                    if (Directory.Exists(templates))
                        foreach (var file in Directory.EnumerateFiles(templates, "*.json"))
                            CopyMissing(file, Path.Combine(root, "templates", Path.GetFileName(file)));
                }
                File.WriteAllText(marker, "1");
            }
        }
        var styles = Path.Combine(applicationDirectory, "answer_styles.json");
        if (File.Exists(styles) && !File.Exists(Path.Combine(root, "answer_styles.json")))
        {
            Storage.ValidateAnswerStyles(Json.Read<AnswerStyleFile>(File.ReadAllText(styles)));
            CopyMissing(styles, Path.Combine(root, "answer_styles.json"));
        }
        return storage;
    }

    public static void Seed(Storage storage, string applicationDirectory, IEnumerable<(string Name,string Text)> bundled)
    {
        var initial = bundled.ToDictionary(x=>x.Name,x=>x.Text,StringComparer.OrdinalIgnoreCase);
        var supplied = Path.Combine(applicationDirectory,"templates");
        if(Directory.Exists(supplied))
            foreach(var file in Directory.EnumerateFiles(supplied,"*.json"))
                initial[Path.GetFileName(file)] = File.ReadAllText(file);
        storage.Seed(initial.Select(x=>(Name:x.Key,Text:x.Value)));
    }

    static void CopyMissing(string source, string destination)
    {
        if (!File.Exists(source) || File.Exists(destination)) return;
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("リンクされた保存データは自動移行できません: " + source);
        // Stage the copy before the final rename so interrupted copies are not mistaken for migrated files.
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.Copy(source, temp); File.Move(temp, destination, false); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

