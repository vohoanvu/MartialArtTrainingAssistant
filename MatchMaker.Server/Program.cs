using System.Reflection;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MatchMaker.Server.Helpers;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Filters;
using SharedEntities;
using SharedEntities.Data;
using Microsoft.AspNetCore.HttpOverrides;

namespace MatchMaker.Server
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
            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opts =>
            {
                opts.EnableAnnotations();

                opts.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "Match Maker service",
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
                        corsBuilder => corsBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

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

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            //builder.Services.AddIdentityApiEndpoints<AppUserEntity>(opts =>
            //{
            //    opts.User.RequireUniqueEmail = true;
            //    opts.Password.RequiredLength = 8;
            //})
            //.AddEntityFrameworkStores<MyDatabaseContext>();

            //builder.Services.AddSignalR();

            var app = builder.Build();
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                // Forward the X-Forwarded-For (client IP) and X-Forwarded-Proto (protocol, e.g., https) headers.
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownProxies.Clear();
            forwardedHeadersOptions.KnownNetworks.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);
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

            app.UseCors("AllowReactApp");

            //app.MapGroup("/api/auth/v1")
            //    .MapIdentityApi<AppUserEntity>();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            if (builder.Environment.IsDevelopment()) 
            {
                await app.RunAsync();
            } 
            else 
            {
                await app.RunAsync("http://0.0.0.0:7082");
            }
        }
    }
}