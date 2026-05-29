using QueueServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<JobQueueService>();
builder.Services.AddHostedService<DispatcherService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();   
app.UseStaticFiles();   
app.MapControllers();

app.Run();