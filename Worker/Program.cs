//using Worker;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
using Worker.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<CodeExecutor>();
builder.Services.AddSingleton<JobProcessorService>();
builder.Services.AddHostedService<HeartbeatService>();
builder.Services.AddHostedService(
    sp => sp.GetRequiredService<JobProcessorService>());

var app = builder.Build();

app.MapControllers();

app.Run("http://0.0.0.0:5001");