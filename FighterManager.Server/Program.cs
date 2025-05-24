using System.Reflection;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FighterManager.Server.Domain.FighterService;
using FighterManager.Server.Helpers;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using SharedEntities;
using SharedEntities.Data;
using SharedEntities.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.OAuth;
using Serilog.Events;
using FighterManager.Server.Controllers;

namespace FighterManager.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Global.Configuration = builder.Configuration;

            // Add services to the container.
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<FighterRegistrationService>();
            builder.Services.AddAutoMapper(typeof(Program));
            builder.Services.AddScoped<IIdentityResponseEnhancer, IdentityResponseEnhancer>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opts =>
            {
                opts.EnableAnnotations();
                opts.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "Martial Art Training Assistant",
                    Version = "v1",
                    Description = "Fighter management service built with ASP.NET Core Web API, React and Docker",
                    Contact = new OpenApiContact()
                    {
                        Name = "Vo Hoan Vu",
                        Email = "vohoanvu96@gmail.com",
                        Url = new Uri("https://github.com/vohoanvu")
                    }
                });

                opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Please enter into field a valid JWT token"
                });
                opts.OperationFilter<SecurityRequirementsOperationFilter>();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    opts.IncludeXmlComments(xmlPath);
                }
                else
                {
                    Console.WriteLine($"Warning: XML documentation file '{xmlPath}' not found. Swagger will run without XML comments.");
                }
            });

            builder.Services.AddRouting(opts =>
            {
                opts.LowercaseUrls = true;
                opts.LowercaseQueryStrings = true;
            });

            builder.Services.AddApiVersioning(opts =>
            {
                opts.DefaultApiVersion = new ApiVersion(1, 0);
                opts.AssumeDefaultVersionWhenUnspecified = true;
                opts.ReportApiVersions = true;
                opts.ApiVersionReader = new UrlSegmentApiVersionReader();
            });

            builder.Services.AddCors(options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.AddDefaultPolicy(corsBuilder =>
                        corsBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                    options.AddPolicy("AllowAll",
                        corsBuilder => corsBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                }
                else
                {
                    var allowedOriginPorts = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.ClientAppPorts)
                        .Split(":");
                    var possibleHttpsOrigins = allowedOriginPorts.Select(port => $"https://localhost:{port}").ToArray();
                    var possibleHttpOrigins = allowedOriginPorts.Select(port => $"http://localhost:{port}").ToArray();
                    options.AddDefaultPolicy(corsBuilder =>
                        corsBuilder.WithOrigins(possibleHttpsOrigins.Concat(possibleHttpOrigins).ToArray())
                            .AllowAnyMethod().AllowAnyHeader());
                }
            });

            builder.Logging.ClearProviders();
            builder.Host.UseSerilog((context, configuration) =>
            {
                // Uncomment the following lines to use the configuration from the builder

                // context.Configuration = builder.Configuration;
                // configuration.ReadFrom.Configuration(context.Configuration);

                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("../logs/log-.log", rollingInterval: RollingInterval.Day);
            });

            builder.Services.AddDbContext<MyDatabaseContext>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = "Google";
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtAudience),
                    ValidIssuer = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtIssuer),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtKey)))
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "Authentication failed.");
                        Console.WriteLine("Authentication failed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Token validated successfully.");
                        Console.WriteLine("Token validated successfully.");
                        return Task.CompletedTask;
                    }
                };
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.AuthenticationGoogleClientId);
                googleOptions.ClientSecret = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.AuthenticationGoogleClientSecret);
                googleOptions.CallbackPath = "/signin-google-callback";
                googleOptions.SaveTokens = true;
                googleOptions.Events = new OAuthEvents
                {
                    OnTicketReceived = ExternalAuthController.HandleGoogleTicketReceived
                };
            });
            builder.Services.AddAuthorization();

            builder.Services.AddIdentityApiEndpoints<AppUserEntity>(opts =>
            {
                opts.User.RequireUniqueEmail = true;
                opts.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<MyDatabaseContext>()
            .AddSignInManager<FighterSignInService<AppUserEntity>>()
            .AddUserManager<UserManager<AppUserEntity>>();
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownProxies.Clear();
            forwardedHeadersOptions.KnownNetworks.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);

            await using (var serviceScope = app.Services.CreateAsyncScope())
            {
                await DbHelper.EnsureDbIsCreatedAndSeededAsync(
                    serviceScope,
                    app.Environment.IsDevelopment() &&
                    Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.DeleteDbIfExistsOnStartup) == "true"
                );
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();

            if (app.Environment.IsDevelopment() ||
                Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.ShowSwaggerInProduction) == "true")
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ResponseEnhancementMiddleware>();

            app.MapGroup("/api/auth/v1")
                .MapIdentityApi<AppUserEntity>();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.MapFallbackToFile("/index.html");
            app.MapHealthChecks("/health");

            if (builder.Environment.IsDevelopment())
            {
                await app.RunAsync();
            }
            else
            {
                await app.RunAsync("http://0.0.0.0:7080");
            }
        }
    }
}