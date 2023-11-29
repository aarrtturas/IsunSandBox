using FluentValidation;
using FluentValidation.AspNetCore;
using Isun.ApplicationContext;
using Isun.Domain.Validators;
using Isun.Services;
using Isun.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NDesk.Options;
using Polly;
using Polly.Retry;
using Serilog;
using System.Net.Http.Headers;

namespace Isun;
public partial class Program
{
    private async static Task Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            ShowInfo();

            if (args.Length == 0)
            {
                ShowHelp(null);
                return;
            }

            bool next = GetParsedArgs(args);

            if (next)
            {
                var host = ConfigureHost().Build();
                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var myService = services.GetRequiredService<IHostedService>();
                    Console.WriteLine("Press any key to exit.");
                    Console.WriteLine();
                    await host.StartAsync();
                    Console.ReadKey();
                    await host.StopAsync();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
            Log.Error(e, "Application failed to start. Method: {@Method}", nameof(Main));
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder ConfigureHost()
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        IConfigurationRoot configuration = BuildInitConfiguration(environment);

        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        Log.Information("Application started. Method: {@Method}", nameof(Main));

        return Host.CreateDefaultBuilder()
                    .UseSerilog()
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName: "IsunInMemoryDatabase"));
                        services.AddSingleton(configuration);
                        services.AddSingleton<ILoggerFactory, LoggerFactory>();
                        services.AddFluentValidationClientsideAdapters();
                        services.AddValidatorsFromAssemblyContaining<ArgsValidator>();
                        services.AddScoped<ICitiesRepository, CitiesRepository>();
                        services.AddScoped<IAuthenticationService, AuthenticationService>();
                        services.AddScoped<ICitiesWeatherService, CitiesService>();
                        services.AddSingleton<CitiesWeatherHostedService>();
                        services.AddSingleton<AuthenticatedHttpClientHandler>();
                        services.AddHttpClient(Constants.HttpClientForAuthentication, c =>
                        {
                            c.BaseAddress = new Uri(configuration["WeatherApi:BaseUrl"]!);
                            c.DefaultRequestHeaders.Add("Accept", "application/json");
                        }).AddTransientHttpErrorPolicy(s => s.WaitAndRetryAsync(3, times => TimeSpan.FromSeconds(times * 1)));
                        services.AddHttpClient(Constants.HttpClientForCityWeather, c =>
                        {
                            c.BaseAddress = new Uri(configuration["WeatherApi:BaseUrl"]!);
                            c.DefaultRequestHeaders.Add("Accept", "application/json");
                        }).AddTransientHttpErrorPolicy(s => s.WaitAndRetryAsync(3, times => TimeSpan.FromSeconds(times * 1)))
                          .AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
                          .AddPolicyHandler((provider, _) => GetRetryPolicyForUnauthorized(configuration, provider));
                        services.AddSingleton<IHostedService, CitiesWeatherHostedService>()
                                .AddLogging(builder =>
                                {
                                    builder.AddSerilog(Log.Logger);
                                });
                    });
    }

    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicyForUnauthorized(IConfiguration configuration, IServiceProvider provider)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            .RetryAsync(3, async (result, retryCount) =>
            {
                string userName = configuration["WeatherApi:UserName"]!;
                var authenticationService = provider.GetRequiredService<IAuthenticationService>();
                var newToken = await authenticationService.GetBearerToken(userName, ArgsManager.Instance.Password);
                TokenManager.Instance.Token = newToken;
                result.Result.RequestMessage!.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
            });
    }

    private static IConfigurationRoot BuildInitConfiguration(string environment)
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile("appsettings.serilog.json", false, true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static bool GetParsedArgs(string[] args)
    {
        bool showHelp = false;
        string passwordForApi = string.Empty;
        string cities = string.Empty;

        var p = new OptionSet()
        {
            { "p|password=", "the {PASSWORD} for weather api", v => passwordForApi = v },
            { "c|cities=", "the {CITIES} to get weather: \"Vilnius, Kaunas, Klaipėda\" ", v => cities = v },
            { "h|help",  "show this message and exit", v => showHelp = v != null },
        };

        IEnumerable<string> extra;
        try
        {
            extra = p.Parse(args);
        }
        catch (OptionException e)
        {
            Console.Write("Isun: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `Isun --help' for more information.");
            return false;
        }

        if (showHelp)
        {
            ShowHelp(p);
            return false;
        }
        if (string.IsNullOrEmpty(passwordForApi))
        {
            Console.WriteLine("Isun: Missing required password for api");
            Console.WriteLine("Try `Isun --help' for more information.");
            return false;
        }
        if (string.IsNullOrEmpty(cities))
        {
            Console.WriteLine("Isun: Missing required cities");
            Console.WriteLine("Try `Isun --help' for more information.");
            return false;
        }

        ArgsManager.Instance.Password = passwordForApi;
        ArgsManager.Instance.Cities = cities;

        return true;
    }

    private static void ShowInfo()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"App working directory: {AppContext.BaseDirectory}");
        Console.WriteLine($"Logging directory: App_Data");
        Console.WriteLine();
        Console.ResetColor();
    }

    private static void ShowHelp(OptionSet? p)
    {
        if (p is null)
        {
            Console.WriteLine("Try `Isun -h or --help' for more information.");
            return;
        }

        string filePath = AppContext.BaseDirectory;

        Console.WriteLine("Use this tool to get weather forecast for cities.");
        Console.WriteLine("You can provide single city or cities array.");
        Console.WriteLine("You will need to provide weather forecast api password to access it");
        Console.WriteLine($"App directory: {filePath}");
        Console.WriteLine($"App logging directory: {filePath}App_Data");
        Console.WriteLine();
        Console.WriteLine("Options:");
        p.WriteOptionDescriptions(Console.Out);
    }
}



