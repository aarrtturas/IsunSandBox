using FluentValidation;
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

    [Fact]
    public async Task GetCitiesWeather_WithSupportedCity_ShouldSaveToDatabase()
    {
        // Arrange
        this.CitiesWeatherServiceMock.Setup(service => service.GetWeather(It.IsAny<string>())).ReturnsAsync(new CityWeatherView());
        this.ConfigurationMock.Setup(s => s["WeatherApi:DelayInSeconds"]).Returns("15");
        var classUnderTest = new CitiesWeatherHostedService(AuthenticationServiceMock.Object,
                                                            CitiesWeatherServiceMock.Object,
                                                            ValidatorMock.Object,
                                                            ConfigurationMock.Object,
                                                            ContextMock.Object);

        this.ConfigurationMock.Setup(s => s[It.IsAny<string>()]).Returns("username");
        ContextMock.Setup(s => s.AddRange(It.IsAny<List<CityWeatherDao>>()));
        classUnderTest.InitTest(new List<string>() { "Vilnius", "Kaunas" });
        ArgsManager.Instance.Password = "password";
        ArgsManager.Instance.Cities = "Vilnius, Kaunas";

        // Act
        await classUnderTest.GetCitiesWeather();

        // Assert
        ContextMock.Verify(db => db.AddRange(It.Is<List<CityWeatherDao>>(x => x.Count == 2)), Times.Once);
        ContextMock.Verify(db => db.SaveChanges(), Times.Once);
    }

    [Fact]
    public async Task GetCitiesWeather_WithUnsupportedCity_ShouldNotSave()
    {
        // Arrange
        this.CitiesWeatherServiceMock.Setup(service => service.GetWeather(It.IsAny<string>())).ReturnsAsync((CityWeatherView?)null);
        this.ConfigurationMock.Setup(s => s["WeatherApi:DelayInSeconds"]).Returns("15");
        var classUnderTest = new CitiesWeatherHostedService(AuthenticationServiceMock.Object,
                                                            CitiesWeatherServiceMock.Object,
                                                            ValidatorMock.Object,
                                                            ConfigurationMock.Object,
                                                            ContextMock.Object);

        this.ConfigurationMock.Setup(s => s[It.IsAny<string>()]).Returns("username");
        ContextMock.Setup(s => s.AddRange(It.IsAny<List<CityWeatherDao>>()));
        classUnderTest.InitTest(new List<string>() { "Vilnius" });
        ArgsManager.Instance.Password = "password";
        ArgsManager.Instance.Cities = "Jonava";

        // Act
        await classUnderTest.GetCitiesWeather();

        // Assert
        ContextMock.Verify(db => db.AddRange(It.IsAny<List<CityWeatherDao>>()), Times.Never);
        ContextMock.Verify(db => db.SaveChanges(), Times.Never);
    }
}
