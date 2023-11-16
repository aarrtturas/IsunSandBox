using FluentValidation;
using Isun.Domain.Dao;
using Isun.Domain.Validators;
using Isun.Domain.View;
using Isun.Services;
using Isun.Shared;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;
using static System.Net.WebRequestMethods;

namespace Isun.Tests;

public class CitiesServiceTest : BaseTests
{
    public readonly Mock<IAuthenticationService> AuthenticationServiceMock = new Mock<IAuthenticationService>();
    public readonly Mock<ICitiesWeatherService> CitiesWeatherServiceMock = new Mock<ICitiesWeatherService>();
    public readonly Mock<IValidator<ArgsValidator>> ValidatorMock = new Mock<IValidator<ArgsValidator>>();

    [Fact]
    public async Task RetryPolicy_RetriesOnUnauthorized()
    {
        // Arrange

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        // Simulate Unauthorized response for the first two requests
        httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("api/weathers/City1")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent
                (
                """
                    {
                        "city":"Vilnius",
                        "temperature":10,
                        "precipitation":85,
                        "windSpeed":6,
                        "summary":"Mild"
                    }
                """
                )
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object) { BaseAddress = new Uri ("https://weather-api.isun.ch/")};


        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        AuthenticationServiceMock.Setup(service => service.GetBearerToken(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("fakeToken");

        var serviceUnderTest = new CitiesService(loggerFactoryMock.Object,
                                          ConfigurationMock.Object,
                                          httpClientFactoryMock.Object,
                                          AuthenticationServiceMock.Object);

        // Act
        var result = await serviceUnderTest.GetWeather("City1");

        // Assert
        Assert.NotNull(result);

        // Ensure that there were three requests in total (2 retries)
        httpMessageHandlerMock.Protected().Verify<Task<HttpResponseMessage>>(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("api/weathers/City1")),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
