# AriaAPI — Claude Code Context

## Project Overview

A **.NET 10 class library** (C# SDK) for interacting with the **Varian Aria oncology information system** via its FHIR R4 API. This is a library, not a standalone executable — it is consumed as a NuGet package or project reference by other .NET applications.

**Domain:** Radiation oncology / healthcare IT. All resources map to HL7 FHIR R4 types.

---

## Compliance Profile (HIPAA)

**This project handles Protected Health Information (PHI).** All development decisions must account for HIPAA obligations:

- **PHI must never appear in plain-text logs.** Use `PhiMask.Mask(value)` (SHA-256, 8-char hex) for all identifiers logged — MRNs, patient IDs, practitioner names.
- **Sensitive HTTP headers** are filtered from logs: `Authorization`, `X-Client-Secret`, `Password`, `PhoneNumber`, `SSN`.
- Any finding that risks PHI exposure scores **90+ on Security** in code review.
- No credentials, connection strings, or secrets in source. `detect-secrets` pre-commit hook enforces this.

---

## Tech Stack

- **Runtime:** .NET 10, `OutputType=Library`
- **Nullable:** enabled — use `?` for nullable refs; non-null is the default
- **ImplicitUsings:** disabled — all `using` statements must be explicit
- **FHIR SDK:** `Hl7.Fhir.R4` v6.0.1 (Firely SDK)
- **DI:** `Microsoft.Extensions.DependencyInjection` v10.0.1
- **Caching:** `Microsoft.Extensions.Caching.Memory` v10.0.1
- **Config:** .NET User Secrets (`FhirOptions` section); `UserSecretsId=f39d63b7-efd2-4b51-a980-129f9f4365b8`
- **Other:** `CsvHelper` v33.1.0, `BenchmarkDotNet` v0.15.8
- **Excluded from build:** `Microsoft.Data.SqlClient` v6.1.4, `DocumentFormat.OpenXml` v3.3.0 (SQL/Workflow dirs excluded)

---

## Project Structure

```
AriaAPI/                            ← repo root
├── AriaAPI.sln
├── AriaAPI/                        ← library project root (AriaAPI.csproj)
│   ├── API/
│   │   ├── Create/                 # DocumentReferenceCreate.cs, TaskCreate.cs, CreateHelpers.cs
│   │   ├── IdentityResolvers/      # IPatientResolver, PatientResolver, IPractitionerResolver, PractitionerResolver
│   │   ├── MultiResourceSearch/    # PatientAppointments, PatientTasks, PatientDocuments, PatientDocumentAppointment,
│   │   │                           # PatientCareTeam, PatientObservations, PatientEncounters
│   │   ├── Operations/             # PatientOperations (EverythingAsync), ValueSetOperations (ExpandAsync)
│   │   ├── SearchHelpers/
│   │   │   └── SearchTypes/        # Per-resource enums/mappings (AppointmentCategory, DocumentType, etc.)
│   │   ├── SingleResourceSearch/   # 34 FHIR resource search classes (Patient, Appointment, Observation, …)
│   │   └── Write/                  # CareTeamWrite, AppointmentWrite (UpdateAsync, UpsertAsync, patient-centric overloads)
│   ├── Core/                       # FhirService, FhirClientFactory, FhirOptions, ClientConfigurator
│   │                               # HTTP pipeline: BearerTokenHandler, CurrencySanitizerHandler,
│   │                               # DefaultQueryParamsHandler, Http2RequestVersionHandler,
│   │                               # LoggingTimingHandler, RawCaptureHandler
│   │                               # FanOutSearchHelper, PhiMask, CodeableConcept, NameFormatting, Helpers
│   ├── Networking/
│   │   ├── Core/                   # AriaAPIClient<T>, Builder<T>, ClientConfigurator, Factory, FhirAction
│   │   └── Helpers/                # BuilderExtensions, FhirIncludeEnums, FhirActionExtensions, Ensure
│   ├── Security/                   # TokenProvider (OAuth2 client_credentials + IMemoryCache, 30s expiry skew)
│   ├── Config/                     # EXCLUDED from build
│   ├── Resources/                  # EXCLUDED from build
│   ├── SQL/                        # EXCLUDED from build
│   └── Workflows/                  # EXCLUDED from build (Special Treatment Procedure document generation)
└── AriaAPI.Tests/                  # xUnit test project (277 tests across 19 test classes)
```

**Build exclusions:** `Config\**`, `Resources\**`, `SQL\**`, `Workflows\**`, `Networking\Helpers\Includes.cs`, `Program.cs`, `Program_Examples.cs` are excluded from the default build via `.csproj` glob removes.

---

## Build & Test Commands

```bash
# Build (from repo root)
dotnet build AriaAPI.sln

# Build release
dotnet build AriaAPI.sln -c Release

# Run tests
dotnet test AriaAPI.Tests/AriaAPI.Tests.csproj

# Pack NuGet
dotnet pack AriaAPI/AriaAPI.csproj -o ./out

# Restore
dotnet restore AriaAPI.sln
```

**Build must succeed with 0 errors and 0 new warnings** (XML doc generation is enabled; missing docs produce warnings).

---

## Commit Style

```
type: short description
```

Types: `feat`, `fix`, `docs`, `chore`, `refactor`, `test`
Branch target: `master`
Examples from git log:
- `feat: add Encounter search class`
- `fix: cap unbounded result sets`
- `docs: update CHANGELOG for PR #8`
- `chore: delete excluded dirs`

---

## Code Style & Conventions

### Naming
- PascalCase: classes, methods, properties, enums
- camelCase with `_` prefix: private fields (`_factory`, `_syncRoot`)
- Interfaces prefixed with `I` (`IPatientResolver`, `IFactory`)
- `sealed` on classes where inheritance is not needed

### Patterns
- **Fluent builder:** `Builder<TResource>` for FHIR `SearchParams` construction
- **Delegating handlers:** HTTP pipeline chain — `SocketsHttpHandler → Http2 → DefaultQueryParams → CurrencySanitizer → Logging → BearerToken`
- **DI extension methods:** register via `IServiceCollection.AddAriaFhirClient(config)`
- **Guard clauses:** `Ensure.NotNullOrWhiteSpace()` from `AriaAPI.Networking.Helpers`
- **Factory pattern:** `AriaClientFactory`, `FhirClientFactory`, `IFactory`
- **Options pattern:** `IOptionsMonitor<FhirOptions>` for live config reload
- **Record types:** for internal DTOs (e.g., `TokenResponse`)

### Thread Safety
- Use `lock (_syncRoot)` for shared mutable state
- `IDisposable` pattern: idempotent `Dispose()`, `ThrowIfDisposed()` guard
- `Volatile.Read` for thread-safe static field access

### Error Handling
- Throw `ArgumentNullException` for null constructor args
- Throw `InvalidOperationException` for invalid state / missing config
- Validate at boundaries (constructor, public methods); don't validate internally

### XML Documentation
- All public types and members require `/// <summary>` XML doc comments
- `<GenerateDocumentationFile>True</GenerateDocumentationFile>` — missing docs produce build warnings
- Use `<remarks>` for non-obvious behavior (thread safety, DI lifecycle, etc.)

### Namespaces
Follow directory structure: `AriaAPI.Core`, `AriaAPI.Networking.Core`, `AriaAPI.API.SingleResourceSearch`, `AriaAPI.API.Write`, `AriaAPI.API.Operations`, etc.

---

## Task Completion Checklist

Before marking any coding task complete:

1. `dotnet build AriaAPI.sln` — 0 errors, 0 new warnings
2. `dotnet test AriaAPI.Tests/AriaAPI.Tests.csproj` — all tests pass
3. All new public types/members have XML doc comments (`/// <summary>`)
4. No nullable reference warnings introduced
5. PHI never logged in plain text — use `PhiMask.Mask()` for all identifiers
6. No secrets, credentials, or connection strings in source
7. `detect-secrets scan --baseline .secrets.baseline` if adding any string resembling a secret
8. New functionality follows existing patterns (builder, delegating handlers, DI)
9. `Ensure.NotNullOrWhiteSpace()` used for string guard clauses

---

## SingleResourceSearch Pattern

Every search class follows the same structure:

```csharp
// Sealed static class
public static class FooSearch
{
    public const int ListReturnLimit = 500;  // always capped

    // Nested record for search parameters
    public record SearchParams(string? PatientId = null, ...);

    // Core search method
    public static async Task<List<Foo>> SearchFoosAsync(
        ClientConfigurator configurator,
        SearchParams searchParams,
        CancellationToken ct = default) { ... }

    // Convenience overloads
    public static Task<List<Foo>> ByPatientAsync(...) { ... }
}
```

- All search classes use `SearchExecutor.ExecuteAsync<T>()` for boilerplate (null guard, `ForResource<T>(ct)`, result trim, fan-out)
- Default page size: `SearchExecutor.DefaultPageSize = 200`; server max: `DefaultServerMaxResults = 500`
- Fan-out: multi-valued parameters use `FanOutSearchHelper` (OR within param, AND across params, dedup by `Resource.Id`)

---

## Implemented FHIR Resource Types (37)

`Patient`, `Appointment`, `Observation`, `Procedure`, `Condition`, `CarePlan`, `CareTeam`, `AllergyIntolerance`, `DocumentReference`, `Task`, `ServiceRequest`, `Device`, `Group`, `HealthcareService`, `Location`, `Organization`, `Practitioner`, `BodyStructure`, `ChargeItem`, `ActivityDefinition`, `ValueSet`, `Coverage`, `DiagnosticReport`, `Encounter`, `ImagingStudy`, `Immunization`, `MedicationAdministration`, `MedicationRequest`, `NutritionOrder`, `PractitionerRole`, `RelatedPerson`, `RiskAssessment`, `Schedule`, `Slot` + 3 more.

---

## Known Gaps & Missing Coverage

### Infrastructure Gaps
- **No circuit breaker** for downed FHIR endpoints
- **No FHIR response caching** (only OAuth token is cached)
- **No request compression** (no GZIP)
- **No OpenTelemetry integration** for metrics/tracing

---

## Open Code Review Findings

From `docs/code-reviews/2026-03-12-full-repository-review.md` — these are in **other projects** (not AriaAPI library itself) but worth noting:

| Finding | File | Severity |
|---|---|---|
| Exception messages leaked to HTTP clients (`ex.Message` in responses) | `MIM_Aria_Document_Request` | HIGH |
| `AllowedHosts: "*"` — Host Header Injection risk | `MIM_Aria_Document_Request/appsettings.json` | HIGH |
| No resource-level authorization (any authed user sees any patient data) | `MIM_Aria_Document_Request` | HIGH (HIPAA) |
| No per-access audit trail (user + patient + action) | `MIM_Aria_Document_Request` | HIGH (HIPAA) |

These findings are in dependent projects, not in `AriaAPI/` itself.

---

## Security Notes

- Pre-commit: `detect-secrets` scans for credential-like strings on every commit
- `.secrets.baseline` is tracked in git
- `App.config` is git-ignored
- Never use `--no-verify` to bypass the pre-commit hook — fix the issue instead
