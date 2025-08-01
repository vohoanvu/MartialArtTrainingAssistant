using System.Reflection;
using Asp.Versioning;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using VideoSharing.Server.Domain.YoutubeSharingService;
using VideoSharing.Server.Helpers;
using VideoSharing.Server.Repository;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Filters;
using SharedEntities.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SharedEntities;
using VideoSharing.Server.Domain.GoogleCloudStorageService;
using VideoSharing.Server.Domain.GeminiService;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using VideoSharing.Server.Domain.AIServices;
using Hangfire;
using Hangfire.PostgreSql;

namespace VideoSharing.Server
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args"></param>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Global.Configuration = builder.Configuration;

            // Add services to the container.
            builder.Services.AddScoped<ISharedVideoRepository, SharedVideoRepository>();
            builder.Services.AddSingleton<IYoutubeServiceWrapper>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.YoutubeApiKey),
                    ApplicationName = "YoutubeVideSharingApp"
                });

                return new YoutubeServiceWrapper(youtubeService);
            });
            builder.Services.AddScoped<IYoutubeDataService, YoutubeDataService>();
            builder.Services.AddScoped<IGoogleCloudStorageService, GoogleCloudStorageService>();
            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddScoped<IGeminiVisionService, GeminiVisionService>();
            builder.Services.AddScoped<AiAnalysisProcessorService>();
            builder.Services.AddScoped<CurriculumRecommendationService>();
            builder.Services.AddHttpClient<IXAIService, XAIService>();
            builder.Services.AddTransient<VideoAnalysisBackgroundJobService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opts =>
            {
                opts.EnableAnnotations();

                opts.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "Video Sharing Service",
                    Version = "v1",
                    Description = "built with ASP.NET Core Web API, React and Docker",
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

                opts.OperationFilter<AuthOperationFilter>();
                opts.OperationFilter<SecurityRequirementsOperationFilter>();

                // Add XML comments to Swagger
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
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

            // Add CORS
            builder.Services.AddCors(options =>
            {
                // Set up CORS policies for the application based on the environment the application is running in
                if (builder.Environment.IsDevelopment())
                {
                    options.AddDefaultPolicy(corsBuilder =>
                        corsBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                    options.AddPolicy("AllowAll",
                        corsBuilder => corsBuilder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                    );

                    options.AddPolicy("AllowReactApp",
                    builder =>
                    {
                        builder.WithOrigins("https://localhost:5173")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                    });
                }
                else
                {
                    var allowedOriginPorts = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.ClientAppPorts)
                        .Split(":");

                    var possibleHttpsOrigins = allowedOriginPorts.Select(port => $"https://localhost:{port}").ToArray();
                    var possibleHttpOrigins = allowedOriginPorts.Select(port => $"http://localhost:{port}").ToArray();

                    options.AddDefaultPolicy(corsBuilder =>
                        corsBuilder.WithOrigins(possibleHttpsOrigins
                                .Concat(possibleHttpOrigins).ToArray())
                            .AllowAnyMethod().AllowAnyHeader());
                }
            });

            // Serilog
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

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtKey))),
                    ValidateLifetime = true,  // Enable lifetime validation
                    ClockSkew = TimeSpan.Zero,  // Removes default 5-minute clock skew
                    LifetimeValidator = (notBefore, expires, token, parameters) =>
                    {
                        if (expires != null)
                        {
                            return expires.Value.ToUniversalTime() > DateTime.UtcNow;
                        }
                        return false;
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            logger.LogWarning("Token expired: {Message}", context.Exception.Message);
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        else
                        {
                            logger.LogError(context.Exception, "Authentication failed: {Message}", context.Exception.Message);
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            builder.Services.AddAuthorization();

            builder.Services.AddSignalR();
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 300 * 1024 * 1024; // 300MB
            });

            // Add Hangfire services and configure with PostgreSQL
            var connectionString = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.AppDb);
            builder.Services.AddHangfire(config =>
            {
                config.UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(option =>
                    {
                        option.UseNpgsqlConnection(connectionString);
                    }, new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire"
                    });
            });
            builder.Services.AddHangfireServer();

            builder.Services.AddHealthChecks();

            var app = builder.Build();
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                // Forward the X-Forwarded-For (client IP) and X-Forwarded-Proto (protocol, e.g., https) headers.
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownProxies.Clear();
            forwardedHeadersOptions.KnownNetworks.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);
            // Initialize the database already executed from FigherManage.Server thread
            //await using (var serviceScope = app.Services.CreateAsyncScope())
            //{
            //    await DbHelper.EnsureDbIsCreatedAndSeededAsync(
            //        serviceScope,
            //        app.Environment.IsDevelopment() &&
            //        Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.DeleteDbIfExistsOnStartup) == "true"
            //    );
            //}
            app.Use(async (context, next) =>
            {
                app.Logger.LogInformation("Request: {Method} {Path} {QueryString}", context.Request.Method, context.Request.Path, context.Request.QueryString);
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Error processing request: {Path}", context.Request.Path);
                    throw;
                }
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() ||
                Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.ShowSwaggerInProduction) == "true")
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapHub<VideoShareHub>("/videoShareHub");
            app.MapHub<VideoAnalysisHub>("/videoAnalysisHub");
            app.UseHangfireDashboard("/hangfire");

            //app.MapGroup("/api/auth/v1")
            //    .MapIdentityApi<AppUserEntity>();

            //app.UseCors("AllowAll");
            app.UseCors("AllowReactApp");

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
                await app.RunAsync("http://0.0.0.0:7081");
            }
        }
    }
}