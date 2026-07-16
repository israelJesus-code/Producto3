using System.Text.Json.Serialization;

namespace SistemaLegalPagares.Services.Recaptcha;

public class GoogleRecaptchaService : IRecaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    private readonly HttpClient _httpClient;
    private readonly string _secretKey;

    public GoogleRecaptchaService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        SiteKey = config["Recaptcha:SiteKey"] ?? string.Empty;
        _secretKey = config["Recaptcha:SecretKey"] ?? string.Empty;
    }

    public string SiteKey { get; }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_secretKey))
        {
            return false;
        }

        var parameters = new Dictionary<string, string>
        {
            ["secret"] = _secretKey,
            ["response"] = token,
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            parameters["remoteip"] = remoteIp;
        }

        using var content = new FormUrlEncodedContent(parameters);
        using var response = await _httpClient.PostAsync(VerifyUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken: ct);
        return result?.Success == true;
    }

    private sealed class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
