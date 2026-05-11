using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace AxonWeave.Sdk;

public class AxonWeaveClient
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public AxonWeaveClient(string baseUrl, string? token = null, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _token = token;
    }

    public void SetToken(string token) => _token = token;

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<RegisterResponse>(HttpMethod.Post, "api/auth/register", request, cancellationToken);
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var auth = await SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/verify-otp", request, cancellationToken);
        _token = auth.Token;
        return auth;
    }

    public async Task<IReadOnlyCollection<UserDto>> SearchUsersAsync(string? phone = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(phone) ? "api/users" : $"api/users?phone={Uri.EscapeDataString(phone)}";
        return await SendAsync<IReadOnlyCollection<UserDto>>(HttpMethod.Get, path, null, cancellationToken);
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<ConversationDto>(HttpMethod.Post, "api/conversations", request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConversationDto>> ListConversationsAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync<IReadOnlyCollection<ConversationDto>>(HttpMethod.Get, "api/conversations", null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<MessageDto>> GetMessagesAsync(Guid conversationId, DateTimeOffset? before = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        var path = $"api/messages?conversationId={conversationId}&limit={limit}";
        if (before.HasValue)
        {
            path += $"&before={Uri.EscapeDataString(before.Value.ToString("O"))}";
        }

        return await SendAsync<IReadOnlyCollection<MessageDto>>(HttpMethod.Get, path, null, cancellationToken);
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        return await SendAsync<MessageDto>(HttpMethod.Post, "api/messages", request, cancellationToken);
    }

    public async Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await SendRawAsync(HttpMethod.Delete, $"api/messages/{messageId}", null, cancellationToken);
    }

    public async Task<MessageDto> MarkReadAsync(Guid messageId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await SendAsync<MessageDto>(HttpMethod.Put, $"api/messages/{messageId}/read", new { conversationId }, cancellationToken);
    }

    public async Task<MediaUploadResponse> UploadMediaAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(content, "file", fileName);
        return await SendAsync<MediaUploadResponse>(HttpMethod.Post, "api/media/upload", form, cancellationToken);
    }

    public HubConnection CreateHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_httpClient.BaseAddress!, "hubs/chat"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_token);
            })
            .WithAutomaticReconnect()
            .Build();
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var response = await SendRawAsync(method, path, body, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: cancellationToken);
        return payload is null ? throw new AxonWeaveException("The API returned an empty response.", (int)response.StatusCode, null) : payload.Data;
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(_token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        request.Content = body switch
        {
            null => null,
            HttpContent content => content,
            _ => JsonContent.Create(body)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AxonWeaveException($"axon-weave request failed with {(int)response.StatusCode}.", (int)response.StatusCode, errorBody);
        }

        return response;
    }
}

public class AxonWeaveException : Exception
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    public AxonWeaveException(string message, int statusCode, string? responseBody) : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
