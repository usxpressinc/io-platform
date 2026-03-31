# .NET 10 Monorepo Scaffolding Guide

This guide walks through creating the complete io-platform monorepo structure following edi-platform patterns.

## **Phase 1: Repository Foundation**

### **1.1 Create Root Structure**
```bash
# Create main directories
mkdir io-platform
cd io-platform

# Create edi-platform style structure
mkdir -p src/Common/Core
mkdir -p src/Common/Infrastructure  
mkdir -p src/Common/Models/IO.Standard.Types
mkdir -p src/Apps/RestAPI/IO.Proxy
mkdir -p src/Apps/RestAPI/IO.Common
mkdir -p src/Apps/RestAPI/IO.Cass
mkdir -p src/Apps/RestAPI/IO.Elsa
mkdir -p src/Apps/RestAPI/IO.Larry
mkdir -p src/Apps/RestAPI/IO.Lea
mkdir -p src/Apps/Handlers/IO.Larry/VendorHandler
mkdir -p src/Apps/Handlers/IO.Lea/JobsHandler
mkdir -p src/Apps/Jobs/IO.Larry/VendorSync
mkdir -p src/Apps/Jobs/IO.Lea/GoogleJobs
mkdir -p src/Libraries
mkdir -p tests
mkdir -p .octopus/deploy
mkdir -p .github/workflows
mkdir -p scripts
mkdir -p docs
mkdir -p style
```

### **1.2 Create Solution File**
```xml
<!-- io-platform.sln -->
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1

# Common Projects
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Common", "Common", "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "IO.Standard.Types", "src\Common\Models\IO.Standard.Types\IO.Standard.Types.csproj", "{11111111-2222-3333-4444-555555555555}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Core", "src\Common\Core\Core.csproj", "{22222222-3333-4444-5555-666666666666}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Infrastructure", "src\Common\Infrastructure\Infrastructure.csproj", "{33333333-4444-5555-6666-777777777777}"
EndProject

# API Projects
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Apps", "Apps", "{B2C3D4E5-F6G7-8901-BCDE-F23456789012}"
	ProjectSection(SolutionItems) = preProject
		Directory.Build.props = Directory.Build.props
		Directory.Packages.props = Directory.Packages.props
		Directory.Build.targets = Directory.Build.targets
	EndProjectSection
EndProject
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "RestAPI", "RestAPI", "{C3D4E5F6-G7H8-9012-CDEF-345678901234}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "IO.Proxy", "src\Apps\RestAPI\IO.Proxy\IO.Proxy.csproj", "{44444444-5555-6666-7777-888888888888}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "IO.Common", "src\Apps\RestAPI\IO.Common\IO.Common.csproj", "{55555555-6666-7777-8888-999999999999}"
EndProject
# ... additional API projects

# Handler Projects
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Handlers", "Handlers", "{D4E5F6G7-H8I9-0123-DEF4-456789012345}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "IO.Larry.VendorHandler", "src\Apps\Handlers\IO.Larry\VendorHandler\IO.Larry.VendorHandler.csproj", "{66666666-7777-8888-9999-000000000000}"
EndProject
# ... additional handler projects

# Job Projects  
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Jobs", "Jobs", "{E5F6G7H8-I9J0-1234-EF56-789012345678}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "IO.Larry.VendorSync", "src\Apps\Jobs\IO.Larry\VendorSync\IO.Larry.VendorSync.csproj", "{77777777-8888-9999-0000-111111111111}"
EndProject
# ... additional job projects

# Test Projects
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Tests", "Tests", "{F6G7H8I9-J0K1-2345-F678-901234567890}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Tests", "tests\Tests.csproj", "{88888888-9999-0000-1111-222222222222}"
EndProject
```

### **1.3 Create Global Configuration Files**

#### **global.json**
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

#### **Directory.Build.props**
```xml
<Project>
  <!-- Global project and StyleCop Analyzers configuration -->
  <PropertyGroup>
    <Version>1.0.0</Version>
    <Authors>US Xpress Engineering</Authors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)\style\Main.ruleset</CodeAnalysisRuleSet>
    <NoWarn>$(NoWarn);CS1591;SA1600</NoWarn>
  </PropertyGroup>

  <!-- Style -->
  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" PrivateAssets="all" />
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)\style\stylecop.json" Link="stylecop.json" Visible="false"/>
    <None Include="$(CodeAnalysisRuleSet)" Condition="'$(CodeAnalysisRuleSet)' != ''" Link="%(Filename)%(Extension)" Visible="false"/>
  </ItemGroup>
</Project>
```

