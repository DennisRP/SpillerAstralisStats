## Context

The repository currently contains only OpenSpec configuration and change artifacts. The skeleton must satisfy `specs/project-bootstrap/spec.md` and establish the .NET 8 isolated Azure Functions host that will later retrieve six to twelve months of PandaScore history. The first function deliberately performs no external I/O and only logs a greeting. See `proposal.md` for motivation.

## Goals / Non-Goals

**Goals:**

- Establish conventional Function App and test projects that work with standard .NET and Azure Functions tooling.
- Define the real daily trigger shape now so later history retrieval can replace the placeholder behavior without changing the host model.
- Make the timer metadata and greeting behavior testable without starting the Functions host or using deployed Azure resources.
- Document the minimal commands and local prerequisites needed to restore, build, test, and run the project.

**Non-Goals:**

- Retrieve or model PandaScore data in this change.
- Introduce application layers, persistence, deployment infrastructure, or LLM integration before they are needed.
- Integrate with or change the independently operated Node.js live-match application.
- Guarantee exactly-once invocation; future data processing must instead be idempotent.

## Decisions

### Use a .NET 8 isolated Function App and one xUnit test project

Create `src/SpillerAstralisStats/SpillerAstralisStats.csproj` as an executable .NET 8 isolated-worker Function App and `src/SpillerAstralisStats.Tests/SpillerAstralisStats.Tests.csproj` as its xUnit test project. The root `SpillerAstralisStats.sln` includes both.

The Function App uses the isolated worker core packages and the timer extension package. The isolated model provides normal .NET hosting and dependency injection for later integrations. Additional domain or infrastructure projects are deferred until real PandaScore responsibilities justify them.

### Encode the initial schedule directly on the timer trigger

Add one function with `[TimerTrigger("0 37 13 * * *", RunOnStartup = true)]`. Keeping the NCRONTAB expression direct and `RunOnStartup` static makes the initial behavior explicit and avoids configuration indirection for a value that is not yet environment-specific.

`RunOnStartup = true` can cause additional invocations when a deployed host restarts or scales. This is accepted for the skeleton. Before the placeholder is replaced, history retrieval must be designed as an idempotent operation so repeated or overlapping invocations do not duplicate or corrupt generated state. Production can later remove the startup behavior without restructuring the function.

### Keep the function body minimal and use structured logging

Inject `ILogger<DailyStatsFunction>` and have the trigger method log `Hello World` as a message template with no dynamic values. `Program.cs` only configures and runs the isolated Functions worker host. This follows the thin-trigger convention and leaves a clear point for later orchestration.

Writing directly to standard output was considered but rejected because Functions applications should use the host's logging pipeline for consistent local and deployed diagnostics.

### Verify behavior without launching the Functions host in unit tests

Use a small test-only `ILogger<T>` implementation to capture the formatted message without adding a mocking package. Invoke the function method directly to verify the exact `Hello World` message. A separate reflection-based test reads `TimerTriggerAttribute` from the function parameter and verifies both the literal schedule and `RunOnStartup` value.

Starting the full Functions host in the automated test suite was considered but rejected for this skeleton because it would require Azure Functions Core Tools and storage emulation in the test environment. Local host startup remains a documented manual integration check.

### Include Functions host configuration and a safe local settings template

Place `host.json` with the Function App project. Provide a non-secret local settings example containing `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` and `AzureWebJobsStorage=UseDevelopmentStorage=true`; keep the developer's actual `local.settings.json` ignored because it is environment-specific and can later contain secrets.

The root `README.md` documents copying the example, starting Azurite, and running the app with Azure Functions Core Tools. A standard .NET/Azure Functions `.gitignore` prevents build output, local settings, and tooling artifacts from being committed.

### Keep project configuration local until repetition warrants centralization

Both projects target `net8.0`, enable nullable reference types, and use implicit global usings in their project files. A root `Directory.Build.props` is deferred because two small project files do not yet justify another configuration layer.

## Risks / Trade-offs

- [Static `RunOnStartup = true` can invoke the function unexpectedly after deployment] → Keep the placeholder side-effect free and require later history processing to be idempotent before it introduces writes.
- [Timer triggers require Azure Storage and local Functions tooling] → Document Azure Functions Core Tools and Azurite, and keep unit tests independent of both.
- [Reflection tests couple to trigger metadata] → This metadata is intentional externally observable scheduling configuration, so a focused regression test is appropriate.
- [The test framework and Functions worker add NuGet restore dependencies] → Use only the standard worker, timer, and xUnit package set and keep application-specific dependencies out of the skeleton.
- [Future architecture will require adding projects or moving responsibilities] → Defer those boundaries until concrete PandaScore and domain requirements make them clear.
