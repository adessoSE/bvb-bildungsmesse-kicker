using Kicker.Server;
using Kicker.Server.GameServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSignalR().AddNewtonsoftJsonProtocol();

builder.Services.AddOptions<GameConfiguration>().Bind(builder.Configuration.GetSection("Game"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.MapHub<GameHub>("/gamehub");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();