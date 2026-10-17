using AiPromptHelper.Core;
using System.Text.Json;
if(args.Length==3 && args[0]=="--initialize") {
    var startupStore=AppDataLocation.Open(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]));
    AppDataLocation.Seed(startupStore,Path.GetFullPath(args[1]),Array.Empty<(string Name,string Text)>());
    var settings=startupStore.LoadSettings();startupStore.LoadBookmarks();var templates=startupStore.LoadTemplates();
    if(!File.Exists(Path.Combine(startupStore.Root,"settings.json")))startupStore.SaveSettings(settings);
    foreach(var error in startupStore.Errors)Console.Error.WriteLine(error);
    Console.WriteLine($"Initialized {templates.Count} templates at {startupStore.Root}");
    return startupStore.Errors.Count==0?0:1;
}if(args.Length!=2 || args[0] is not ("--check" or "--apply")) {Console.Error.WriteLine("Usage: JsonMaintenance --check|--apply DATA_FOLDER");return 2;}
var root=Path.GetFullPath(args[1]);
var files=Directory.GetFiles(Path.Combine(root,"templates"),"*.json").Append(Path.Combine(root,"answer_styles.json")).Where(File.Exists).ToArray();
var before=files.ToDictionary(p=>p,p=>File.ReadAllText(p));
foreach(var (path,text) in before) {
 var element=Json.Read<JsonElement>(text);
 if(Path.GetFileName(path)=="answer_styles.json")Storage.ValidateAnswerStyles(Json.Read<AnswerStyleFile>(text));else Storage.ValidateTemplate(Json.Read<Template>(text));
 Console.WriteLine($"{Path.GetFileName(path)}: escapes={text.Contains((char)92+"u")}, normalizationChangesText={Json.Write(element)!=text}");
}
if(args[0]=="--check")return 0;
var storage=new Storage(root);
storage.LoadTemplates();
storage.LoadAnswerStyles(new Settings());
foreach(var error in storage.Errors)Console.Error.WriteLine(error);
foreach(var (path,text) in before){
 var after=File.ReadAllText(path);
 if(!JsonElement.DeepEquals(Json.Read<JsonElement>(text),Json.Read<JsonElement>(after)))throw new Exception("Content changed: "+path);
 Console.WriteLine($"{Path.GetFileName(path)}: content preserved, Japanese={after.Any(c=>c>127)}, backup={File.Exists(path+".unicode.bak")}");
}
return storage.Errors.Count==0?0:1;



