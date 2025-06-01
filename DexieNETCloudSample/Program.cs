using DexieCloudNET;
using DexieNETCloudSample;
using DexieNETCloudSample.Administration;
using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
});

builder.Services.AddDexieCloudNET<ToDoDB>();

builder.Services.AddSingleton(sp => new DexieCloudService(sp));
builder.Services.AddSingleton(sp => new AdministrationService(sp));
builder.Services.AddScoped(sp => new ToDoItemService(sp));
builder.Services.AddSingleton(sp => new ToDoListMemberService(sp));
builder.Services.AddSingleton(sp => new ToDoListService(sp));

builder.Logging.ClearProviders();
#if !DEBUG
builder.Services.AddLogging(l => l.SetMinimumLevel(LogLevel.Warning));
#endif

await builder.LoadConfigurationAsync(); // also adds HttpClient

await builder.Build().RunAsync();
