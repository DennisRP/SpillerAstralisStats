# project-bootstrap Specification

## Purpose

Establishes a minimal, runnable, and testable scheduled-function foundation for incrementally building the SpillerAstralisStats application.

## Requirements

### Requirement: Repository layout separates source from root files
The project SHALL keep the solution, documentation, and general repository files at the repository root, and SHALL keep both application code and automated test code beneath the `src/` directory.

#### Scenario: Inspecting the initial repository layout
- **WHEN** a developer inspects the completed project skeleton
- **THEN** the solution and general files are present at the repository root
- **AND** the application project and test project are present beneath `src/`

### Requirement: Function App runs on .NET 8
The application SHALL be a .NET 8 isolated Azure Function App and SHALL be runnable with the local Azure Functions host from a clean checkout after dependencies are restored.

#### Scenario: Starting the Function App locally
- **WHEN** a developer starts the application using the local Azure Functions host and the documented local prerequisites
- **THEN** the host starts successfully and discovers the timer-triggered function

### Requirement: Function executes on the daily schedule
The Function App SHALL invoke the timer-triggered function according to the NCRONTAB expression `0 37 13 * * *`.

#### Scenario: Reaching the scheduled time
- **WHEN** the Functions host reaches a time matching `0 37 13 * * *`
- **THEN** the timer-triggered function is invoked

### Requirement: Function executes when the host starts
The Function App SHALL invoke the timer-triggered function whenever the Functions host starts.

#### Scenario: Starting the Functions host
- **WHEN** the Functions host starts and discovers the timer-triggered function
- **THEN** the function is invoked without waiting for the next scheduled occurrence

### Requirement: Function logs the initial greeting
Each invocation of the timer-triggered function SHALL emit a log message whose message text is exactly `Hello World`.

#### Scenario: Observing an invocation
- **WHEN** the timer-triggered function is invoked by either startup or its schedule
- **THEN** it emits a log message with the exact message text `Hello World`

### Requirement: Skeleton behavior is covered by an automated test
The solution SHALL include automated tests that verify the timer configuration and the function's initial greeting behavior.

#### Scenario: Running the solution tests
- **WHEN** a developer runs the solution's automated tests
- **THEN** the daily schedule, startup invocation configuration, and exact greeting message are verified without requiring network access, credentials, or deployed Azure resources
