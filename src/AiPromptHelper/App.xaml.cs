using System.Windows;
using System.Reflection;
using AiPromptHelper.Core;
using AiPromptHelper.ViewModels;
namespace AiPromptHelper;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("操作を完了できませんでした。文章をコピーして保管し、操作をやり直してください。\n" + args.Exception.Message, "操作エラー");
            args.Handled = true;
        };
        try
        {
            var store = AppDataLocation.Open(AppContext.BaseDirectory,
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetEnvironmentVariable("AIPROMPTHELPER_DATA"));
            var root = store.Root;
            var asm = Assembly.GetExecutingAssembly();
            var bundled=asm.GetManifestResourceNames().Where(x=>x.StartsWith("Templates.",StringComparison.Ordinal)).Select(x=>{using var r=new StreamReader(asm.GetManifestResourceStream(x)!);return (Name:x["Templates.".Length..],Text:r.ReadToEnd());}).ToArray();
            AppDataLocation.Seed(store, AppContext.BaseDirectory, bundled);
            using(var legacy=new StreamReader(asm.GetManifestResourceStream("LegacyTemplates.json")!))
                store.UpgradeBundled(Json.Read<List<Template>>(legacy.ReadToEnd()),bundled.Select(x=>Json.Read<Template>(x.Text)));
            var vm = new MainViewModel(store);
            if(!File.Exists(Path.Combine(root,"settings.json")))
                try { store.SaveSettings(vm.Settings); } catch(Exception ex) { store.Errors.Add("初期設定を保存できませんでした。"+ex.Message); }
            MainWindow = new Views.MainWindow(vm);
            MainWindow.Show();
        }
        catch (Exception ex) { MessageBox.Show("起動できませんでした。保存先のアクセス権を確認してください。\n" + ex.Message, "起動エラー"); Shutdown(1); }
    }
}


