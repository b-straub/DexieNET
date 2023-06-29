using DexieNET;
using DexieNETCloudSample;
using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddDexieNET<ToDoDB>();
builder.Services.AddSingleton<DexieCloudService>();
builder.Services.AddScoped<ToDoListService>();
builder.Services.AddScoped<ToDoItemService>();
builder.Services.AddScoped<ToDoListMemberService>();

builder.Services.Configure<DemoSettings>(settings => builder.Configuration.GetSection("DemoSettings").Bind(settings));

await builder.Build().RunAsync();
