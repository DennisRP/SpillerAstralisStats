## 1. Solution and Function App Structure

- [x] 1.1 Create the root `SpillerAstralisStats.sln`, the .NET 8 isolated Function App under `src/SpillerAstralisStats/`, and the xUnit project under `src/SpillerAstralisStats.Tests/`; verify both projects are listed by the solution.
- [x] 1.2 Add isolated worker and timer extension packages, configure nullable reference types and implicit usings, and add the test-to-application project reference; verify `dotnet restore` and `dotnet build` complete successfully from the repository root.
- [x] 1.3 Add `host.json` and a non-secret `local.settings.json.example` with `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` and `AzureWebJobsStorage=UseDevelopmentStorage=true`; verify the settings template is valid JSON and the actual local settings path is ignored.
- [x] 1.4 Add a standard .NET/Azure Functions `.gitignore` and a root `README.md` describing the layout, prerequisites, restore, build, test, local settings, Azurite, and Functions Core Tools run commands; verify the documented paths match the created solution.

## 2. Scheduled Function

- [x] 2.1 Implement the isolated worker `Program.cs` host configuration and a timer-triggered function with `TimerTrigger("0 37 13 * * *", RunOnStartup = true)`; verify the project builds and the Functions host discovers the function when started locally.
- [x] 2.2 Log exactly `Hello World` for each timer invocation using the Functions logging pipeline; verify a local host startup invocation produces the expected log message.

## 3. Automated Tests and Validation

- [x] 3.1 Add an xUnit test-only logger and test the exact `Hello World` log message by invoking the function directly; verify the targeted test passes without external services.
- [x] 3.2 Add a reflection-based test for the timer trigger's literal schedule and `RunOnStartup` value; verify the targeted test passes.
- [x] 3.3 Run `dotnet restore`, `dotnet build`, and `dotnet test` against the root solution, then run the local Functions host when Core Tools and Azurite are available; verify all available checks succeed and no PandaScore or external API call is made.
