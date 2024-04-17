using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebSocketsSample.Model;

var builder = WebApplication.CreateBuilder(args);

ConcurrentDictionary<uint, StaticObjects> staticObjectsManager = [];
ConcurrentDictionary<uint, UpdatableObjects> updatableObjectsManager = [];
ConcurrentDictionary<uint, WebSocket> connections = new();

builder.Services.AddSingleton(s => staticObjectsManager);
builder.Services.AddSingleton(s => updatableObjectsManager);
builder.Services.AddSingleton(s => connections);

builder.Services.AddControllers();

var app = builder.Build();

// <snippet_UseWebSockets>
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);
// </snippet_UseWebSockets>

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
