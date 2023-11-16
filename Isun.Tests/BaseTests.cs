using Isun.ApplicationContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Isun.Tests;
public abstract class BaseTests : IDisposable
{
    protected readonly Mock<ApplicationDbContext> ContextMock;
    protected readonly Mock<IConfigurationRoot> configurationRootMock;
    protected readonly Mock<IConfiguration> ConfigurationMock;
    protected Mock<ILoggerFactory> loggerFactoryMock;
    protected Mock<ILogger> loggerMock;

    public BaseTests()
    {
        var dbContextOptions = new DbContextOptions<ApplicationDbContext>();
        this.ContextMock = new Mock<ApplicationDbContext>(dbContextOptions);

        this.ConfigurationMock = new Mock<IConfiguration>();
        this.ConfigurationMock.Setup(s => s[It.IsAny<string>()]).Returns("value");
        this.ConfigurationMock.Setup(s => s[It.IsAny<string>()]).Returns("value");
        this.configurationRootMock = new Mock<IConfigurationRoot>();
        this.configurationRootMock.Setup(s => s.GetSection(It.IsAny<string>()).Value).Returns("value");

        this.loggerMock = new Mock<ILogger>();
        this.loggerFactoryMock = new Mock<ILoggerFactory>();
        this.loggerFactoryMock.Setup(s => s.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
    }

    public void Dispose()
    {
        this.ContextMock.Object.Dispose();
        this.loggerFactoryMock.Object.Dispose();
        this.loggerFactoryMock.Object.Dispose();
    }
}