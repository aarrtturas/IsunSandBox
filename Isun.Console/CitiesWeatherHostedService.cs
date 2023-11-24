using FluentValidation;
using Isun.ApplicationContext;
using Isun.Domain.Dao;
using Isun.Domain.Projections;
using Isun.Domain.Validators;
using Isun.Services;
using Isun.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Isun;
public class CitiesWeatherHostedService : IHostedService, IDisposable
{
    private readonly int refreshTimeInSeconds = 15;
    private static string token = string.Empty;
    private int executionCount = 0;
    private readonly ILogger logger;
    private readonly IAuthenticationService authentication;
    private readonly ICitiesWeatherService citiesWeatherService;
    private readonly IValidator<ArgsValidator> validator;
    private readonly IConfiguration configuration;
    private readonly ICitiesRepository citiesRepository;
    private List<string> citiesToUse = new();

    private Timer? _timer = null;

    public CitiesWeatherHostedService(ILoggerFactory loggerFactory,
                                      IAuthenticationService authentication,
                                      ICitiesWeatherService citiesWeatherService,
                                      IValidator<ArgsValidator> validator,
                                      IConfiguration configuration,
                                      ICitiesRepository citiesRepository)
    {
        this.logger = loggerFactory.CreateLogger(nameof(CitiesWeatherHostedService));
        this.authentication = authentication;
        this.citiesWeatherService = citiesWeatherService;
        this.validator = validator;
        this.configuration = configuration;
        this.citiesRepository = citiesRepository;
        this.refreshTimeInSeconds = int.Parse(configuration["WeatherApi:DelayInSeconds"]!);
    }

    public void InitTest(List<string> citiesToUse)
    {
        this.citiesToUse = citiesToUse;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Method: {@Method}. Timed Hosted Service started.", nameof(StartAsync));
        Console.WriteLine($"Cities weather hosted service started.");

        var userName = configuration["WeatherApi:UserName"]!;
        token = await authentication.GetBearerToken(userName, ArgsManager.Instance.Password);
        TokenManager.Instance.Token = token;

        if (!await ValidateCities())
        {
            ConsoleWriteRed("Cities weather hosted service is exiting. Bad inputs.");
            await StopAsync(default);
            return;
        }

        _timer = new Timer(async o => { await DoWork(o); }, null, TimeSpan.Zero, TimeSpan.FromSeconds(refreshTimeInSeconds));
    }

    private async Task<bool> ValidateCities()
    {
        var result = this.validator.Validate(new ArgsValidator(ArgsManager.Instance.Cities));

        if (!result.IsValid)
        {

            ConsoleWriteRed($"Cities {ArgsManager.Instance.Cities} are not valid. Error: {result.Errors[0].ErrorMessage}");
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
                ConsoleWriteRed($"City {city} is not supported");
        }

        return true;
    }

    private async Task DoWork(object? state)
    {
        var count = Interlocked.Increment(ref executionCount);

        await GetCitiesWeather();

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
                    logger.LogWarning("City {@City} is not supported", city);
                }
            }

            foreach (var item in items)
            {
                ConsoleWriteGreen($"City: {item.City} Temperature: {item.Temperature} Precipitation: {item.Precipitation} WindSpeed: {item.WindSpeed} Summary: {item.Summary}");
            }

            if (items.Count == 0)
                return;

            await citiesRepository.AddRange(items);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Method: {@Method}", nameof(GetCitiesWeather));
            throw;
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {

        logger.LogInformation("Method: {@Method}. Timed Hosted Service stopped.", nameof(StopAsync));

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void ConsoleWriteRed(string content)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(content);
        Console.ResetColor();
    }

    private void ConsoleWriteGreen(string content)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(content);
        Console.ResetColor();
    }
}
