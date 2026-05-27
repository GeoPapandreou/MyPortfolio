using System.Text.Json;
using MyPortfolioAPI.DTOs;

namespace MyPortfolioAPI.Utilities;

public static class GeneratedBackendTemplates
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Dictionary<string, string> BuildBackendFiles(UserProfileDto profile)
    {
        var seedJson = JsonSerializer.Serialize(profile, SerializerOptions);
        var seedJsonLiteral = JsonSerializer.Serialize(seedJson, SerializerOptions);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MyPortfolioAPI.csproj"] = """
                                         <Project Sdk="Microsoft.NET.Sdk.Web">
                                           <PropertyGroup>
                                             <TargetFramework>net8.0</TargetFramework>
                                             <Nullable>enable</Nullable>
                                             <ImplicitUsings>enable</ImplicitUsings>
                                           </PropertyGroup>

                                           <ItemGroup>
                                             <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.16" />
                                           </ItemGroup>
                                         </Project>
                                         """,
            ["Program.cs"] = """
                             using Microsoft.EntityFrameworkCore;
                             using MyPortfolioAPI.Data;

                             var builder = WebApplication.CreateBuilder(args);
                             var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=portfolio.db";

                             builder.Services.AddControllers();
                             builder.Services.AddDbContext<AppDbContext>(options =>
                                 options.UseSqlite(connectionString));

                             builder.Services.AddCors(options =>
                             {
                                 options.AddPolicy("Client", policy =>
                                 {
                                     policy
                                         .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                                         .AllowAnyHeader()
                                         .AllowAnyMethod();
                                 });
                             });

                             var app = builder.Build();

                             app.UseCors("Client");
                             app.MapGet("/", () => Results.Ok(new
                             {
                                 message = "MyPortfolio API is running.",
                                 endpoints = new[] { "/api/portfolio", "/health" }
                             }));
                             app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
                             app.MapControllers();

                             await using (var scope = app.Services.CreateAsyncScope())
                             {
                                 var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                 await dbContext.Database.EnsureCreatedAsync();
                                 await PortfolioSeedData.EnsureSeededAsync(dbContext);
                             }

                             app.Run();
                             """,
            ["Controllers/PortfolioController.cs"] = """
                                                   using Microsoft.AspNetCore.Mvc;
                                                   using Microsoft.EntityFrameworkCore;
                                                   using MyPortfolioAPI.Data;
                                                   using MyPortfolioAPI.DTOs;

                                                   namespace MyPortfolioAPI.Controllers;

                                                   [ApiController]
                                                   [Route("api/portfolio")]
                                                   public sealed class PortfolioController : ControllerBase
                                                   {
                                                       private readonly AppDbContext _dbContext;

                                                       public PortfolioController(AppDbContext dbContext)
                                                       {
                                                           _dbContext = dbContext;
                                                       }

                                                       [HttpGet]
                                                       public async Task<ActionResult<UserProfileDto>> GetAsync(CancellationToken cancellationToken)
                                                       {
                                                           var document = await _dbContext.Portfolios
                                                               .OrderBy(item => item.UpdatedAtUtc)
                                                               .FirstOrDefaultAsync(cancellationToken);

                                                           if (document is null)
                                                           {
                                                               return NotFound(new { message = "No portfolio document is available." });
                                                           }

                                                           return Ok(document.ToProfile());
                                                       }

                                                       [HttpPut]
                                                       public async Task<ActionResult<UserProfileDto>> PutAsync(UserProfileDto request, CancellationToken cancellationToken)
                                                       {
                                                           var document = await _dbContext.Portfolios.FirstOrDefaultAsync(cancellationToken);
                                                           if (document is null)
                                                           {
                                                               document = PortfolioDocument.Create(request);
                                                               _dbContext.Portfolios.Add(document);
                                                           }
                                                           else
                                                           {
                                                               document.Update(request);
                                                           }

                                                           await _dbContext.SaveChangesAsync(cancellationToken);
                                                           return Ok(document.ToProfile());
                                                       }
                                                   }
                                                   """,
            ["DTOs/UserProfileDto.cs"] = """
                                             namespace MyPortfolioAPI.DTOs;

                                             public sealed class UserProfileDto
                                             {
                                                 public string Theme { get; set; } = "Minimal";
                                                 public PersonalInfoDto PersonalInfo { get; set; } = new();
                                                 public List<ExperienceDto> Experiences { get; set; } = new();
                                                 public List<WorkSampleDto> WorkSamples { get; set; } = new();
                                                 public ContactInfoDto ContactInfo { get; set; } = new();
                                                 public List<PortfolioVersionDto> Versions { get; set; } = new();
                                             }

                                             public sealed class PersonalInfoDto
                                             {
                                                 public string FullName { get; set; } = string.Empty;
                                                 public string Profession { get; set; } = string.Empty;
                                                 public string Bio { get; set; } = string.Empty;
                                                 public string PhotoUrl { get; set; } = string.Empty;
                                                 public string Location { get; set; } = string.Empty;
                                             }

                                             public sealed class ExperienceDto
                                             {
                                                 public string Organisation { get; set; } = string.Empty;
                                                 public string Role { get; set; } = string.Empty;
                                                 public DateTime? StartDate { get; set; }
                                                 public DateTime? EndDate { get; set; }
                                                 public bool IsCurrent { get; set; }
                                                 public List<string> Bullets { get; set; } = new();
                                             }

                                             public sealed class WorkSampleDto
                                             {
                                                 public string Title { get; set; } = string.Empty;
                                                 public string Description { get; set; } = string.Empty;
                                                 public List<string> Tools { get; set; } = new();
                                                 public string LiveUrl { get; set; } = string.Empty;
                                             }

                                             public sealed class ContactInfoDto
                                             {
                                                 public string Email { get; set; } = string.Empty;
                                                 public string Phone { get; set; } = string.Empty;
                                                 public string LinkedIn { get; set; } = string.Empty;
                                                 public string Instagram { get; set; } = string.Empty;
                                                 public string Facebook { get; set; } = string.Empty;
                                                 public string GitHub { get; set; } = string.Empty;
                                             }

                                             public sealed class PortfolioVersionDto
                                             {
                                                 public Guid Id { get; set; }
                                                 public DateTime GeneratedAt { get; set; }
                                             }
                                             """,
            ["Data/PortfolioDocument.cs"] = """
                                             using System.Text.Json;
                                             using MyPortfolioAPI.DTOs;

                                             namespace MyPortfolioAPI.Data;

                                             public sealed class PortfolioDocument
                                             {
                                                 private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

                                                 public Guid Id { get; set; }
                                                 public string PayloadJson { get; set; } = "{}";
                                                 public DateTime UpdatedAtUtc { get; set; }

                                                 public UserProfileDto ToProfile()
                                                 {
                                                     return JsonSerializer.Deserialize<UserProfileDto>(PayloadJson, SerializerOptions) ?? new UserProfileDto();
                                                 }

                                                 public void Update(UserProfileDto profile)
                                                 {
                                                     PayloadJson = JsonSerializer.Serialize(profile, SerializerOptions);
                                                     UpdatedAtUtc = DateTime.UtcNow;
                                                 }

                                                 public static PortfolioDocument Create(UserProfileDto profile)
                                                 {
                                                     var document = new PortfolioDocument
                                                     {
                                                         Id = Guid.NewGuid()
                                                     };

                                                     document.Update(profile);
                                                     return document;
                                                 }
                                             }
                                             """,
            ["Data/AppDbContext.cs"] = $$"""
                                        using System.Text.Json;
                                        using Microsoft.EntityFrameworkCore;
                                        using MyPortfolioAPI.DTOs;

                                        namespace MyPortfolioAPI.Data;

                                        public sealed class AppDbContext : DbContext
                                        {
                                            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
                                            {
                                            }

                                            public DbSet<PortfolioDocument> Portfolios => Set<PortfolioDocument>();

                                            protected override void OnModelCreating(ModelBuilder modelBuilder)
                                            {
                                                modelBuilder.Entity<PortfolioDocument>(entity =>
                                                {
                                                    entity.ToTable("Portfolios");
                                                    entity.HasKey(item => item.Id);
                                                    entity.Property(item => item.PayloadJson).IsRequired();
                                                });
                                            }
                                        }

                                        public static class PortfolioSeedData
                                        {
                                            private const string SeedJson = {{seedJsonLiteral}};

                                            public static async Task EnsureSeededAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
                                            {
                                                if (await dbContext.Portfolios.AnyAsync(cancellationToken))
                                                {
                                                    return;
                                                }

                                                var profile = JsonSerializer.Deserialize<UserProfileDto>(SeedJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                                                    ?? new UserProfileDto();

                                                dbContext.Portfolios.Add(PortfolioDocument.Create(profile));
                                                await dbContext.SaveChangesAsync(cancellationToken);
                                            }
                                        }
                                        """,
            ["appsettings.json"] = """
                                  {
                                    "ConnectionStrings": {
                                      "DefaultConnection": "Data Source=portfolio.db"
                                    },
                                    "Logging": {
                                      "LogLevel": {
                                        "Default": "Information",
                                        "Microsoft.AspNetCore": "Warning"
                                      }
                                    }
                                  }
                                  """,
            ["appsettings.example.json"] = """
                                         {
                                           "ConnectionStrings": {
                                             "DefaultConnection": "Data Source=portfolio.db"
                                           }
                                         }
                                         """,
            ["Properties/launchSettings.json"] = """
                                                 {
                                                   "$schema": "https://json.schemastore.org/launchsettings.json",
                                                   "profiles": {
                                                     "MyPortfolioAPI": {
                                                       "commandName": "Project",
                                                       "dotnetRunMessages": true,
                                                       "launchBrowser": false,
                                                       "applicationUrl": "http://localhost:5000",
                                                       "environmentVariables": {
                                                         "ASPNETCORE_ENVIRONMENT": "Development"
                                                       }
                                                     }
                                                   }
                                                 }
                                                 """
        };
    }

}
