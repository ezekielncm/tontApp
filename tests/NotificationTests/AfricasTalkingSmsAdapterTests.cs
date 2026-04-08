using System.Net;
using Domain.NotificationManagement.Ports;
using Infrastructure.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace NotificationTests;

public class AfricasTalkingSmsAdapterTests
{
    private readonly ILogger<AfricasTalkingSmsAdapter> _logger;
    private readonly AfricasTalkingSmsOptions _options;

    public AfricasTalkingSmsAdapterTests()
    {
        _logger = NullLogger<AfricasTalkingSmsAdapter>.Instance;
        _options = new AfricasTalkingSmsOptions
        {
            ApiKey = "test-api-key",
            Username = "sandbox",
            ShortCode = "12345",
            BaseUrl = "https://api.africastalking.com/version1/",
            RetryBackoffMinutes = [0, 0, 0] // No delay in tests
        };
    }

    [Fact]
    public async Task EnvoyerAsync_WithInvalidPhoneNumber_ReturnsFalse()
    {
        // Arrange
        var adapter = CreateAdapter(CreateMockHandler(HttpStatusCode.OK, CreateSuccessResponse()));

        // Act
        var result = await adapter.EnvoyerAsync("invalid-phone", "Test message");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("E.164", result.Description);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abc")]
    [InlineData("+")]
    [InlineData("")]
    public async Task EnvoyerAsync_WithVariousInvalidPhones_RejectsAll(string phone)
    {
        var adapter = CreateAdapter(CreateMockHandler(HttpStatusCode.OK, CreateSuccessResponse()));

        var result = await adapter.EnvoyerAsync(phone, "Test");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task EnvoyerAsync_WithValidE164Phone_SendsSms()
    {
        // Arrange
        var handler = CreateMockHandler(HttpStatusCode.OK, CreateSuccessResponse());
        var adapter = CreateAdapter(handler);

        // Act
        var result = await adapter.EnvoyerAsync("+22670000000", "Test message");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("msg-123", result.MessageId);
    }

    [Fact]
    public async Task EnvoyerAsync_WithApiError_ReturnsFalse()
    {
        // Arrange
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "Server Error");
        var adapter = CreateAdapter(handler);

        // Act
        var result = await adapter.EnvoyerAsync("+22670000000", "Test message");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("500", result.Description);
    }

    [Fact]
    public async Task EnvoyerAsync_WithNetworkError_RetriesAndReturnsFalse()
    {
        // Arrange - handler that always throws
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.EnvoyerAsync("+22670000000", "Test message");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("réseau", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnvoyerAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange - handler that checks cancellation token
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateSuccessResponse())
                });
            });

        var adapter = CreateAdapter(handlerMock.Object);

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => adapter.EnvoyerAsync("+22670000000", "Test", cts.Token));
    }

    [Fact]
    public async Task EnvoyerAsync_WithFailedRecipient_ReturnsFalse()
    {
        // Arrange
        var response = @"{
            ""SMSMessageData"": {
                ""Message"": ""Sent to 0/1"",
                ""Recipients"": [{
                    ""statusCode"": 403,
                    ""number"": ""+22670000000"",
                    ""status"": ""InvalidPhoneNumber"",
                    ""cost"": ""0"",
                    ""messageId"": null
                }]
            }
        }";

        var handler = CreateMockHandler(HttpStatusCode.OK, response);
        var adapter = CreateAdapter(handler);

        // Act
        var result = await adapter.EnvoyerAsync("+22670000000", "Test message");

        // Assert
        Assert.False(result.Success);
    }

    private AfricasTalkingSmsAdapter CreateAdapter(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };

        return new AfricasTalkingSmsAdapter(
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

    private static string CreateSuccessResponse()
    {
        return @"{
            ""SMSMessageData"": {
                ""Message"": ""Sent to 1/1"",
                ""Recipients"": [{
                    ""statusCode"": 101,
                    ""number"": ""+22670000000"",
                    ""status"": ""Success"",
                    ""cost"": ""XOF 5"",
                    ""messageId"": ""msg-123""
                }]
            }
        }";
    }
}
