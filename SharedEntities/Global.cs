using Microsoft.Extensions.Configuration;

namespace SharedEntities;

/// <summary>
/// Global class for accessing environment variables and other global settings
/// </summary>
public static class Global
{
    /// <summary>
    /// Configuration property for accessing environment variables
    /// </summary>
    public static IConfiguration? Configuration { get; set; }

    /// <summary>
    /// Returns true if the application is running in a container
    /// </summary>
    public static bool RunsInContainer => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    /// <summary>
    /// Accesses an environment variable
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string AccessAppEnvironmentVariable(AppEnvironmentVariables variable)
    {
        switch (variable)
        {
            case AppEnvironmentVariables.AppDb:

                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("ASPNETCORE_APP_DB");

                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }

                return Configuration?["ConnectionStrings:AppDb"]
                       ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.ClientAppPorts:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("CLIENT_APP_PORTS");

                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["ClientAppPorts"]
                       ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.ShowSwaggerInProduction:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("ASPNETCORE_SHOW_SWAGGER_IN_PRODUCTION");

                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["ShowSwaggerInProduction"]
                       ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.DeleteDbIfExistsOnStartup:
                return Configuration?["DeleteDbIfExistsOnStartup"]
                       ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.YoutubeApiKey:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");

                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["YOUTUBE_API_KEY"]
                       ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.GoogleCloudProjectId:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT_ID");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["GoogleCloud:ProjectId"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.GoogleCloudBucketName:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_BUCKET_NAME");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["GoogleCloud:BucketName"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.GoogleCloudServiceAccountKeyPath:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GoogleCloud__ServiceAccountKeyPath");
                    if (!string.IsNullOrEmpty(possibleValue))
                    {
                        Console.WriteLine($"Using GoogleCloud__ServiceAccountKeyPath: {possibleValue}");
                        if (!File.Exists(possibleValue))
                        {
                            Console.WriteLine($"Service account key file does not exist at: {possibleValue}");
                            throw new FileNotFoundException($"Service account key file does not exist at: {possibleValue}");
                        }
                        return possibleValue;
                    }
                }
                var configValue = Configuration?["GoogleCloud:ServiceAccountKeyPath"];
                if (!string.IsNullOrEmpty(configValue))
                {
                    Console.WriteLine($"Using GoogleCloud:ServiceAccountKeyPath from configuration: {configValue}");
                    if (!File.Exists(configValue))
                    {
                        Console.WriteLine($"Service account key file does not exist at: {configValue}");
                        throw new FileNotFoundException($"Service account key file does not exist at: {configValue}");
                    }
                    return configValue;
                }
                Console.WriteLine($"Environment variable {variable} not found");
                throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.GeminiVisionLocation:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GEMINI_VISION_LOCATION");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["GeminiVision:Location"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.GeminiVisionModel:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GEMINI_VISION_MODEL");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["GeminiVision:Model"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.GeminiVisionVideoAnalysisPrompt:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GEMINI_VISION_VIDEO_ANALYSIS_PROMPT");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["GeminiVision:VideoAnalysisPrompt"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.JwtAudience:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["Jwt:Audience"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.JwtIssuer:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("JWT_ISSUER");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["Jwt:Issuer"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.JwtKey:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("JWT_KEY");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["Jwt:Key"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.AuthenticationGoogleClientId:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["Authentication:Google:ClientId"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.AuthenticationGoogleClientSecret:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["Authentication:Google:ClientSecret"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.XAIGrokApiKey:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("XAIGROK_API_KEY");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["XAIGROK_API_KEY"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            case AppEnvironmentVariables.XAIGrokEndpoint:
                if (RunsInContainer)
                {
                    var possibleValue = Environment.GetEnvironmentVariable("XAIGROK_ENDPOINT");
                    if (!string.IsNullOrEmpty(possibleValue))
                        return possibleValue;
                }
                return Configuration?["XAIGROK_ENDPOINT"]
                    ?? throw new InvalidOperationException($"Environment variable {variable} not found");
            default:
                throw new ArgumentOutOfRangeException(nameof(variable), variable, null);
        }
    }
}

/// <summary>
/// Enumeration of environment variables used by the application
/// </summary>
public enum AppEnvironmentVariables
{
    /// <summary>
    /// The connection string for the application database
    /// </summary>
    /// <example>
    /// Server=localhost;Database=AppDb;User Id=sa;Password=your_password...
    /// </example>
    AppDb,
    /// <summary>
    /// The ports used by the client application
    /// </summary>
    /// <example>
    /// 3000:80
    /// </example>
    ClientAppPorts,
    /// <summary>
    /// The environment variable that determines whether to show Swagger in production
    /// </summary>
    /// <example>
    /// true
    /// </example>
    ShowSwaggerInProduction,
    /// <summary>
    /// The environment variable that determines whether to delete the database if it exists on startup
    /// </summary>
    DeleteDbIfExistsOnStartup,
    /// <summary>
    /// The environment variable for external Youtube Data Service API key
    /// </summary>
    YoutubeApiKey,
    /// <summary>
    /// The Google Cloud Project ID
    /// </summary>
    GoogleCloudProjectId,
    /// <summary>
    /// The Google Cloud Storage Bucket Name
    /// </summary>
    GoogleCloudBucketName,
    /// <summary>
    /// The Google Cloud Service Account Key Path
    /// </summary>
    GoogleCloudServiceAccountKeyPath,
    /// <summary>
    /// The Gemini Vision Location
    /// </summary>
    GeminiVisionLocation,
    /// <summary>
    /// The Gemini Vision Model
    /// </summary>
    GeminiVisionModel,
    /// <summary>
    /// The Gemini Vision Video Analysis Prompt
    /// </summary>
    GeminiVisionVideoAnalysisPrompt,
    /// <summary>
    /// JWT Audience
    /// </summary>
    JwtAudience,
    /// <summary>
    /// JWT Issuer
    /// </summary>
    JwtIssuer,
    /// <summary>
    /// JWT Key
    /// </summary>
    JwtKey,
    /// <summary>
    /// Google OAuth ClientId
    /// </summary>
    AuthenticationGoogleClientId,
    /// <summary>
    /// Google OAuth ClientSecret
    /// </summary>
    AuthenticationGoogleClientSecret,
    XAIGrokApiKey,
    XAIGrokEndpoint,
}