using IO.Clara.App;
using IO.Clara.Core;
using IO.Core.Authentication;
using IO.Clara.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services using extension pattern
builder.AddCore().AddInfrastructure().AddApplication();

// Add X-Auth token authentication
builder.Services.AddXAuthTokenAuthentication(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
app.UseXAuthTokenAuthentication();
app.UseCors();

app.Run();
