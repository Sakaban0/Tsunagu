using System.ComponentModel;
using System.Runtime.CompilerServices;
using AiPromptHelper.Core;
namespace AiPromptHelper.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    string prompt = ""; public string Prompt
    {
        get => prompt; set
        {
            prompt = value;
            Changed();
        }
    }
    string status = ""; public string Status
    {
        get => status; set
        {
            status = value;
            Changed();
        }
    }
    public Storage Storage
    {
        get;
    }
    public Settings Settings
    {
        get;
    }
    public BookmarkFile Bookmarks
    {
        get;
    }
    public Dictionary<string, (Template Template, string Path)> Templates { get; private set; } = [];
    public Template? Active
    {
        get; set;
    }
    public Dictionary<string, string[]> Answers { get; } = []; public int Index
    {
        get; set;
    }
    public MainViewModel(Storage storage)
    {
        Storage = storage;
        Settings = storage.LoadSettings();
        Bookmarks = storage.LoadBookmarks();
        Reload();
    }
    public void Reload() => Templates = Storage.LoadTemplates();
    public List<Question> Visible => Active == null ? [] : Engine.Visible(Active, Answers);
    public bool UseAnswerStyle { get; private set; } = true;
    public void OpenBookmark(Bookmark item) { Active=null; UseAnswerStyle=item.UseAnswerStyle; Prompt=item.PromptText; }
    public void Start(Template t)
    {
        Active = t; UseAnswerStyle=t.UseAnswerStyle;
        Answers.Clear();
        foreach (var q in t.Questions)
            Answers[q.Id] = Engine.DefaultAnswer(q);
        Index = 0;
    }
    public void Generate() => Prompt = PromptComposer.Compose(Active!, Answers, Settings);
    public void RefreshResponseStyle() { if(UseAnswerStyle) Prompt = PromptComposer.UpdateStyle(Prompt, Settings); }
    public Bookmark SaveBookmark(string title, Bookmark? existing = null)
    {
        var item = existing ?? new Bookmark { UseAnswerStyle=UseAnswerStyle, TemplateId = Active?.Id ?? "", TemplateName = Active?.Name ?? "保存した文章" };
        var copy = Json.Read<BookmarkFile>(Json.Write(Bookmarks));
        var entry = copy.Items.FirstOrDefault(x => x.Id == item.Id) ?? item;
        entry.Title = title;
        entry.PromptText = Prompt;
        entry.UpdatedAt = DateTimeOffset.Now;
        if (!copy.Items.Any(x => x.Id == entry.Id))
            copy.Items.Add(entry);
        Storage.SaveBookmarks(copy);
        Bookmarks.Items = copy.Items;
        return entry;
    }
    public void DeleteBookmark(Bookmark b)
    {
        var copy = Json.Read<BookmarkFile>(Json.Write(Bookmarks));
        copy.Items.RemoveAll(x => x.Id == b.Id);
        Storage.SaveBookmarks(copy);
        Bookmarks.Items = copy.Items;
    }
    void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}



