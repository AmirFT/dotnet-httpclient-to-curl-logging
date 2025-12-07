using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AmirFT.Http.CurlLogging;


public class CurlLoggingHandler : DelegatingHandler
{
    private readonly ILogger<CurlLoggingHandler> _logger;
    private readonly SensitiveDataRedactor _redactor;

    public CurlLoggingHandler(ILogger<CurlLoggingHandler> logger)
        : this(logger, CurlLoggingOptions.Default)
    {
    }

    public CurlLoggingHandler(ILogger<CurlLoggingHandler> logger, CurlLoggingOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redactor = new SensitiveDataRedactor(options ?? CurlLoggingOptions.Default);
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

        return await base.SendAsync(request, cancellationToken);
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
}