#### **Directory.Packages.props**
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup Label="USXpress">
    <PackageVersion Include="USXpress.Api.Common" Version="0.1.13" />
    <PackageVersion Include="USXpress.Configuration.Mongo" Version="0.2.8" />
    <PackageVersion Include="USXpress.Kafka" Version="0.0.5" />
    <PackageVersion Include="USXpress.Monitoring" Version="3.1.10" />
    <PackageVersion Include="USXpress.Standard.Types" Version="1.3.5" />
    <PackageVersion Include="USXpress.Standard.Types.Common" Version="0.1.10" />
  </ItemGroup>
  
  <ItemGroup Label="Microsoft">
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.11" />
    <PackageVersion Include="Microsoft.Extensions.Hosting.Abstractions" Version="9.0.11" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.2.0" />
  </ItemGroup>
  
  <ItemGroup Label="ThirdParty">
    <PackageVersion Include="MongoDB.Bson" Version="2.30.0" />
    <PackageVersion Include="MongoDB.Driver" Version="2.28.0" />
    <PackageVersion Include="FluentValidation" Version="11.11.0" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="7.3.1" />
    <PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
  </ItemGroup>
  
  <ItemGroup Label="Test Libraries">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>
</Project>
```

#### **nuget.config**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="github_usx" value="https://nuget.pkg.github.com/usxpressinc/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <package key="USXpress.*" source="github_usx" />
    <package key="*" source="nuget.org" />
  </packageSourceMapping>
</configuration>
```

## **Phase 2: Common Libraries**

### **2.1 Models Library - IO.Standard.Types**

#### **src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="USXpress.Standard.Types" />
    <PackageReference Include="USXpress.Standard.Types.Common" />
  </ItemGroup>
</Project>
```

#### **Create Domain Models**
```csharp
// src/Common/Models/IO.Standard.Types/Email/
// SendEmailRequest.cs
namespace IO.Standard.Types.Email;

