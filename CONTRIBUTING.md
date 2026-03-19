# Contributing to AriaAPI

Thank you for your interest in contributing to AriaAPI! This document provides guidelines and instructions for contributing.

## Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/<your-username>/AriaAPI.git
   ```
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A C# IDE (Visual Studio 2022, VS Code with C# Dev Kit, or JetBrains Rider)

### Building

```bash
dotnet build AriaAPI.sln
```

### Pre-commit Hooks

This repository uses [detect-secrets](https://github.com/Yelp/detect-secrets) to prevent accidental credential commits.

To set up:
```bash
pip install pre-commit detect-secrets
pre-commit install
```

The hook runs automatically on every commit. If it flags a false positive, update the baseline:
```bash
detect-secrets scan --baseline .secrets.baseline
```

### Running Tests

Always run the full test suite before submitting a pull request:
```bash
dotnet test AriaAPI.Tests/AriaAPI.Tests.csproj
```

All 277 tests must pass. No live FHIR server is required.

## How to Contribute

### Reporting Bugs

- Use [GitHub Issues](https://github.com/ddicostanzo/AriaAPI/issues) to report bugs
- Include steps to reproduce, expected behavior, and actual behavior
- Include your .NET version and operating system

### Suggesting Features

- Open a [GitHub Issue](https://github.com/ddicostanzo/AriaAPI/issues) with the **enhancement** label
- Describe the use case and why it would be valuable

### Submitting Changes

1. Ensure your code follows the existing code style and conventions in the project
2. Write clear, descriptive commit messages
3. Keep pull requests focused — one feature or fix per PR
4. Update documentation if your changes affect the public API
5. Submit a [Pull Request](https://github.com/ddicostanzo/AriaAPI/pulls) against the `master` branch

### Code Style

- Follow standard [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use the `.editorconfig` settings included in the repository
- Use meaningful names for classes, methods, and variables
- Keep methods focused and concise

### FHIR-Specific Guidelines

- Follow [HL7 FHIR R4](https://hl7.org/fhir/R4/) naming conventions for resource-related code
- Use the Firely .NET SDK types (`Hl7.Fhir.R4`) rather than custom models where possible
- Document any Aria-specific FHIR extensions or behaviors

## Pull Request Process

1. Fill out the PR description with a summary of changes
2. Link any related issues
3. Ensure the project builds without errors
4. A maintainer will review your PR and may request changes
5. Once approved, a maintainer will merge your PR

## License

By contributing to AriaAPI, you agree that your contributions will be licensed under the [GNU Affero General Public License v3.0](LICENSE.txt).

## Questions?

If you have questions about contributing, feel free to open an issue for discussion.
