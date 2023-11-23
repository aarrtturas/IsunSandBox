using FluentValidation;
using Isun.ApplicationContext;
using Isun.Domain.Dao;
using Isun.Domain.Validators;
using Isun.Domain.View;
using Isun.Services;
using Isun.Shared;
using Moq;
using Xunit;

namespace Isun.Tests;

public class CitiesWeatherHostedServiceTest : BaseTests
{
    public readonly Mock<IAuthenticationService> AuthenticationServiceMock = new Mock<IAuthenticationService>();
    public readonly Mock<ICitiesWeatherService> CitiesWeatherServiceMock = new Mock<ICitiesWeatherService>();
    public readonly Mock<IValidator<ArgsValidator>> ValidatorMock = new Mock<IValidator<ArgsValidator>>();
    public readonly Mock<ICitiesRepository> CitiesRepositoryMock = new Mock<ICitiesRepository>();

    [Fact]
    public async Task GetCitiesWeather_WithSupportedCity_ShouldSaveToDatabase()
    {
        // Arrange
        this.CitiesWeatherServiceMock.Setup(service => service.GetWeather(It.IsAny<string>())).ReturnsAsync(new CityWeatherView());
        this.ConfigurationMock.Setup(s => s["WeatherApi:DelayInSeconds"]).Returns("15");
        var classUnderTest = new CitiesWeatherHostedService(loggerFactoryMock.Object,
                                                            AuthenticationServiceMock.Object,
                                                            CitiesWeatherServiceMock.Object,
                                                            ValidatorMock.Object,
                                                            ConfigurationMock.Object,
                                                            CitiesRepositoryMock.Object);

        this.ConfigurationMock.Setup(s => s[It.IsAny<string>()]).Returns("username");
        CitiesRepositoryMock.Setup(s => s.AddRange(It.IsAny<List<CityWeatherDao>>()));
        classUnderTest.InitTest(new List<string>() { "Vilnius", "Kaunas" });
        ArgsManager.Instance.Password = "password";
        ArgsManager.Instance.Cities = "Vilnius, Kaunas";

        // Act
        await classUnderTest.GetCitiesWeather();

        // Assert
        CitiesRepositoryMock.Verify(db => db.AddRange(It.Is<List<CityWeatherDao>>(x => x.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task GetCitiesWeather_WithUnsupportedCity_ShouldNotSave()
    {
        // Arrange
        this.CitiesWeatherServiceMock.Setup(service => service.GetWeather(It.IsAny<string>())).ReturnsAsync((CityWeatherView?)null);
        this.ConfigurationMock.Setup(s => s["WeatherApi:DelayInSeconds"]).Returns("15");

        var classUnderTest = new CitiesWeatherHostedService(loggerFactoryMock.Object,
                                                            AuthenticationServiceMock.Object,
                                                            CitiesWeatherServiceMock.Object,
                                                            ValidatorMock.Object,
                                                            ConfigurationMock.Object,
                                                            CitiesRepositoryMock.Object);

        this.ConfigurationMock.Setup(s => s[It.IsAny<string>()]).Returns("username");
        CitiesRepositoryMock.Setup(s => s.AddRange(It.IsAny<List<CityWeatherDao>>()));
        classUnderTest.InitTest(new List<string>() { "Vilnius" });
        ArgsManager.Instance.Password = "password";
        ArgsManager.Instance.Cities = "Jonava";

        // Act
        await classUnderTest.GetCitiesWeather();

        // Assert
        CitiesRepositoryMock.Verify(db => db.AddRange(It.IsAny<List<CityWeatherDao>>()), Times.Never);
    }
}
