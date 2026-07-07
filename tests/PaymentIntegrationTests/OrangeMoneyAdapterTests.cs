using System.Net;
using System.Net.Http.Json;
using Domain.PaymentManagement.Ports;
using Infrastructure.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace PaymentIntegrationTests;

public class OrangeMoneyAdapterTests
{
    private readonly ILogger<OrangeMoneyAdapter> _logger;
    private readonly AfricasTalkingOptions _options;

    public OrangeMoneyAdapterTests()
    {
        _logger = NullLogger<OrangeMoneyAdapter>.Instance;
        _options = new AfricasTalkingOptions
        {
            ApiKey = "test-api-key",
            Username = "sandbox",
            BaseUrl = "https://payments.africastalking.com",
            ProductName = "test-product",
            WebhookHmacSecret = "test-secret",
            TimeoutSeconds = 10
        };
    }

    [Fact]
    public async Task InitierPaiementAsync_Success_ReturnsPendingConfirmation()
    {
        // Arrange
        var response = @"{
            ""status"": ""PendingConfirmation"",
            ""description"": ""Waiting for user input"",
            ""transactionId"": ""ATPid_12345""
        }";
        var handler = CreateMockHandler(HttpStatusCode.OK, response);
        var adapter = CreateAdapter(handler);
        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", "REF123");

        // Act
        var result = await adapter.InitierPaiementAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("ATPid_12345", result.TransactionId);
        Assert.Equal("Waiting for user input", result.Description);
    }

    [Fact]
    public async Task InitierPaiementAsync_HttpError_ReturnsFalse()
    {
        // Arrange
        var handler = CreateMockHandler(HttpStatusCode.BadRequest, "Invalid request");
        var adapter = CreateAdapter(handler);
        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", "REF123");

        // Act
        var result = await adapter.InitierPaiementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.Contains("400", result.Description);
    }

    [Fact]
    public async Task InitierPaiementAsync_Timeout_ReturnsFalseWithTimeoutMessage()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var adapter = CreateAdapter(handlerMock.Object);
        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", "REF123");

        // Act
        var result = await adapter.InitierPaiementAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.Equal("Request timed out after 10 seconds.", result.Description);
    }

    [Fact]
    public async Task InitierPaiementAsync_CanceledToken_ThrowsTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var adapter = CreateAdapter(handlerMock.Object);
        var request = new MobileMoneyRequest("+22670000000", 500m, "XOF", "REF123");

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => adapter.InitierPaiementAsync(request, cts.Token));
    }

    private OrangeMoneyAdapter CreateAdapter(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };

        return new OrangeMoneyAdapter(
            httpClient,
            _logger,
            Options.Create(_options));
    }

    private static HttpMessageHandler CreateMockHandler(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return handlerMock.Object;
    }
}
