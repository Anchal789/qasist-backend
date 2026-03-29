using QAsist.Api.Extensions;
using QAsist.Api.Middleware;
using QAsist.Application;
using QAsist.Infrastructure;
using QAsist.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8080));

builder.Host.UseSerilog();

// ==========================
// SERVICE REGISTRATION
// ==========================
builder.Services
    .AddDapperConfig()
    .AddSerilogConfig(builder.Configuration)
    .AddHttpClients()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddControllersWithValidation()
    .AddApiVersioningConfig()
    .AddRateLimitingConfig()
    .AddCorsConfig()
    .AddJwtAuthentication(builder.Configuration)
    .AddSwaggerConfig()
    .AddHangfireConfig(builder.Configuration);

// ==========================
// BUILD APP
// ==========================
var app = builder.Build();

// ==========================
// MIDDLEWARE PIPELINE
// ==========================
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerConfig();
app.UseRateLimiter();

app.MapControllers();
app.UseQAsistHangfire();
app.MapHub<QAsist.Infrastructure.SignalR.MonitoringHub>("/hubs/monitoring");

app.Run();