# Stop IO Platform Services
Write-Host "Stopping IO Platform Services..." -ForegroundColor Green

# Stop .NET services
Write-Host "Stopping .NET services..." -ForegroundColor Yellow
Get-Job | Stop-Job | Remove-Job
Write-Host "✓ .NET services stopped" -ForegroundColor Green

# Stop Docker services
Write-Host "Stopping Docker services..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml down
Write-Host "✓ Docker services stopped" -ForegroundColor Green

# Optional: Remove volumes (uncomment if you want to clean data)
# Write-Host "Removing Docker volumes..." -ForegroundColor Yellow
# docker-compose -f docker-compose.dev.yml down -v
# Write-Host "✓ Docker volumes removed" -ForegroundColor Green

Write-Host "All services stopped!" -ForegroundColor Green
