namespace IO.Platform.Common.Core.Constants;

/// <summary>
/// Environment variable name constants for IO Platform services
/// Aligns with DX infrastructure auto-generated variables
/// </summary>
public static class EnvironmentVariables
{
    /// <summary>
    /// MongoDB cluster connection string (DX auto-generated)
    /// Pattern: MONGODB__CLUSTER__CONNECTION_STRING
    /// </summary>
    public const string MongoDbConnectionString = "MONGODB__CLUSTER__CONNECTION_STRING";

    /// <summary>
    /// MongoDB cluster server/host name (DX auto-generated)
    /// Pattern: MONGODB__CLUSTER__SERVER
    /// </summary>
    public const string MongoDbServer = "MONGODB__CLUSTER__SERVER";

    /// <summary>
    /// MongoDB cluster TLS certificate file path (DX auto-generated)
    /// Pattern: MONGODB__CLUSTER__TLS_CRT_KEY_FILE
    /// </summary>
    public const string MongoDbTlsCertFile = "MONGODB__CLUSTER__TLS_CRT_KEY_FILE";

    /// <summary>
    /// MongoDB database name (legacy - for migration compatibility)
    /// Pattern: MONGODB_DATABASE_NAME
    /// </summary>
    public const string MongoDbDatabaseName = "MONGODB_DATABASE_NAME";

    /// <summary>
    /// Kafka bootstrap servers
    /// Pattern: KAFKA_BOOTSTRAP_SERVERS
    /// </summary>
    public const string KafkaBootstrapServers = "KAFKA_BOOTSTRAP_SERVERS";

    /// <summary>
    /// OpenTelemetry exporter endpoint
    /// Pattern: OTEL_EXPORTER_OTLP_ENDPOINT
    /// </summary>
    public const string OtelExporterEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// ASP.NET Core environment
    /// Pattern: ASPNETCORE_ENVIRONMENT
    /// </summary>
    public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

    /// <summary>
    /// ASP.NET Core URLs
    /// Pattern: ASPNETCORE_URLS
    /// </summary>
    public const string AspNetCoreUrls = "ASPNETCORE_URLS";

    /// <summary>
    /// SendGrid API key
    /// Pattern: SENDGRID_API_KEY
    /// </summary>
    public const string SendGridApiKey = "SENDGRID_API_KEY";

    /// <summary>
    /// XAuth master token
    /// Pattern: XAUTH_MASTER_TOKEN
    /// </summary>
    public const string XauthMasterToken = "XAUTH_MASTER_TOKEN";

    /// <summary>
    /// Highway API key
    /// Pattern: HIGHWAY_API_KEY
    /// </summary>
    public const string HighwayApiKey = "HIGHWAY_API_KEY";

    /// <summary>
    /// McLeod API key
    /// Pattern: MCLEOD_API_KEY
    /// </summary>
    public const string McLeodApiKey = "MCLEOD_API_KEY";

    /// <summary>
    /// Google Jobs API key
    /// Pattern: GOOGLE_JOBS_API_KEY
    /// </summary>
    public const string GoogleJobsApiKey = "GOOGLE_JOBS_API_KEY";
}
