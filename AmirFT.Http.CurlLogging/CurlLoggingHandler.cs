using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AmirFT.Http.CurlLogging;


public class CurlLoggingHandler : DelegatingHandler
{
    private readonly ILogger<CurlLoggingHandler> _logger;
    private readonly SensitiveDataRedactor _redactor;
    private readonly CurlLoggingOptions _options;

    public CurlLoggingHandler(ILogger<CurlLoggingHandler> logger)
        : this(logger, CurlLoggingOptions.Default)
    {
    }

    public CurlLoggingHandler(ILogger<CurlLoggingHandler> logger, CurlLoggingOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? CurlLoggingOptions.Default;
        _redactor = new SensitiveDataRedactor(_options);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var curlCommand = await ToCurlCommand(request);
            _logger.LogInformation("HTTP Request as cURL:\n{CurlCommand}", curlCommand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate cURL command for request to {Uri}", request.RequestUri);
        }

        HttpResponseMessage response;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP request to {Uri} failed after {ElapsedMs} ms", request.RequestUri, stopwatch.ElapsedMilliseconds);
            throw;
        }

        stopwatch.Stop();

        if (_options.LogResponse)
        {
            await LogResponseAsync(response, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    private async Task<string> ToCurlCommand(HttpRequestMessage request)
    {
        var redactedUri = _redactor.RedactUri(request.RequestUri);
        var curl = $"curl -X {request.Method.Method} '{redactedUri}'";

        foreach (var header in request.Headers)
        {
            if (_redactor.ShouldExcludeHeader(header.Key))
                continue;

            foreach (var value in header.Value)
            {
                var redactedValue = _redactor.RedactHeader(header.Key, value);
                curl += $" \\\n  -H '{header.Key}: {redactedValue}'";
            }
        }

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                if (_redactor.ShouldExcludeHeader(header.Key))
                    continue;

                foreach (var value in header.Value)
                {
                    var redactedValue = _redactor.RedactHeader(header.Key, value);
                    curl += $" \\\n  -H '{header.Key}: {redactedValue}'";
                }
            }

            try
            {
                var content = await request.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var redactedContent = _redactor.RedactBody(content);
                    curl += $" \\\n  -d '{redactedContent.Replace("'", "\\'")}'";
                }
            }
            catch (Exception)
            {
                // Content stream may not be readable multiple times, skip body in curl command
                curl += " \\\n  [Content body not available for logging]";
            }
        }

        return curl;
    }

    private async Task LogResponseAsync(HttpResponseMessage response, long elapsedMs)
    {
        try
        {
            var responseBody = response.Content != null
                ? await response.Content.ReadAsStringAsync()
                : string.Empty;

            var headers = string.Join("\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

            if (response.Content?.Headers != null)
            {
                var contentHeaders = string.Join("\n", response.Content.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                if (!string.IsNullOrEmpty(contentHeaders))
                {
                    headers = string.IsNullOrEmpty(headers) ? contentHeaders : $"{headers}\n{contentHeaders}";
                }
            }

            var redactedBody = _options.EnableRedaction ? _redactor.RedactBody(responseBody) : responseBody;

            _logger.LogInformation(
                "HTTP Response from {Uri} in {ElapsedMs}ms - Status: {StatusCode}\nHeaders:\n{Headers}\nBody:\n{Body}",
                response.RequestMessage?.RequestUri,
                elapsedMs,
                (int)response.StatusCode,
                headers,
                redactedBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log HTTP response for {Uri}", response.RequestMessage?.RequestUri);
        }
    }
}
