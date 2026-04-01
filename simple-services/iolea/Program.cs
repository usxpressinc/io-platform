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
    service = "IO.Lea", 
    description = "Job Processing",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
});

app.MapGet("/", () => "Job Processing is running!");

app.MapGet("/info", () => new {
    service = "IO.Lea",
    description = "Job Processing",
    port = 8085,
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
