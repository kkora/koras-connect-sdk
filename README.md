# Koras Connect SDK

Independent .NET 10 integration SDK. No code or architecture imported from kkora/AdpIntegration.

## Projects
- Koras.Connect: provider-neutral OAuth client-credentials, HTTP transport, and certificate primitives.
- Koras.Connect.Adp: initial ADP Workers read API with asynchronous pagination.
- Koras.Connect.Tests: isolated HTTP tests.

## Local verification
```sh
dotnet restore tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj
dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj --configuration Release
dotnet pack src/Koras.Connect.Adp/Koras.Connect.Adp.csproj --configuration Release
```

## Status
G0/G1 initial design and foundation committed. G2/G3 initial implementation, not feature-complete. CI and live ADP verification pending. No packages published or resources deployed.
