# Start IO Platform Services Locally
Write-Host "Starting IO Platform Services..." -ForegroundColor Green

# Check if Docker is running
try {
    docker version | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Start infrastructure services
Write-Host "Starting infrastructure services..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml up -d

# Wait for services to be ready
Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Check service status
$services = docker-compose -f docker-compose.dev.yml ps
Write-Host $services

# Build and run .NET services
Write-Host "Building .NET services..." -ForegroundColor Yellow

# Try a simple build first to check for issues
try {
    dotnet restore io-platform.sln
    Write-Host "✓ NuGet restore completed" -ForegroundColor Green
} catch {
    Write-Host "✗ NuGet restore failed. Checking for package issues..." -ForegroundColor Yellow
}

# Start each service in background
$services = @(
    @{Name="IO.Proxy"; Port=8080; Path="src/Apps/RestAPI/IO.Proxy"},
    @{Name="IO.Common"; Port=8081; Path="src/Apps/RestAPI/IO.Common"},
    @{Name="IO.Cass"; Port=8082; Path="src/Apps/RestAPI/IO.Cass"},
    @{Name="IO.Elsa"; Port=8083; Path="src/Apps/RestAPI/IO.Elsa"},
    @{Name="IO.Larry"; Port=8084; Path="src/Apps/RestAPI/IO.Larry"},
    @{Name="IO.Lea"; Port=8085; Path="src/Apps/RestAPI/IO.Lea"}
)

foreach ($service in $services) {
    Write-Host "Starting $($service.Name) on port $($service.Port)..." -ForegroundColor Cyan
    
    # Start in background job
    $job = Start-Job -ScriptBlock {
        param($ServicePath, $Port, $ServiceName)
        Set-Location $using:PWD
        cd $ServicePath
        try {
            dotnet run --urls "http://localhost:$Port"
        } catch {
            Write-Host "Error starting $ServiceName`: $_" -ForegroundColor Red
        }
    } -ArgumentList $service.Path, $service.Port, $service.Name
    
    Write-Host "✓ $($service.Name) started (Job ID: $($job.Id))" -ForegroundColor Green
    Start-Sleep -Seconds 2
}

Write-Host "`nAll services started!" -ForegroundColor Green
Write-Host "`nAccess URLs:" -ForegroundColor Cyan
Write-Host "  API Gateway:        http://localhost:8080" -ForegroundColor White
Write-Host "  Common Services:    http://localhost:8081" -ForegroundColor White
Write-Host "  Carrier Vetting:     http://localhost:8082" -ForegroundColor White
Write-Host "  Pricing Service:     http://localhost:8083" -ForegroundColor White
Write-Host "  Vendor Management:   http://localhost:8084" -ForegroundColor White
Write-Host "  Job Processing:      http://localhost:8085" -ForegroundColor White
Write-Host "`nMonitoring:" -ForegroundColor Cyan
Write-Host "  Grafana:             http://localhost:3000 (admin/admin123)" -ForegroundColor White
Write-Host "  Prometheus:          http://localhost:9090" -ForegroundColor White
Write-Host "`nTo stop all services:" -ForegroundColor Yellow
Write-Host "  1. Stop .NET jobs: Get-Job | Stop-Job | Remove-Job" -ForegroundColor White
Write-Host "  2. Stop Docker: docker-compose -f docker-compose.dev.yml down" -ForegroundColor White

# Show running jobs
Write-Host "`nRunning .NET Services:" -ForegroundColor Cyan
Get-Job | Format-Table Id, Name, State -AutoSize
