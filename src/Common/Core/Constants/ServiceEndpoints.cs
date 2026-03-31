namespace IO.Core.Constants;

/// <summary>
/// Service endpoint constants for downstream service HTTP clients
/// </summary>
public static class ServiceEndpoints
{
    // Service names for HttpClient registration
    public const string CommonService = "common";
    public const string CassService = "cass";
    public const string ElsaService = "elsa";
    public const string LarryService = "larry";
    public const string LeaService = "lea";

    // Base URL configuration keys
    public const string CommonBaseUrlKey = "Services:Common:BaseUrl";
    public const string CassBaseUrlKey = "Services:Cass:BaseUrl";
    public const string ElsaBaseUrlKey = "Services:Elsa:BaseUrl";
    public const string LarryBaseUrlKey = "Services:Larry:BaseUrl";
    public const string LeaBaseUrlKey = "Services:Lea:BaseUrl";

    // API route prefixes
    public const string CommonApiPrefix = "/api/common";
    public const string CassApiPrefix = "/api/cass";
    public const string ElsaApiPrefix = "/api/elsa";
    public const string LarryApiPrefix = "/api/larry";
    public const string LeaApiPrefix = "/api/lea";
}
