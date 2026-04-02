using IO.Common.App;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace IO.Common;

/// <summary>
/// Entry point class for the application, responsible for initializing and starting the application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Arguments.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        WebApplication app;
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddUserSecrets(typeof(Program).Assembly);

            builder.AddApplication();

            app = builder.Build();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly during initialization");
            Console.Write(ex.ToString());
            return;
        }

        try
        {
            await app.MapRoutes().RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
