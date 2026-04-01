# Quick Start Script for IO Platform
Write-Host "🚀 Starting IO Platform Quick Start..." -ForegroundColor Green

# Step 1: Stop any existing containers
Write-Host "📋 Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml down 2>$null
docker-compose down 2>$null

# Step 2: Start infrastructure only
Write-Host "🏗️ Starting infrastructure services..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml up -d mongodb kafka zookeeper otel-collector prometheus grafana

# Wait for infrastructure to be ready
Write-Host "⏳ Waiting for infrastructure to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Check infrastructure status
Write-Host "✅ Checking infrastructure status..." -ForegroundColor Green
$infraStatus = docker-compose -f docker-compose.dev.yml ps mongodb kafka zookeeper otel-collector prometheus grafana
Write-Host $infraStatus

# Step 3: Create simple test services
Write-Host "🔧 Creating simple test services..." -ForegroundColor Yellow

# Create a simple web API for each service
$services = @(
    @{Name="IO.Proxy"; Port=8080; Description="API Gateway"},
    @{Name="IO.Common"; Port=8081; Description="Common Services"},
    @{Name="IO.Cass"; Port=8082; Description="Carrier Vetting"},
    @{Name="IO.Elsa"; Port=8083; Description="Pricing Service"},
    @{Name="IO.Larry"; Port=8084; Description="Vendor Management"},
    @{Name="IO.Lea"; Port=8085; Description="Job Processing"}
)

foreach ($service in $services) {
    $serviceName = $service.Name.ToLower().replace(".", "")
    
    Write-Host "Creating $($service.Description) on port $($service.Port)..." -ForegroundColor Cyan
    
    # Create simple service directory
    $serviceDir = "simple-services/$serviceName"
    if (!(Test-Path $serviceDir)) {
        New-Item -ItemType Directory -Path $serviceDir -Force
    }
    
    # Create simple Program.cs
    $programContent = @"
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => new { 
    status = "healthy", 
    service = "$($service.Name)", 
    description = "$($service.Description)",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
});

app.MapGet("/", () => "$($service.Description) is running!");

app.MapGet("/info", () => new {
    service = "$($service.Name)",
    description = "$($service.Description)",
    port = $($service.Port),
    endpoints = new[] {
        "/health",
        "/",
        "/info",
        "/swagger"
    },
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
});

app.Run();
"@
    
    Set-Content -Path "$serviceDir/Program.cs" -Value $programContent
    
    # Create simple project file
    $projectContent = @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
"@
    
    Set-Content -Path "$serviceDir/$serviceName.csproj" -Value $projectContent
    
    # Run the service in background
    Write-Host "Starting $($service.Name)..." -ForegroundColor Green
    $job = Start-Job -ScriptBlock {
        param($ServiceDir, $Port, $ServiceName)
        Set-Location $using:PWD
        cd $ServiceDir
        try {
            dotnet run --project "$ServiceName.csproj" --urls "http://localhost:$Port"
        } catch {
            Write-Host "Error starting $ServiceName`: $_" -ForegroundColor Red
        }
    } -ArgumentList $serviceDir, $service.Port, $serviceName
    
    Write-Host "✓ $($service.Name) started (Job ID: $($job.Id))" -ForegroundColor Green
    Start-Sleep -Seconds 2
}

# Step 4: Wait for services to start
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Step 5: Test the services
Write-Host "🧪 Testing services..." -ForegroundColor Green
foreach ($service in $services) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$($service.Port)/health" -Method Get -TimeoutSec 5
        Write-Host "✓ $($service.Name): $($response.status)" -ForegroundColor Green
    } catch {
        Write-Host "✗ $($service.Name): Not responding" -ForegroundColor Red
    }
}

# Step 6: Show access information
Write-Host "`n🎉 IO Platform is running!" -ForegroundColor Green
Write-Host "`n📊 Service Endpoints:" -ForegroundColor Cyan
foreach ($service in $services) {
    Write-Host "  $($service.Name.PadRight(15)): http://localhost:$($service.Port)" -ForegroundColor White
    Write-Host "                     Health: http://localhost:$($service.Port)/health" -ForegroundColor Gray
    Write-Host "                     Swagger: http://localhost:$($service.Port)/swagger" -ForegroundColor Gray
}

Write-Host "`n📈 Monitoring:" -ForegroundColor Cyan
Write-Host "  Grafana:     http://localhost:3000 (admin/admin123)" -ForegroundColor White
Write-Host "  Prometheus:  http://localhost:9090" -ForegroundColor White
Write-Host "  MongoDB:    localhost:27017 (admin/password123)" -ForegroundColor White
Write-Host "  Kafka:       localhost:9092" -ForegroundColor White

Write-Host "`n🛑 To stop all services:" -ForegroundColor Yellow
Write-Host "  1. Stop .NET jobs: Get-Job | Stop-Job | Remove-Job" -ForegroundColor White
Write-Host "  2. Stop Docker: docker-compose -f docker-compose.dev.yml down" -ForegroundColor White

# Show running jobs
Write-Host "`n📋 Running .NET Services:" -ForegroundColor Cyan
Get-Job | Format-Table Id, Name, State -AutoSize

Write-Host "`n✨ Ready for development!" -ForegroundColor Green
