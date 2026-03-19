# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| latest  | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in AriaAPI, please report it responsibly.

**Do not open a public GitHub issue for security vulnerabilities.**

Instead, please report vulnerabilities by emailing **Dominic DiCostanzo** directly or by using [GitHub's private vulnerability reporting](https://github.com/ddicostanzo/AriaAPI/security/advisories/new).

### What to include

- A description of the vulnerability
- Steps to reproduce the issue
- The potential impact
- Any suggested fixes (optional)

### Response timeline

- **Acknowledgment**: Within 48 hours of receipt
- **Initial assessment**: Within 7 days
- **Resolution target**: Within 30 days for confirmed vulnerabilities

### Scope

This project is a class library (SDK) and does not run as a standalone service. Security considerations primarily involve:

- **Credential handling**: OAuth2 client secrets and token management
- **Data exposure**: Patient health information (PHI) handled via FHIR resources
- **Dependency vulnerabilities**: Third-party NuGet package security

## Built-in Security Controls

### PHI Protection (HIPAA)

This library handles Protected Health Information. The following controls are in place:

- **`PhiMask.Mask()`** — All patient identifiers (MRNs, patient IDs, practitioner names) are hashed (SHA-256, 8-char hex) before logging. Never log PHI in plain text.
- **Sensitive header filtering** — `Authorization`, `X-Client-Secret`, `Password`, `PhoneNumber`, and `SSN` headers are stripped from HTTP log output.

### Credential & Secret Management

- **OAuth2 token caching** — Tokens are cached in-memory via `IMemoryCache` with a 30-second expiry skew to prevent reuse of expired tokens.
- **`detect-secrets` pre-commit hook** — Powered by [Yelp/detect-secrets](https://github.com/Yelp/detect-secrets) (v1.4.0), scans every commit for credential-like strings against a tracked `.secrets.baseline`.
- **No secrets in source** — `App.config` is git-ignored. Configuration uses [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or `IOptionsMonitor<FhirOptions>`.

### HTTP Pipeline

The delegating handler chain enforces security at the transport layer:

- **`BearerTokenHandler`** — Injects OAuth2 bearer tokens; never exposes raw credentials to callers.
- **`LoggingTimingHandler`** — Logs request/response metadata with PHI masking; no raw bodies logged.
- **`Http2RequestVersionHandler`** — Enforces HTTP/2 for TLS-secured connections.

## Security Best Practices for Consumers

- Store credentials using [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or a secure vault — never in source control
- Keep dependencies up to date with `dotnet list package --vulnerable`
- Follow your organization's HIPAA and data governance policies when handling FHIR resources containing PHI
- Implement resource-level authorization in your consuming application — AriaAPI does not enforce patient-level access control
- Add per-access audit logging (user + patient + action) in your application layer for HIPAA compliance
- Do not expose FHIR SDK exception messages to end users — they may contain PHI or internal system details
