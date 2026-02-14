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

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers + validation
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// ==========================
// CORS (IMPORTANT)
// ==========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:8081",
                "https://localhost:8081"
            )
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

// ==========================
// BUILD APP
// ==========================
var app = builder.Build();

// ==========================
// MIDDLEWARE ORDER (CRITICAL)
// ==========================
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

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

// ==========================
// ENDPOINTS
// ==========================
app.MapControllers();

app.Run();
