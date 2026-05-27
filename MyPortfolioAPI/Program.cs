using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyPortfolioAPI.Data;
using MyPortfolioAPI.Exceptions;
using MyPortfolioAPI.Options;
using MyPortfolioAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<StartupHealthState>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IZipService, ZipService>();
builder.Services.AddScoped<IPortfolioGenerationService, PortfolioGenerationService>();
builder.Services.AddScoped<IPortfolioPersistenceService, PortfolioPersistenceService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MyPortfolio API",
        Version = "v1"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste a JWT access token to call protected endpoints.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [jwtSecurityScheme] = Array.Empty<string>()
    });
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The submitted value is invalid." : error.ErrorMessage)
                    .ToArray());

        return new BadRequestObjectResult(new
        {
            message = "One or more submitted fields are invalid.",
            errors
        });
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition");
    });
});

var jwtSection = builder.Configuration.GetRequiredSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration could not be loaded.");

jwtOptions.Secret = ResolveJwtSecret(jwtOptions, builder.Environment);

builder.Services.Configure<JwtOptions>(options =>
{
    options.Secret = jwtOptions.Secret;
});

ValidateJwtOptions(jwtOptions);

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await TryApplyMigrationsAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");

        if (exception is not null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "You are not allowed to do that."),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Your portfolio was updated in another request. Please try again."),
            ClientSafeException clientSafeException => (clientSafeException.StatusCode, clientSafeException.Message),
            InvalidOperationException => (StatusCodes.Status500InternalServerError, "This action could not be completed right now."),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong while processing your request.")
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            message
        });
    });
});

var startupHealthState = app.Services.GetRequiredService<StartupHealthState>();
app.Use(async (context, next) =>
{
    if (!startupHealthState.IsBlocked || IsStartupHealthExemptPath(context.Request.Path))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    context.Response.ContentType = "application/json";

    await context.Response.WriteAsJsonAsync(new
    {
        message = startupHealthState.Message ?? "The API cannot serve requests until the startup database issue is resolved."
    });
});

app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task TryApplyMigrationsAsync(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        return;
    }

    var applyMigrationsOnStartup = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
    if (!applyMigrationsOnStartup)
    {
        return;
    }

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
    var startupHealthState = app.Services.GetRequiredService<StartupHealthState>();

    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (!pendingMigrations.Any())
        {
            return;
        }

        logger.LogInformation("Applying pending database migrations: {Migrations}", string.Join(", ", pendingMigrations));
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Automatic database migration failed. The API will continue starting without applying pending migrations.");
        startupHealthState.Block(
            "Automatic database migration failed during startup. Fix the database connection or pending migrations, or disable Database:ApplyMigrationsOnStartup before retrying.");
    }
}

static bool IsStartupHealthExemptPath(PathString path)
{
    return path == "/" || path.StartsWithSegments("/swagger");
}

static void ValidateJwtOptions(JwtOptions options)
{
    if (!JwtOptions.HasConfiguredSecret(options.Secret) || options.Secret.Length < JwtOptions.MinimumSecretLength)
    {
        throw new InvalidOperationException(
            $"JWT configuration is invalid. Set {JwtOptions.SectionName}:Secret to a unique secret with at least {JwtOptions.MinimumSecretLength} characters.");
    }

    if (string.IsNullOrWhiteSpace(options.Issuer))
    {
        throw new InvalidOperationException($"JWT configuration is invalid. Set {JwtOptions.SectionName}:Issuer.");
    }

    if (string.IsNullOrWhiteSpace(options.Audience))
    {
        throw new InvalidOperationException($"JWT configuration is invalid. Set {JwtOptions.SectionName}:Audience.");
    }

    if (options.ExpiryMinutes <= 0)
    {
        throw new InvalidOperationException($"JWT configuration is invalid. Set {JwtOptions.SectionName}:ExpiryMinutes to a value greater than 0.");
    }
}

static string ResolveJwtSecret(JwtOptions options, IHostEnvironment environment)
{
    if (JwtOptions.HasConfiguredSecret(options.Secret))
    {
        return options.Secret;
    }

    if (!environment.IsDevelopment())
    {
        return options.Secret;
    }

    var seed = $"{Environment.MachineName}|{environment.ContentRootPath}|MyPortfolioAPI|development-jwt";
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
    return Convert.ToHexString(bytes);
}

sealed class StartupHealthState
{
    public bool IsBlocked { get; private set; }

    public string? Message { get; private set; }

    public void Block(string message)
    {
        IsBlocked = true;
        Message = message;
    }
}
