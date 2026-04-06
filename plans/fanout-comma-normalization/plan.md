# Plan: FanOut Comma Normalization

> Source spec: docs/specs/2026-04-06-fanout-comma-normalization-design.md
> Project profile: project-profile.yml
> Created: 2026-04-06

## Tasks

### task-01: Add PreserveCommas field and comma normalization to FanOutSearchHelper
**Depends on**: (none)
**Files to create/modify**:
- `AriaAPI/Core/FanOutSearchHelper.cs`
- `AriaAPI.Tests/FanOut/FanOutSearchHelperTests.cs`

**Test approach**: Use the existing `ParamMappedExecutor` test pattern from `FanOutSearchHelperTests` to verify that comma-separated values produce separate queries. Each test creates `FanOutParam` instances with comma-containing values and asserts that the query executor receives individual values, not raw comma strings.

**Acceptance criteria**:
- [ ] `FanOutParam` record struct has a `bool PreserveCommas = false` third parameter
- [ ] `SeparateParams` splits values on commas, trims whitespace, drops empties when `PreserveCommas` is false
- [ ] `SeparateParams` passes values through unchanged when `PreserveCommas` is true
- [ ] Test: `["valA,valB"]` with `PreserveCommas=false` produces two separate queries with `"valA"` and `"valB"`
- [ ] Test: `["valA,valB", "valC"]` produces three separate queries
- [ ] Test: `["valA , valB"]` trims to `["valA", "valB"]`
- [ ] Test: `["valA,,valB"]` drops empty segment, produces `["valA", "valB"]`
- [ ] Test: `["valA,valB"]` with `PreserveCommas=true` produces one query with raw `"valA,valB"`
- [ ] All 7 existing `FanOutSearchHelperTests` pass unchanged
- [ ] `dotnet build AriaAPI.sln` succeeds with 0 errors

### task-02: Update PatientSearch to opt out of comma splitting
**Depends on**: task-01
**Files to create/modify**:
- `AriaAPI/API/SingleResourceSearch/PatientSearch.cs`

**Test approach**: Verify build succeeds and existing tests pass. PatientSearch's identifier handling intentionally comma-joins OR groups; adding `PreserveCommas: true` preserves this behavior.

**Acceptance criteria**:
- [ ] `PatientSearch` identifier `FanOutParam` uses `PreserveCommas: true`
- [ ] `dotnet build AriaAPI.sln` succeeds with 0 errors
- [ ] `dotnet test AriaAPI.Tests/AriaAPI.Tests.csproj` — all existing tests pass
- [ ] XML doc comment on `PreserveCommas` explains when to use it

### task-03: Final verification and cleanup
**Depends on**: task-01, task-02
**Files to create/modify**: (none — verification only)

**Test approach**: Run full build and test suite. Verify no new warnings introduced.

**Acceptance criteria**:
- [ ] `dotnet build AriaAPI.sln` — 0 errors, 0 new warnings
- [ ] `dotnet test AriaAPI.Tests/AriaAPI.Tests.csproj` — all tests pass (existing + new)
- [ ] No nullable reference warnings introduced

## Dependency Graph

```
task-01 ──→ task-02 ──→ task-03
task-01 ─────────────→ task-03
```
