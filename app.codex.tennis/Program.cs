using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SocialTennis;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<SocialTennis.Players.IPlayerStorage, SocialTennis.Players.BrowserPlayerStorage>();
builder.Services.AddScoped<SocialTennis.Scheduling.IScheduleStorage, SocialTennis.Scheduling.BrowserScheduleStorage>();
await builder.Build().RunAsync();
