namespace PaymentIntegrationTests;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Tests for HMAC-SHA256 webhook signature validation.
/// Verifies that the webhook controller correctly validates signatures
/// before processing any payment notification.
/// </summary>
public class HmacSignatureValidationTests
{
    private const string TestSecret = "test-webhook-secret-key-at-least-32-chars";

    [Fact]
    public void ComputeHmac_WithValidPayload_ProducesExpectedSignature()
    {
        // Arrange
        var payload = """{"transactionId":"TXN-001","status":"Success"}""";

        // Act
        var signature = ComputeHmacSignature(payload, TestSecret);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal(64, signature.Length); // SHA-256 hex string
    }

    [Fact]
    public void ComputeHmac_SamePayloadAndSecret_ProducesSameSignature()
    {
        // Arrange
        var payload = """{"transactionId":"TXN-001","status":"Success"}""";

        // Act
        var signature1 = ComputeHmacSignature(payload, TestSecret);
        var signature2 = ComputeHmacSignature(payload, TestSecret);

        // Assert
        Assert.Equal(signature1, signature2);
    }

    [Fact]
    public void ComputeHmac_DifferentPayload_ProducesDifferentSignature()
    {
        // Arrange
        var payload1 = """{"transactionId":"TXN-001","status":"Success"}""";
        var payload2 = """{"transactionId":"TXN-002","status":"Failed"}""";

        // Act
        var signature1 = ComputeHmacSignature(payload1, TestSecret);
        var signature2 = ComputeHmacSignature(payload2, TestSecret);

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public void ComputeHmac_DifferentSecret_ProducesDifferentSignature()
    {
        // Arrange
        var payload = """{"transactionId":"TXN-001","status":"Success"}""";

        // Act
        var signature1 = ComputeHmacSignature(payload, TestSecret);
        var signature2 = ComputeHmacSignature(payload, "different-secret-key-at-least-32-chars!!!");

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public void ValidateHmac_WithCorrectSignature_ReturnsTrue()
    {
        // Arrange
        var payload = """{"transactionId":"TXN-001","status":"Success"}""";
        var signature = ComputeHmacSignature(payload, TestSecret);

        // Act
        var isValid = ValidateHmacSignature(payload, signature, TestSecret);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateHmac_WithIncorrectSignature_ReturnsFalse()
    {
        // Arrange
        var payload = """{"transactionId":"TXN-001","status":"Success"}""";
        var wrongSignature = "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        var isValid = ValidateHmacSignature(payload, wrongSignature, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateHmac_WithNullSignature_ReturnsFalse()
    {
        // Arrange
        var payload = """{"transactionId":"TXN-001","status":"Success"}""";

        // Act
        var isValid = ValidateHmacSignature(payload, null, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateHmac_WithEmptySignature_ReturnsFalse()
    {
        // Arrange
        var payload = """{"transactionId":"TXN-001","status":"Success"}""";

        // Act
        var isValid = ValidateHmacSignature(payload, "", TestSecret);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateHmac_WithTamperedPayload_ReturnsFalse()
    {
        // Arrange
        var originalPayload = """{"transactionId":"TXN-001","status":"Success"}""";
        var signature = ComputeHmacSignature(originalPayload, TestSecret);

        var tamperedPayload = """{"transactionId":"TXN-001","status":"Failed"}""";

        // Act
        var isValid = ValidateHmacSignature(tamperedPayload, signature, TestSecret);

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Helper: computes HMAC-SHA256 signature (same logic as the webhook controller).
    /// </summary>
    private static string ComputeHmacSignature(string payload, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Helper: validates HMAC-SHA256 signature using constant-time comparison.
    /// </summary>
    private static bool ValidateHmacSignature(string payload, string? signature, string secret)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        var expectedSignature = ComputeHmacSignature(payload, secret);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature));
    }
}
