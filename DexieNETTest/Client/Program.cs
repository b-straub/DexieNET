using DexieNET;
using DexieNETTest.Client;
using DexieNETTest.TestBase.Components;
using DexieNETTest.TestBase.Test;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddDexieNET<TestDB>();
builder.Services.AddDexieNET<SecondDB>();
builder.Services.AddDexieNET<FriendsDB>();

await builder.Build().RunAsync();
