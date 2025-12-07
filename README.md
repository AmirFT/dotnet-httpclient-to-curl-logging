<p align="center">
  <img src="https://img.shields.io/nuget/v/AmirFT.Http.CurlLogging?style=for-the-badge&logo=nuget&logoColor=white&color=004880" alt="NuGet Version" />
  <img src="https://img.shields.io/nuget/dt/AmirFT.Http.CurlLogging?style=for-the-badge&logo=nuget&logoColor=white&color=004880" alt="NuGet Downloads" />
  <img src="https://img.shields.io/github/license/Am1rFT/CurlLogging?style=for-the-badge&color=yellow" alt="License" />
</p>

<h1 align="center">AmirFT.Http.CurlLogging</h1>

<p align="center">
  <strong>Transform your HTTP requests into ready-to-use cURL commands</strong>
</p>

<p align="center">
  A lightweight <code>DelegatingHandler</code> that automatically logs all HttpClient requests as cURL commands.<br/>
  Perfect for debugging, API documentation, and reproducing issues.
</p>

---

## Why I Built This

I've been there — multiple times in production, dealing with errors from third-party APIs. The support team asks: *"Can you send us the cURL command to reproduce the issue?"*

And I didn't have it.

Manually reconstructing HTTP requests as cURL commands is tedious and error-prone. So I built this library to solve that problem once and for all. Now every HTTP request my applications make is automatically logged as a ready-to-use cURL command. When issues arise, I just grab the command from the logs and share it — no more guesswork.

---

## Features

- **Zero Configuration** - Just add the handler and start logging
- **Full Request Capture** - Headers, body, and query parameters
- **Copy & Paste Ready** - Output commands work directly in terminal
- **Sensitive Data Redaction** - Automatically redacts passwords, tokens, API keys, credit cards, and more
- **Multi-Framework Support** - .NET Standard 2.0, .NET 6, .NET 8, .NET 10

---

## Installation

```bash
dotnet add package AmirFT.Http.CurlLogging
```

**Package Manager:**
```powershell
Install-Package AmirFT.Http.CurlLogging
```

---

## Quick Start

### With Dependency Injection (Recommended)

```csharp
// Program.cs or Startup.cs
services.AddTransient<CurlLoggingHandler>();

services.AddHttpClient("MyApi")
    .AddHttpMessageHandler<CurlLoggingHandler>();
```

### Direct Usage

```csharp
var logger = loggerFactory.CreateLogger<CurlLoggingHandler>();
var handler = new CurlLoggingHandler(logger)
{
    InnerHandler = new HttpClientHandler()
};

using var client = new HttpClient(handler);
await client.PostAsync("https://api.example.com/users", content);
```

---

## Output Example

When you make an HTTP request:

```csharp
await client.PostAsync("https://api.example.com/users",
    new StringContent("{\"name\":\"John\"}", Encoding.UTF8, "application/json"));
```

You'll see this in your logs:

```bash
curl -X POST 'https://api.example.com/users' \
  -H 'Content-Type: application/json; charset=utf-8' \
  -d '{"name":"John"}'
```

---

## Sensitive Data Redaction

By default, sensitive information is automatically redacted from logs to prevent accidental exposure of credentials and personal data.

### What Gets Redacted

| Category | Examples |
|----------|----------|
| **Headers** | Authorization, X-API-Key, X-Auth-Token, Cookie, X-CSRF-Token |
| **Query Parameters** | api_key, token, password, secret, access_token, code |
| **Body Fields** | password, apiKey, token, credit_card, cvv, ssn, username, email |

### Example Output

```csharp
// Request with sensitive data
await client.PostAsync("https://api.example.com/login?api_key=secret123",
    new StringContent("{\"username\":\"john\",\"password\":\"mypassword\"}",
    Encoding.UTF8, "application/json"));
```

Logged output (with redaction):
```bash
curl -X POST 'https://api.example.com/login?api_key=[REDACTED]' \
  -H 'Authorization: [REDACTED]' \
  -H 'Content-Type: application/json; charset=utf-8' \
  -d '{"username":"[REDACTED]","password":"[REDACTED]"}'
```

### Configuration Options

```csharp
var options = new CurlLoggingOptions
{
    // Enable/disable redaction (default: true)
    EnableRedaction = true,

    // Custom placeholder text (default: "[REDACTED]")
    RedactedPlaceholder = "***",

    // Add custom sensitive headers
    AdditionalSensitiveHeaders = new[] { "X-Custom-Secret" },

    // Completely exclude headers from output
    ExcludedHeaders = new[] { "X-Internal-Header" },

    // Add custom sensitive query parameters
    AdditionalSensitiveQueryParams = new[] { "session_id" },

    // Add custom sensitive body fields
    AdditionalSensitiveBodyFields = new[] { "social_security_number", "bank_account" }
};

var handler = new CurlLoggingHandler(logger, options);
```

### With Dependency Injection

```csharp
// Register with custom options
services.AddSingleton(new CurlLoggingOptions
{
    RedactedPlaceholder = "***",
    AdditionalSensitiveHeaders = new[] { "X-Custom-Auth" }
});

services.AddTransient<CurlLoggingHandler>();

services.AddHttpClient("MyApi")
    .AddHttpMessageHandler<CurlLoggingHandler>();
```

### Disable Redaction

If you need to see the full request data (e.g., in development):

```csharp
var options = CurlLoggingOptions.NoRedaction;
var handler = new CurlLoggingHandler(logger, options);
```

---

## Supported Frameworks

| Framework | Version |
|-----------|---------|
| .NET Standard | 2.0+ |
| .NET | 6.0+ |
| .NET | 8.0+ |
| .NET | 10.0+ |

---

## Contributing

Contributions are welcome! Feel free to:

- Report bugs
- Suggest features
- Submit pull requests

---

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Made with :heart: by <a href="https://github.com/AmirFT">AmirFT</a>
</p>
