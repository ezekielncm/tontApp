namespace PaymentIntegrationTests;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Domain.PaymentManagement.Ports;
using Infrastructure.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

public class OrangeMoneyAdapterTests
{
    [Fact]
    public async Task InitierPaiementAsync_WhenRequestTimesOut_ReturnsTimeoutResponse()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://payments.africastalking.com")
        };

        var loggerMock = new Mock<ILogger<OrangeMoneyAdapter>>();

        var optionsMock = new Mock<IOptions<AfricasTalkingOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new AfricasTalkingOptions
        {
            Username = "sandbox",
            ProductName = "test-product",
            BaseUrl = "https://payments.africastalking.com"
        });

        var adapter = new OrangeMoneyAdapter(
            httpClient,
            loggerMock.Object,
            optionsMock.Object
        );

        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", "REF12345");

        // Act
        var result = await adapter.InitierPaiementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.Equal("Request timed out after 10 seconds.", result.Description);
    }
}
