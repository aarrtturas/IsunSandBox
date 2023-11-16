using FluentValidation;
using Isun.ApplicationContext;
using Isun.Domain.Dao;
using Isun.Domain.Projections;
using Isun.Domain.Validators;
using Isun.Services;
using Isun.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Isun;
public class CitiesWeatherHostedService : IHostedService, IDisposable
{
    private readonly int refreshTimeInSeconds = 15;
    private static string token = string.Empty;
    private int executionCount = 0;
    private readonly IAuthenticationService authentication;
    private readonly ICitiesWeatherService citiesWeatherService;
    private readonly IValidator<ArgsValidator> validator;
    private readonly IConfiguration configuration;
    private readonly ApplicationDbContext context;
    private List<string> citiesToUse = new();

    private Timer? _timer = null;

    public CitiesWeatherHostedService(IAuthenticationService authentication,
                                      ICitiesWeatherService citiesWeatherService,
                                      IValidator<ArgsValidator> validator,
                                      IConfiguration configuration,
                                      ApplicationDbContext context)
    {
        this.authentication = authentication;
        this.citiesWeatherService = citiesWeatherService;
        this.validator = validator;
        this.configuration = configuration;
        this.context = context;
#pragma warning disable CS8604
        this.refreshTimeInSeconds = int.Parse(configuration["WeatherApi:DelayInSeconds"]);
#pragma warning restore CS8604
    }

    public void InitTest(List<string> citiesToUse)
    {
        this.citiesToUse = citiesToUse;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        Log.Information("Method: {@Method}. Timed Hosted Service started.", nameof(StartAsync));
        Console.WriteLine($"Cities weather hosted service started.");

        var userName = configuration["WeatherApi:UserName"];
#pragma warning disable CS8604
        token = await authentication.GetBearerToken(userName, ArgsManager.Instance.Password);
#pragma warning restore CS8604
        this.citiesWeatherService.Init(token);

        if (!await ValidateCities())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Cities weather hosted service is exiting. Bad inputs.");
            Console.ResetColor();
            await StopAsync(default);
            return;
        }

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(refreshTimeInSeconds));
    }

    private async Task<bool> ValidateCities()
    {
        var result = this.validator.Validate(new ArgsValidator(ArgsManager.Instance.Cities));

        if (!result.IsValid)
        {

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Cities {ArgsManager.Instance.Cities} are not valid. Error: {result.Errors[0].ErrorMessage}");
            Console.ResetColor();
            return false;
        }

        string[] citiesProvidedArray = ArgsManager.Instance.Cities.Trim()
                                                                   .Replace(", ", ",")
                                                                   .Split(',');

        var citiesExisting = await this.citiesWeatherService.GetCities();
        foreach (var city in citiesProvidedArray)
        {
            if (citiesExisting.Contains(city))
                citiesToUse.Add(city);
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"City {city} is not supported and will be skipped");
                Console.ResetColor();
                Log.Warning("City {@City} is not supported and will be skipped", city);
            }
        }

        return true;
    }

    private void DoWork(object? state)
    {
        var count = Interlocked.Increment(ref executionCount);

        var t1 = Task.Run(() => GetCitiesWeather());
        Task.WaitAll(t1);

        Console.WriteLine($"Cities weather hosted service is working. Count: {count}. Delay {refreshTimeInSeconds}");
    }

    public async Task GetCitiesWeather()
    {
        try
        {
            var items = new List<CityWeatherDao>();

            foreach (var city in citiesToUse)
            {
                var result = await this.citiesWeatherService.GetWeather(city);
                if (result is not null)
                    items.Add(result.ToDao());
                else
                {
                    Console.WriteLine($"City {city} is not supported");
                    Log.Warning("City {@City} is not supported", city);
                }
            }

            foreach (var item in items)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"City: {item.City} Temperature: {item.Temperature} Precipitation: {item.Precipitation} WindSpeed: {item.WindSpeed} Summary: {item.Summary}");
                Console.ResetColor();
            }

            if (items.Count == 0)
                return;

            context.AddRange(items);
            context.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, "Method: {@Method}", nameof(GetCitiesWeather));
            throw;
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {

        Log.Information("Method: {@Method}. Timed Hosted Service stopped.", nameof(StopAsync));

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
