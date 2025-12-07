using System.Collections.Generic;

namespace AmirFT.Http.CurlLogging;

/// <summary>
/// Configuration options for the CurlLoggingHandler.
/// </summary>
public class CurlLoggingOptions
{
    /// <summary>
    /// Gets or sets whether sensitive data redaction is enabled.
    /// Default is true.
    /// </summary>
    public bool EnableRedaction { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log HTTP response details (status, headers, body, elapsed time).
    /// Default is true.
    /// </summary>
    public bool LogResponse { get; set; } = true; 
    

    /// <summary>
    /// Gets or sets the placeholder text used when redacting sensitive values.
    /// Default is "[REDACTED]".
    /// </summary>
    public string RedactedPlaceholder { get; set; } = "[REDACTED]";

    /// <summary>
    /// Gets or sets additional header names to treat as sensitive (beyond the built-in list).
    /// Headers are matched case-insensitively.
    /// </summary>
    /// <remarks>
    /// Built-in sensitive headers include: Authorization, X-API-Key, X-Auth-Token,
    /// X-Access-Token, Cookie, Set-Cookie, X-CSRF-Token, X-XSRF-Token, etc.
    /// </remarks>
    public ICollection<string>? AdditionalSensitiveHeaders { get; set; }

    /// <summary>
    /// Gets or sets header names to completely exclude from the curl output.
    /// Use this when you don't want a header to appear at all (not even redacted).
    /// </summary>
    public ICollection<string>? ExcludedHeaders { get; set; }

    /// <summary>
    /// Gets or sets additional query parameter names to treat as sensitive.
    /// Parameters are matched case-insensitively.
    /// </summary>
    /// <remarks>
    /// Built-in sensitive query params include: api_key, apikey, access_token, token,
    /// auth, password, secret, client_secret, key, code, refresh_token, etc.
    /// </remarks>
    public ICollection<string>? AdditionalSensitiveQueryParams { get; set; }

    /// <summary>
    /// Gets or sets additional JSON field names to treat as sensitive in request bodies.
    /// Fields are matched case-insensitively.
    /// </summary>
    /// <remarks>
    /// Built-in sensitive fields include: password, secret, api_key, token, authorization,
    /// client_secret, private_key, code, otp, pin, cvv, card_number, credit_card,
    /// account_number, ssn, username, email, phone, etc.
    /// </remarks>
    public ICollection<string>? AdditionalSensitiveBodyFields { get; set; }

    /// <summary>
    /// Creates a new instance of CurlLoggingOptions with default settings.
    /// </summary>
    public static CurlLoggingOptions Default => new CurlLoggingOptions();

    /// <summary>
    /// Creates a new instance of CurlLoggingOptions with redaction disabled.
    /// Use this when you want to log all data without any redaction.
    /// </summary>
    public static CurlLoggingOptions NoRedaction => new CurlLoggingOptions { EnableRedaction = false };
}
