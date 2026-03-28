using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QAsist.Api.Filters;
using QAsist.Api.Middleware;
using QAsist.Application;
using QAsist.Infrastructure;
using Serilog;
using System.Text;
using QAsist.Infrastructure.Extensions;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});
// ==========================
// DAPPER CONFIG
// ==========================
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// ==========================
// SERILOG CONFIG
// ==========================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "QAsist")
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/qasist-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================
// SERVICE REGISTRATION
// ==========================
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("OpenApiFetcher");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers + validation
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddHangfireServer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;    // X-Api-Supported-Versions header
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),            // /api/v1/...
        new HeaderApiVersionReader("X-Api-Version"), // X-Api-Version: 1.0
        new QueryStringApiVersionReader("api-version") // ?api-version=1.0
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Fixed window: 100 requests per 1 minute per IP
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 100;
        limiterOptions.QueueLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Stricter limit for AI endpoints: 20 per minute (AI is expensive)
    options.AddFixedWindowLimiter("ai_strict", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 20;
        limiterOptions.QueueLimit = 5;
    });

    // 429 response when limit exceeded
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message = "Too many requests. Please slow down.",
            retryAfter = "60 seconds"
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, token);
    };
});

// ==========================
// CORS (IMPORTANT)
// ==========================
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("Frontend", policy =>
//    {
//        policy
//            .WithOrigins(
//                "http://localhost:8081",
//                "https://localhost:8081"
//            )
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials();
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true) 
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ==========================
// AUTHENTICATION (JWT)
// ==========================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("JWT Secret not configured")
            )
        ),

        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Error(
                context.Exception,
                "JWT authentication failed. Token: {Token}",
                context.Request.Headers["Authorization"].ToString()
            );
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning(
                "JWT challenge error. Error: {Error}, Description: {Description}",
                context.Error,
                context.ErrorDescription
            );
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ==========================
// SWAGGER
// ==========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QAsist API",
        Version = "v1",
        Description = "QA Management System API with Clean Architecture"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(
              builder.Configuration.GetConnectionString("DefaultConnection"));
});


// ==========================
// BUILD APP
// ==========================
var app = builder.Build();

// ==========================
// MIDDLEWARE ORDER (CRITICAL)
// ==========================
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();


// ? CORS MUST BE BEFORE AUTH + SWAGGER
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

// ==========================
// SWAGGER (AFTER CORS)
// ==========================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QAsist API V1");
    c.RoutePrefix = "swagger";
});
app.UseRateLimiter();
// ==========================
// ENDPOINTS
// ==========================
app.MapControllers();
app.UseQAsistHangfire();
app.MapHub<QAsist.Infrastructure.SignalR.MonitoringHub>("/hubs/monitoring");

app.Run();
