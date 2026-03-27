using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using SharedEntities;

namespace CodeJitsu.Tests.Helpers;

/// <summary>
/// Module initializer that sets up environment variables and configuration
/// needed by services during tests. Runs once before any test in the assembly.
/// </summary>
public static class TestEnvironmentSetup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Set up Global.Configuration so Global.AccessAppEnvironmentVariable works
        // without requiring real environment variables or appsettings.json
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test_db;Username=test;Password=test",
                ["Jwt:Key"] = "test-jwt-key-that-is-long-enough-for-hmac-sha256-algorithm",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["YOUTUBE_API_KEY"] = "test-youtube-key",
                ["XAIGROK_API_KEY"] = "test-xai-key",
                ["XAIGROK_ENDPOINT"] = "https://api.x.ai/v1/chat/completions",
                ["GoogleCloud:ProjectId"] = "test-project",
                ["GoogleCloud:BucketName"] = "test-bucket",
                ["GoogleCloud:ServiceAccountKeyPath"] = "",
                ["GeminiVision:Location"] = "us-central1",
                ["GeminiVision:Model"] = "gemini-1.5-pro",
                ["GeminiVision:VideoAnalysisPrompt"] = "test-prompt",
                ["ShowSwaggerInProduction"] = "false",
                ["DeleteDbIfExistsOnStartup"] = "false",
                ["ClientAppPorts"] = "3000:80",
                ["Authentication:Google:ClientId"] = "test-client-id",
                ["Authentication:Google:ClientSecret"] = "test-client-secret",
            })
            .Build();

        Global.Configuration = config;
    }
}