public class SendEmailRequest
{
    public string Body { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmails { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string CcEmails { get; set; } = string.Empty;
    public string BccEmails { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? FromName { get; set; }
}

// src/Common/Models/IO.Standard.Types/Email/
// SendEmailResponse.cs
namespace IO.Standard.Types.Email;

public class SendEmailResponse
{
    public string Status { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

// src/Common/Models/IO.Standard.Types/CarrierVetting/
// CarrierValidityRequest.cs
namespace IO.Standard.Types.CarrierVetting;

public class CarrierValidityRequest
{
    public string? DotNumber { get; set; }
    public string? McNumber { get; set; }
    public string? BrokerageOrderId { get; set; }
}

// ... additional domain models
```

### **2.2 Core Library - Business Logic & Interfaces**

#### **src/Common/Core/Core.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="USXpress.Configuration.Mongo" />
    <PackageReference Include="USXpress.Monitoring" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../Models/IO.Standard.Types/IO.Standard.Types.csproj" />
  </ItemGroup>
</Project>
```

#### **Create Core Interfaces**
```csharp
// src/Common/Core/Interfaces/
// IEmailService.cs
namespace IO.Core.Interfaces;

public interface IEmailService
{
    Task<Email.SendEmailResponse> SendEmailAsync(Email.SendEmailRequest request);
}

// src/Common/Core/Interfaces/
// ICarrierVettingService.cs
namespace IO.Core.Interfaces;

public interface ICarrierVettingService
{
    Task<CarrierVetting.CarrierValidityResponse> GetCarrierValidityAsync(
        CarrierVetting.CarrierValidityRequest request);
}

// ... additional interfaces
```

#### **Create Constants**
```csharp
// src/Common/Core/Constants/
// MongoDbCollections.cs
namespace IO.Core.Constants;

public class MongoDbCollections
{
    public const string VendorCollection = $"{Prefix}:VendorCollection";
    public const string ContextCollection = $"{Prefix}:ContextCollection";
    public const string CarrierCollection = $"{Prefix}:CarrierCollection";
    public const string PricingCollection = $"{Prefix}:PricingCollection";
    public const string JobCollection = $"{Prefix}:JobCollection";
    private const string Prefix = "Database";
}

// src/Common/Core/Constants/
// MongoDbDatabases.cs
namespace IO.Core.Constants;

public class MongoDbDatabases
{
    public const string MainDatabaseName = $"{Prefix}:MainDatabaseName";
    private const string Prefix = "Database";
}

// src/Common/Core/Constants/
// MongoDbClusterOptions.cs
namespace IO.Core.Constants;

public class MongoDbClusterOptions
{
    public const string ConnectionString = $"{Prefix}:CONNECTION_STRING";
    public const string TlsCrtKeyFile = $"{Prefix}:TLS_CRT_KEY_FILE";
    private const string Prefix = "MONGODB:CLUSTER";
}

// src/Common/Core/Constants/
// Constants Pattern
namespace IO.Core.Constants;

public class MongoDbCollections
{
    public const string VendorCollection = $"{Prefix}:VendorCollection";
    public const string ContextCollection = $"{Prefix}:ContextCollection";
    public const string CarrierCollection = $"{Prefix}:CarrierCollection";
    public const string PricingCollection = $"{Prefix}:PricingCollection";
    public const string JobCollection = $"{Prefix}:JobCollection";
    private const string Prefix = "Database";
}

public class MongoDbDatabases
{
    public const string MainDatabaseName = $"{Prefix}:MainDatabaseName";
    private const string Prefix = "Database";
}

public class MongoDbClusterOptions
{
    public const string ConnectionString = $"{Prefix}:CONNECTION_STRING";
    public const string TlsCrtKeyFile = $"{Prefix}:TLS_CRT_KEY_FILE";
    private const string Prefix = "MONGODB:CLUSTER";
}

public class KafkaTopics
{
    public const string VendorLookupEvent = $"{Prefix}:vendor_lookup_evt";
    public const string JobSearchEvent = $"{Prefix}:job_search_evt";
    public const string EmailNotificationEvent = $"{Prefix}:email_notification_evt";
    private const string Prefix = "IO";
}

public class KafkaGroups
{
    public const string VendorProcessorGroup = $"{Prefix}:vendor-processor";
    public const string JobProcessorGroup = $"{Prefix}:job-processor";
    private const string Prefix = "io-platform";
}

public class EnvironmentVariables
{
    public const string MongoConnectionString = "MONGODB__CLUSTER__CONNECTION_STRING";
    public const string MongoTlsFile = "MONGODB__CLUSTER__TLS_CRT_KEY_FILE";
    public const string KafkaBootstrapServer = "KAFKA__bootstrap_server";
    public const string KafkaApiKey = "KAFKA__api_key";
    public const string KafkaApiSecret = "KAFKA__api_secret";
    public const string SendGridApiKey = "EMAIL_SendgridKey";
    public const string HighwayApiKey = "HIGHWAY_API_KEY";
    public const string McleodUrl = "MCLEOD_BASE_URL";
    public const string ApplicationProject = "APPLICATION__PROJECT";
    public const string ApplicationGroup = "APPLICATION__GROUP";
    public const string ApplicationEnvironment = "APPLICATION__ENVIRONMENT";
}

public class ServiceEndpoints
{
    public const string CommonService = "io-common";
    public const string CassService = "io-cass";
    public const string ElsaService = "io-elsa";
    public const string LarryService = "io-larry";
    public const string LeaService = "io-lea";
}

public class AuthenticationScopes
{
    public const string CommonScope = "common";
    public const string CassScope = "clara";
    public const string ElsaScope = "elsa";
    public const string LarryScope = "larry";
    public const string LeaScope = "lea";
}
```

### **2.3 Infrastructure Library - External Integrations**

#### **src/Common/Infrastructure/Infrastructure.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="USXpress.Configuration.Mongo" />
    <PackageReference Include="USXpress.Kafka" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../Core/Core.csproj" />
    <ProjectReference Include="../Models/IO.Standard.Types/IO.Standard.Types.csproj" />
  </ItemGroup>
</Project>
```

#### **Create Repository Extensions**
```csharp
// src/Common/Infrastructure/Mongo/
// MongoExtensions.cs
using IO.Core.Constants;
using USXpress.Configuration.Mongo;

namespace IO.Infrastructure.Mongo;

public static class MongoExtensions
{
    public static IHostApplicationBuilder AddMongoRepositories(
        this IHostApplicationBuilder builder)
    {
        builder.UseDefaultMongoConventions();
        return builder
            .AddVendorRepository()
            .AddContextRepository()
            .AddCarrierRepository()
            .AddPricingRepository()
            .AddJobRepository();
    }

    public static IHostApplicationBuilder AddVendorRepository(
        this IHostApplicationBuilder builder)
    {
        var config = new MongoDbConfig
        {
            ConnectionString = builder.Configuration[EnvironmentVariables.MongoConnectionString]!,
            DatabaseName = builder.Configuration[MongoDbDatabases.MainDatabaseName]!,
            CollectionName = builder.Configuration[MongoDbCollections.VendorCollection]!,
            MaxConnectionPoolSize = builder.Configuration["Database:MaxConnectionPoolSize"],
            TlsCertFile = builder.Configuration[EnvironmentVariables.MongoTlsFile]
        };

        builder.Services.AddMongoRepository<Vendor>(config);
        return builder;
    }

    // ... additional repository methods using constants
}

// src/Common/Infrastructure/Kafka/
// KafkaExtensions.cs
using IO.Core.Constants;
using USXpress.Kafka;

namespace IO.Infrastructure.Kafka;

public static class KafkaExtensions
{
    public static IHostApplicationBuilder AddKafkaConsumers(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddUSXpressKafkaConsumer(builder.Configuration);
        
        // Register consumer services
        builder.Services.AddHostedService<VendorLookupConsumer>();
        builder.Services.AddHostedService<JobSearchConsumer>();
        
        return builder;
    }
}

// src/Apps/Handlers/IO.Larry/VendorHandler/VendorLookupConsumer.cs
using IO.Core.Constants;

namespace IO.Larry.VendorHandler;

public class VendorLookupConsumer : BackgroundService
{
    private readonly IKafkaConsumer _kafkaConsumer;
    private readonly IVendorService _vendorService;

    public VendorLookupConsumer(
        IKafkaConsumer kafkaConsumer,
        IVendorService vendorService)
    {
        _kafkaConsumer = kafkaConsumer;
        _vendorService = vendorService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _kafkaConsumer.ConsumeAsync(
            KafkaTopics.VendorLookupEvent, 
            async (message, token) =>
            {
                await _vendorService.ProcessVendorLookupAsync(message, token);
            }, 
            stoppingToken);
    }
}

// src/Apps/RestAPI/IO.Proxy/Controllers/ProxyController.cs
using IO.Core.Constants;

namespace IO.Proxy.Controllers;

[ApiController]
[Route("api")]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("common/email")]
    public async Task<IActionResult> SendEmail([FromBody] Email.SendEmailRequest request)
    {
        var client = _httpClientFactory.CreateClient(ServiceEndpoints.CommonService);
        var response = await client.PostAsJsonAsync("/api/email", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Email.SendEmailResponse>();
            return Ok(result);
        }
        
        return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    // ... additional endpoints using ServiceEndpoints constants
}
```

## **Phase 3: API Projects**

### **3.1 Proxy API - Gateway**

#### **src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
    <PackageReference Include="USXpress.Monitoring" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../../Common/Core/Core.csproj" />
    <ProjectReference Include="../../../Common/Infrastructure/Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

#### **Program.cs**
```csharp
using USXpress.Monitoring;
using USXpress.Monitoring.Models;
using IO.Infrastructure.Mongo;

var builder = WebApplication.CreateBuilder(args);

// Application metadata from configuration
var configuration = builder.Configuration;
var environment = Enum.Parse<MonitoringEnvironment>(
    configuration.GetValue<string>("APPLICATION:ENVIRONMENT") ?? "development",
    ignoreCase: true);
var project = configuration.GetValue<string>("APPLICATION:PROJECT") ?? "io-platform";
var group = configuration.GetValue<string>("APPLICATION:GROUP") ?? "gateway";

// Configure Serilog with console output
var loggingConfiguration = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

// Add monitoring (Grafana/OTEL)
builder.AddMonitoring(new MonitoringOptions
{
    ProjectGroup = group,
    ProjectName = project,
    Environment = environment,
    ReleaseVersion = configuration.GetValue<string>("REVISION") ?? "1.0.0",
    EnableOtel = true,
    SerilogLoggerConfiguration = loggingConfiguration,
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP clients for downstream services
builder.Services.AddHttpClient("io-common", client =>
{
    client.BaseAddress = new Uri(configuration["Services:Common:BaseUrl"]!);
});

builder.Services.AddHttpClient("io-cass", client =>
{
    client.BaseAddress = new Uri(configuration["Services:Cass:BaseUrl"]!);
});

// ... additional HTTP clients

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### **Controllers/ProxyController.cs**
```csharp
namespace IO.Proxy.Controllers;

[ApiController]
[Route("api")]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IHttpClientFactory httpClientFactory, ILogger<ProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("common/email")]
    public async Task<IActionResult> SendEmail([FromBody] Email.SendEmailRequest request)
    {
        var client = _httpClientFactory.CreateClient("io-common");
        var response = await client.PostAsJsonAsync("/api/email", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Email.SendEmailResponse>();
            return Ok(result);
        }
        
        return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [HttpPost("cass/carriers/valid")]
    public async Task<IActionResult> ValidateCarrier([FromBody] CarrierVetting.CarrierValidityRequest request)
    {
        var client = _httpClientFactory.CreateClient("io-cass");
        var response = await client.PostAsJsonAsync("/api/carriers/valid", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<CarrierVetting.CarrierValidityResponse>();
            return Ok(result);
        }
        
        return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    // ... additional proxy endpoints
}
```

### **3.2 Common API - Email & Context**

#### **src/Apps/RestAPI/IO.Common/IO.Common.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
    <PackageReference Include="USXpress.Monitoring" />
    <PackageReference Include="USXpress.Configuration.Mongo" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../../Common/Core/Core.csproj" />
    <ProjectReference Include="../../../Common/Infrastructure/Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

#### **Program.cs**
```csharp
using USXpress.Monitoring;
using USXpress.Monitoring.Models;
using IO.Infrastructure.Mongo;
using IO.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add monitoring (same pattern as proxy)
// ... monitoring setup code ...

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MongoDB repositories
builder.AddMongoRepositories();

// Register application services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IContextService, ContextService>();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### **Services/EmailService.cs**
```csharp
using IO.Core.Interfaces;
using IO.Standard.Types.Email;

namespace IO.Common.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<SendEmailResponse> SendEmailAsync(SendEmailRequest request)
    {
        // Implement SendGrid integration
        // This would use the SendGrid API similar to the Python implementation
        
        _logger.LogInformation("Sending email to {ToEmails} with subject {Subject}", 
            request.ToEmails, request.Subject);

        // TODO: Implement SendGrid API call
        // For now, return success response
        
        return new SendEmailResponse
        {
            Status = "200",
            Errors = new List<string>()
        };
    }
}
```

## **Phase 4: Handler & Job Projects**

### **4.1 Larry Vendor Handler**

#### **src/Apps/Handlers/IO.Larry/VendorHandler/IO.Larry.VendorHandler.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="USXpress.Monitoring" />
    <PackageReference Include="USXpress.Kafka" />
    <PackageReference Include="USXpress.Configuration.Mongo" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../../../../Common/Core/Core.csproj" />
    <ProjectReference Include="../../../../Common/Infrastructure/Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

#### **Program.cs**
```csharp
using USXpress.Monitoring;
using USXpress.Monitoring.Models;
using IO.Infrastructure.Mongo;
using USXpress.Kafka;

var builder = Host.CreateApplicationBuilder(args);

// Add monitoring
// ... monitoring setup code ...

// Add MongoDB repositories
builder.AddMongoRepositories();

// Add Kafka consumer
builder.Services.AddUSXpressKafkaConsumer(builder.Configuration);

// Register background service
builder.Services.AddHostedService<VendorLookupWorker>();

var host = builder.Build();
host.Run();
```

#### **VendorLookupWorker.cs**
```csharp
namespace IO.Larry.VendorHandler;

public class VendorLookupWorker : BackgroundService
{
    private readonly ILogger<VendorLookupWorker> _logger;
    private readonly IKafkaConsumer _kafkaConsumer;
    private readonly IVendorService _vendorService;

    public VendorLookupWorker(
        ILogger<VendorLookupWorker> logger,
        IKafkaConsumer kafkaConsumer,
        IVendorService vendorService)
    {
        _logger = logger;
        _kafkaConsumer = kafkaConsumer;
        _vendorService = vendorService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _kafkaConsumer.ConsumeAsync("vendor_lookup_evt", async (message, token) =>
        {
            _logger.LogInformation("Processing vendor lookup request: {Message}", message);
            
            // Process vendor lookup
            await _vendorService.ProcessVendorLookupAsync(message, token);
        }, stoppingToken);
    }
}
```

## **Phase 5: Deployment Configuration**

### **5.1 Create Deployment YAMLs**

#### **.octopus/deploy/io-proxy-api.yaml**
```yaml
---
name: io-proxy-api
octopus:
  space: USXpress
  group: gateway
tags:
  owner: USXpress
  team: Platform
  purpose: API Gateway for IO services
infrastructure:
  auth:
    roles:
      - io-proxy-reader
      - io-proxy-writer
    group_roles_assignment:
      - name: Everybody
        roles:
          - io-proxy-reader
          - io-proxy-writer
    redirect_uri_paths:
      - path: /signin-oidc
        type: api
      - path: /swagger/oauth2-redirect.html
        type: spa
api:
  routes:
    subdomains:
      - product: io
        type: api
  global:
    upstream:
      perTryTimeout: 15s
  enabled: true
  service:
    targetPort: 8080
  configVars:
    APPLICATION_ENTRYPOINT: IO.Proxy.dll
    Serilog__MinimumLevel__Default: Information
    Services__Common__BaseUrl: '#{Services__Common__BaseUrl}'
    Services__Cass__BaseUrl: '#{Services__Cass__BaseUrl}'
    Services__Elsa__BaseUrl: '#{Services__Elsa__BaseUrl}'
    Services__Larry__BaseUrl: '#{Services__Larry__BaseUrl}'
    Services__Lea__BaseUrl: '#{Services__Lea__BaseUrl}'
  secretVars:
    AUTH__CLIENT_SECRET: '#{AUTH__CLIENT_SECRET}'
```

### **5.2 Create GitHub Actions**

#### **.github/workflows/build.yml**
```yaml
---
name: Build & Deploy

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  MASTER_BRANCH: main

on:
  push:
    paths:
      - '.octopus/deploy/**'
      - '.github/workflows/build.yaml'
      - 'src/**'

jobs:
  build:
    name: Build and Deploy
    runs-on: ubuntu-latest
    
    permissions:
      id-token: write
      contents: write
      
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0
          
      - name: Artifact 📦
        uses: variant-inc/actions-dotnet@v2
        with:
          dotnet-version: 10.0.x
          ecr_repository: usxpress/io-platform
          
      - name: Release 🛸
        uses: variant-inc/actions-octopus@v3
        with:
          deploy_yaml_dir: .octopus/deploy
```

### **5.3 Create Dockerfile**

#### **Dockerfile**
```dockerfile
# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim AS restore
ARG GITHUB_TOKEN
ARG GITHUB_USER

WORKDIR /app

# Copy NuGet configuration and build props
COPY nuget.config .
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["io-platform.sln", "."]

# Copy ALL project files for restore
COPY ["src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj", "src/Common/Models/IO.Standard.Types/"]
COPY ["src/Common/Core/Core.csproj", "src/Common/Core/"]
COPY ["src/Common/Infrastructure/Infrastructure.csproj", "src/Common/Infrastructure/"]
COPY ["src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj", "src/Apps/RestAPI/IO.Proxy/"]
COPY ["src/Apps/RestAPI/IO.Common/IO.Common.csproj", "src/Apps/RestAPI/IO.Common/"]
COPY ["src/Apps/RestAPI/IO.Cass/IO.Cass.csproj", "src/Apps/RestAPI/IO.Cass/"]
COPY ["src/Apps/RestAPI/IO.Elsa/IO.Elsa.csproj", "src/Apps/RestAPI/IO.Elsa/"]
COPY ["src/Apps/RestAPI/IO.Larry/IO.Larry.csproj", "src/Apps/RestAPI/IO.Larry/"]
COPY ["src/Apps/RestAPI/IO.Lea/IO.Lea.csproj", "src/Apps/RestAPI/IO.Lea/"]
COPY ["src/Apps/Handlers/IO.Larry/VendorHandler/IO.Larry.VendorHandler.csproj", "src/Apps/Handlers/IO.Larry/VendorHandler/"]
COPY ["src/Apps/Handlers/IO.Lea/JobsHandler/IO.Lea.JobsHandler.csproj", "src/Apps/Handlers/IO.Lea/JobsHandler/"]
COPY ["src/Apps/Jobs/IO.Larry/VendorSync/IO.Larry.VendorSync.csproj", "src/Apps/Jobs/IO.Larry/VendorSync/"]
COPY ["src/Apps/Jobs/IO.Lea/GoogleJobs/IO.Lea.GoogleJobs.csproj", "src/Apps/Jobs/IO.Lea/GoogleJobs/"]

# Copy test projects, only for restore
COPY ["tests/Tests.csproj", "tests/"]

# Restore all dependencies in one command
ENV NUGET_XMLDOC_MODE=none
RUN echo ">>> Restoring NuGet packages..." && \
    dotnet restore io-platform.sln /p:WarningLevel=0

# Stage 2: Build Common/Shared projects
FROM restore AS build-common

# Copy style folder for StyleCop analyzers
COPY style/ style/

# Copy ONLY the Common source code
COPY src/Common/ src/Common/

# Build the shared libraries
RUN echo ">>> Building Common/Models..." && \
    dotnet build "src/Common/Models/IO.Standard.Types/IO.Standard.Types.csproj" -c Release --no-restore && \
    echo ">>> Building Common/Core..." && \
    dotnet build "src/Common/Core/Core.csproj" -c Release --no-restore && \
    echo ">>> Building Common/Infrastructure..." && \
    dotnet build "src/Common/Infrastructure/Infrastructure.csproj" -c Release --no-restore

# Stage 3: Build and Publish Apps
FROM build-common AS publish

# Copy all App source code
COPY src/Apps/ src/Apps/

# Build and publish all applications
WORKDIR /app
RUN echo ">>> Publishing IO.Proxy..." && \
    dotnet publish "src/Apps/RestAPI/IO.Proxy/IO.Proxy.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Common..." && \
    dotnet publish "src/Apps/RestAPI/IO.Common/IO.Common.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Cass..." && \
    dotnet publish "src/Apps/RestAPI/IO.Cass/IO.Cass.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Elsa..." && \
    dotnet publish "src/Apps/RestAPI/IO.Elsa/IO.Elsa.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Larry..." && \
    dotnet publish "src/Apps/RestAPI/IO.Larry/IO.Larry.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Lea..." && \
    dotnet publish "src/Apps/RestAPI/IO.Lea/IO.Lea.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Larry.VendorHandler..." && \
    dotnet publish "src/Apps/Handlers/IO.Larry/VendorHandler/IO.Larry.VendorHandler.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Lea.JobsHandler..." && \
    dotnet publish "src/Apps/Handlers/IO.Lea/JobsHandler/IO.Lea.JobsHandler.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Larry.VendorSync..." && \
    dotnet publish "src/Apps/Jobs/IO.Larry/VendorSync/IO.Larry.VendorSync.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0 && \
    echo ">>> Publishing IO.Lea.GoogleJobs..." && \
    dotnet publish "src/Apps/Jobs/IO.Lea/GoogleJobs/IO.Lea.GoogleJobs.csproj" -c Release -o /app/publish --no-restore /p:WarningLevel=0

# Stage 4: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS final

# Runtime dependencies
RUN apt-get update -y \
    && apt-get install -y \
    tzdata ca-certificates dumb-init \
    libicu-dev krb5-user openssl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/* \
    && sed -i 's/\[openssl_init\]/# [openssl_init]/' /etc/ssl/openssl.cnf \
    && printf "\n\n[openssl_init]\nssl_conf = ssl_sect" >> /etc/ssl/openssl.cnf \
    && printf "\n\n[ssl_sect]\nsystem_default = ssl_default_sect" >> /etc/ssl/openssl.cnf \
    && printf "\n\n[ssl_default_sect]\nMinProtocol = TLSv1\nCipherString = DEFAULT@SECLEVEL=0\n" >> /etc/ssl/openssl.cnf

# Copy startup script
COPY scripts/startup.sh /startup.sh
RUN chmod 755 /startup.sh

# Copy all appsettings for various entrypoints
RUN mkdir -p /appSettings && chown 1000:1000 /appSettings
COPY src/Apps/RestAPI/IO.Proxy/appsettings.json /appSettings/IO.Proxy.dll.json
COPY src/Apps/RestAPI/IO.Common/appsettings.json /appSettings/IO.Common.dll.json
COPY src/Apps/RestAPI/IO.Cass/appsettings.json /appSettings/IO.Cass.dll.json
COPY src/Apps/RestAPI/IO.Elsa/appsettings.json /appSettings/IO.Elsa.dll.json
COPY src/Apps/RestAPI/IO.Larry/appsettings.json /appSettings/IO.Larry.dll.json
COPY src/Apps/RestAPI/IO.Lea/appsettings.json /appSettings/IO.Lea.dll.json
COPY src/Apps/Handlers/IO.Larry/VendorHandler/appsettings.json /appSettings/IO.Larry.VendorHandler.dll.json
COPY src/Apps/Handlers/IO.Lea/JobsHandler/appsettings.json /appSettings/IO.Lea.JobsHandler.dll.json
COPY src/Apps/Jobs/IO.Larry/VendorSync/appsettings.json /appSettings/IO.Larry.VendorSync.dll.json
COPY src/Apps/Jobs/IO.Lea/GoogleJobs/appsettings.json /appSettings/IO.Lea.GoogleJobs.dll.json
RUN chown 1000:1000 /appSettings/*.json

# Set user permissions and working directory
RUN mkdir -p /app/log && chown -R 1000:1000 /app
USER 1000
WORKDIR /app/log

# Set environment variables for profiling
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8
ENV DOTNET_gcServer=1
ENV DOTNET_GCDynamicAdaptationMode=1

# Final working directory and copy published files
WORKDIR /app
COPY --chown=1000:1000 --from=publish /app/publish .

# Define the entry point for the application
ENTRYPOINT ["../startup.sh"]
```

#### **scripts/startup.sh**
```bash
#!/bin/bash

# Get the entrypoint from environment variable or default to IO.Proxy
ENTRYPOINT=${APPLICATION_ENTRYPOINT:-IO.Proxy.dll}

echo "Starting application: $ENTRYPOINT"

# Start the application
exec dotnet "$ENTRYPOINT"
```

## **Phase 6: Development Workflow**

### **6.1 Local Development Setup**
```bash
# Clone and setup
git clone <repository-url>
cd io-platform

# Install .NET 10 SDK
dotnet --version

# Restore packages
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Run specific API locally
cd src/Apps/RestAPI/IO.Proxy
dotnet run
```

### **6.2 Adding New Services**
When adding new services to the monorepo:

1. **Create project structure** following existing patterns
2. **Add to solution file** with appropriate GUID
3. **Update Directory.Packages.props** if new dependencies needed
4. **Create deployment YAML** in `.octopus/deploy/`
5. **Update Dockerfile** to include new project in publish stage
6. **Add HTTP client** in Proxy for downstream communication
7. **Update startup.sh** with new appsettings.json

### **6.3 Testing Strategy**
```bash
# Unit tests for each project
dotnet test src/Common/Core/tests/
dotnet test src/Common/Infrastructure/tests/
dotnet test src/Apps/RestAPI/IO.Proxy/tests/

# Integration tests
dotnet test tests/Integration/

# End-to-end tests
dotnet test tests/E2E/
```

This scaffolding guide provides the complete foundation for the .NET 10 monorepo migration, following edi-platform patterns and USXpress DX standards.
