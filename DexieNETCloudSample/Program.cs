using DexieNET;
using DexieNETCloudSample;
using DexieNETCloudSample.Aministration;
using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using RxBlazorLightCore;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
});

builder.Services.AddDexieNET<ToDoDB>();
var collector = builder.Services.AddRxBLServiceCollector();

builder.Services.AddRxBLService(collector, sp => new DexieCloudService(sp));
builder.Services.AddRxBLService(collector, sp => new AdministrationService(sp));
builder.Services.AddRxBLService(collector, sp => new ToDoItemService(sp));
builder.Services.AddRxBLService(collector, sp => new ToDoListService(sp));
builder.Services.AddRxBLService(collector, sp => new ToDoListMemberService(sp));

await builder.LoadConfigurationAsync(); // also adds HttpClient

await builder.Build().RunAsync();
