using System.Reflection;
using Asp.Versioning;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Filters;

namespace SampleAspNetReactDockerApp.Server
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
                    ApiKey = configuration["YOUTUBE_API_KEY"],
                    ApplicationName = "YoutubeVideSharingApp"
                });

                return new YoutubeServiceWrapper(youtubeService);
            });
            builder.Services.AddScoped<IYoutubeDataService, YoutubeDataService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opts =>
            {
                opts.EnableAnnotations();

                opts.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "Remitano Video Sharing App",
                    Version = "v1",
                    Description = "Youtube Vid Sharing App built with ASP.NET Core Web API, React and Docker",
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
                        corsBuilder => corsBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                }
                else
                {
                    var allowedOriginPorts = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.ClientAppPorts)
                        .Split(":");

                    var possibleHttpsOrigins = allowedOriginPorts.Select(port => $"https://localhost:{port}").ToArray();
                    var possibleHttpOrigins = allowedOriginPorts.Select(port => $"http://localhost:{port}").ToArray();
                    possibleHttpsOrigins.Append("https://youtubesharingapp.azurewebsites.net");
                    possibleHttpOrigins.Append("http://youtubesharingapp.azurewebsites.net");

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

            builder.Services.AddDbContext<DatabaseContext>();

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            builder.Services.AddIdentityApiEndpoints<AppUserEntity>(opts =>
                {
                    opts.User.RequireUniqueEmail = true;
                    opts.Password.RequiredLength = 8;
                })
                .AddEntityFrameworkStores<DatabaseContext>();

            builder.Services.AddSignalR();

            var app = builder.Build();

            // Initialize the database if it doesn't exist
            await using (var serviceScope = app.Services.CreateAsyncScope())
            {
                await DbHelper.EnsureDbIsCreatedAsync(
                    serviceScope,
                    app.Environment.IsDevelopment() &&
                    Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.DeleteDbIfExistsOnStartup) == "true"
                );
            }

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

            app.MapGroup("/api/auth/v1")
                .MapIdentityApi<AppUserEntity>();

            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // allow any origin
                .AllowCredentials());  // allow credentials

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            await app.RunAsync();
        }
    }
}