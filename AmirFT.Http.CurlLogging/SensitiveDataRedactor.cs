using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AmirFT.Http.CurlLogging;

/// <summary>
/// Provides functionality to redact sensitive information from HTTP request data.
/// </summary>
public class SensitiveDataRedactor
{
    private readonly CurlLoggingOptions _options;
    private readonly HashSet<string> _sensitiveHeaders;
    private readonly HashSet<string> _sensitiveQueryParams;
    private readonly List<Regex> _sensitiveBodyPatterns;

    public SensitiveDataRedactor(CurlLoggingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "X-API-Key",
            "X-Api-Key",
            "API-Key",
            "Api-Key",
            "ApiKey",
            "X-Auth-Token",
            "X-Access-Token",
            "X-Token",
            "Bearer",
            "Cookie",
            "Set-Cookie",
            "X-CSRF-Token",
            "X-XSRF-Token"
        };

        _sensitiveQueryParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "api_key",
            "apikey",
            "api-key",
            "access_token",
            "accesstoken",
            "access-token",
            "token",
            "auth",
            "auth_token",
            "authtoken",
            "password",
            "pwd",
            "pass",
            "secret",
            "client_secret",
            "clientsecret",
            "key",
            "code",
            "refresh_token",
            "refreshtoken"
        };

        _sensitiveBodyPatterns = new List<Regex>
        {
            // JSON patterns for common sensitive fields
            CreateJsonFieldPattern("password"),
            CreateJsonFieldPattern("pwd"),
            CreateJsonFieldPattern("pass"),
            CreateJsonFieldPattern("secret"),
            CreateJsonFieldPattern("api_key"),
            CreateJsonFieldPattern("apikey"),
            CreateJsonFieldPattern("apiKey"),
            CreateJsonFieldPattern("api-key"),
            CreateJsonFieldPattern("access_token"),
            CreateJsonFieldPattern("accesstoken"),
            CreateJsonFieldPattern("accessToken"),
            CreateJsonFieldPattern("access-token"),
            CreateJsonFieldPattern("refresh_token"),
            CreateJsonFieldPattern("refreshtoken"),
            CreateJsonFieldPattern("refreshToken"),
            CreateJsonFieldPattern("refresh-token"),
            CreateJsonFieldPattern("token"),
            CreateJsonFieldPattern("auth"),
            CreateJsonFieldPattern("authorization"),
            CreateJsonFieldPattern("client_secret"),
            CreateJsonFieldPattern("clientsecret"),
            CreateJsonFieldPattern("clientSecret"),
            CreateJsonFieldPattern("client-secret"),
            CreateJsonFieldPattern("private_key"),
            CreateJsonFieldPattern("privatekey"),
            CreateJsonFieldPattern("privateKey"),
            CreateJsonFieldPattern("private-key"),
            CreateJsonFieldPattern("code"),
            CreateJsonFieldPattern("otp"),
            CreateJsonFieldPattern("pin"),
            CreateJsonFieldPattern("cvv"),
            CreateJsonFieldPattern("cvc"),
            CreateJsonFieldPattern("card_number"),
            CreateJsonFieldPattern("cardnumber"),
            CreateJsonFieldPattern("cardNumber"),
            CreateJsonFieldPattern("card-number"),
            CreateJsonFieldPattern("credit_card"),
            CreateJsonFieldPattern("creditcard"),
            CreateJsonFieldPattern("creditCard"),
            CreateJsonFieldPattern("credit-card"),
            CreateJsonFieldPattern("cc_number"),
            CreateJsonFieldPattern("ccnumber"),
            CreateJsonFieldPattern("ccNumber"),
            CreateJsonFieldPattern("account_number"),
            CreateJsonFieldPattern("accountnumber"),
            CreateJsonFieldPattern("accountNumber"),
            CreateJsonFieldPattern("routing_number"),
            CreateJsonFieldPattern("routingnumber"),
            CreateJsonFieldPattern("routingNumber"),
            CreateJsonFieldPattern("ssn"),
            CreateJsonFieldPattern("social_security"),
            CreateJsonFieldPattern("socialsecurity"),
            CreateJsonFieldPattern("socialSecurity"),
            CreateJsonFieldPattern("username"),
            CreateJsonFieldPattern("user_name"),
            CreateJsonFieldPattern("userName"),
            CreateJsonFieldPattern("user"),
            CreateJsonFieldPattern("login"),
            CreateJsonFieldPattern("email"),
            CreateJsonFieldPattern("phone"),
            CreateJsonFieldPattern("mobile"),
            CreateJsonFieldPattern("x-api-key"),
            CreateJsonFieldPattern("bearer"),
            CreateJsonFieldPattern("signature"),
            CreateJsonFieldPattern("cert"),
            CreateJsonFieldPattern("certificate")
        };

        // Add custom sensitive headers
        if (_options.AdditionalSensitiveHeaders != null)
        {
            foreach (var header in _options.AdditionalSensitiveHeaders)
            {
                _sensitiveHeaders.Add(header);
            }
        }

        // Add custom sensitive query parameters
        if (_options.AdditionalSensitiveQueryParams != null)
        {
            foreach (var param in _options.AdditionalSensitiveQueryParams)
            {
                _sensitiveQueryParams.Add(param);
            }
        }

        // Add custom sensitive body field patterns
        if (_options.AdditionalSensitiveBodyFields != null)
        {
            foreach (var field in _options.AdditionalSensitiveBodyFields)
            {
                _sensitiveBodyPatterns.Add(CreateJsonFieldPattern(field));
            }
        }
    }

    private static Regex CreateJsonFieldPattern(string fieldName)
    {
        // Matches JSON field patterns like: "fieldName": "value" or "fieldName":"value"
        // Captures the value (including quoted strings, numbers, booleans)
        var pattern = $@"(""{Regex.Escape(fieldName)}""\s*:\s*)(""[^""\\]*(?:\\.[^""\\]*)*""|[0-9]+(?:\.[0-9]+)?|true|false|null)";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Redacts sensitive information from a header value if the header is sensitive.
    /// </summary>
    public string RedactHeader(string headerName, string headerValue)
    {
        if (!_options.EnableRedaction)
            return headerValue;

        if (_sensitiveHeaders.Contains(headerName))
        {
            return _options.RedactedPlaceholder;
        }

        return headerValue;
    }

    /// <summary>
    /// Checks if a header should be completely excluded from logging.
    /// </summary>
    public bool ShouldExcludeHeader(string headerName)
    {
        if (_options.ExcludedHeaders != null && _options.ExcludedHeaders.Contains(headerName))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Redacts sensitive query parameters from a URI.
    /// </summary>
    public string RedactUri(Uri? uri)
    {
        if (uri == null || !_options.EnableRedaction)
            return uri?.ToString() ?? string.Empty;

        var uriString = uri.ToString();

        if (string.IsNullOrEmpty(uri.Query) || uri.Query == "?")
            return uriString;

        var query = uri.Query.TrimStart('?');
        var parts = query.Split('&');
        var redactedParts = new List<string>();

        foreach (var part in parts)
        {
            var keyValue = part.Split(new[] { '=' }, 2);
            if (keyValue.Length == 2)
            {
                var key = keyValue[0];
                if (_sensitiveQueryParams.Contains(key))
                {
                    redactedParts.Add($"{key}={_options.RedactedPlaceholder}");
                }
                else
                {
                    redactedParts.Add(part);
                }
            }
            else
            {
                redactedParts.Add(part);
            }
        }

        var baseUri = uriString.Substring(0, uriString.IndexOf('?'));
        return baseUri + "?" + string.Join("&", redactedParts);
    }

    /// <summary>
    /// Redacts sensitive fields from request body content.
    /// </summary>
    public string RedactBody(string content)
    {
        if (string.IsNullOrEmpty(content) || !_options.EnableRedaction)
            return content;

        var redactedContent = content;

        foreach (var pattern in _sensitiveBodyPatterns)
        {
            redactedContent = pattern.Replace(redactedContent, match =>
            {
                // Keep the field name and colon, replace only the value
                return match.Groups[1].Value + $"\"{_options.RedactedPlaceholder}\"";
            });
        }

        return redactedContent;
    }
}
