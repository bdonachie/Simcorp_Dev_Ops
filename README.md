# Dimension.ServiceControl

A .NET library and PowerShell module for driving `ScdSvcCtl.exe`, the service control tool that comes with SimCorp Dimension.

Dimension, `ScdSvcCtl.exe` and the XML it produces are all SimCorp's. This just wraps the tool, because typed objects in .NET and PowerShell were a lot easier to work with than the bare exe. It's not affiliated with SimCorp in any way.

Both projects target netstandard2.0, so the same build of the module works in Windows PowerShell 5.1 and PowerShell 7.

## Why I wrote it

I used to have a function called `get-dimension-info` that ran `ScdSvcCtl.exe service list` and pulled the printed table apart by hand. It had problems.

| What the old script did | What this does |
| --- | --- |
| Told agent lines from service lines with `$_.Length -gt 100` | Reads the actual `<Agent>` and `<Service>` elements |
| Swapped every `:` for `=` so ConvertFrom-StringData would take it, which broke any value with a colon in it | Doesn't touch the text at all |
| Split on 7 fixed columns, so when Nickname got added it just went missing | Every field is a typed property |
| Ignored the exit code, so a permissions failure looked the same as "no services" | A failing exit code throws, with the exit code attached |

The tool will already give you XML if you ask with `-format:xml`, and it can export the schemas too. So nothing here parses console text. The XML also has the service type id, which the text output doesn't show, and a schema version. If a Dimension upgrade changes the format you find out straight away instead of getting wrong answers.

## Layout

```
Dimension.ServiceControl.sln
schemas/                                SimCorp's XSDs, exported from the tool
src/
  Dimension.ServiceControl/             the library       netstandard2.0
  Dimension.ServiceControl.PowerShell/  the cmdlets       netstandard2.0
tests/
  Dimension.ServiceControl.Tests/       xunit             net8.0
```

## Build and test

```powershell
dotnet build Dimension.ServiceControl.sln
dotnet test  Dimension.ServiceControl.sln
```

You don't need Dimension installed to run the tests. The process code gets tested against `cmd.exe`, and the client gets tested against a fake `ScdSvcCtl.cmd` that hands back canned XML and whatever exit code the test wants. That covers the schema version check, the unexpected error payload, failing exit codes, the negative state codes and timeouts. Every sample XML document gets checked against SimCorp's XSDs first, so the test data can't drift away from the real format.

## PowerShell

```powershell
Import-Module .\src\Dimension.ServiceControl.PowerShell\bin\Debug\netstandard2.0\Dimension.ServiceControl.PowerShell.psd1

# Set it once per session. It also reads the DIMENSION_SCDSVCCTL_PATH environment
# variable, which is handy for scheduled tasks.
Set-DimensionServiceControlPath '\\dimension-share\DEVT1\bin\ScdSvcCtl.exe'

Test-DimensionServiceControl        # checks the tool can be reached and prints its version
```

### Cmdlets

| Cmdlet | What it does |
| --- | --- |
| `Get-DimensionService` | Lists service instances, each one flattened with its agent |
| `Get-DimensionAgent` | Lists the running agents and the load they report |
| `Get-DimensionQueueItem` | Lists start, stop and move actions that haven't finished yet |
| `Start-DimensionService` | Starts service instances |
| `Stop-DimensionService` | Stops service instances. It asks first, and `-WhatIf` works |
| `Remove-DimensionQueueItem` | Cancels a queued action |
| `Set-` / `Get-DimensionServiceControlPath` | Sets or shows the path for the session |
| `Test-DimensionServiceControl` | Checks the tool can be reached and shows its version |

### Examples

```powershell
# Anything that isn't healthy
Get-DimensionService | Where-Object { -not $_.IsOnline }

# Filters get passed to the tool, so it does the filtering, not PowerShell
Get-DimensionService -Online -AgentName APPSERVER01

# They're enums, so no string matching
Get-DimensionService | Where-Object ManagementMode -eq Automated

# Anything the controller hasn't heard from in a while
Get-DimensionService -Stale $true

# Look first, then stop exactly what you looked at
Get-DimensionService -ServiceType 'Calculation service' | Stop-DimensionService -WhatIf
Get-DimensionService -ServiceType 'Calculation service' | Stop-DimensionService -Wait

Start-DimensionService -ServiceType 'Calculation service' -Count 2 -Wait
```

### The old function, rewritten

```powershell
# Before: about 40 lines of parsing, with the answer left in a global
get-dimension-info
$global:TrueAgents[0].State

# After
Get-DimensionService
```

## From C#

```csharp
var client = new ServiceControlClient(@"\\dimension-share\DEVT1\bin\ScdSvcCtl.exe");

ServiceListResult result = client.ListServices(new ServiceListFilter
{
    AgentName = "APPSERVER01",
    OnlineOnly = true
});

foreach (ServiceInstance service in result.Services)
{
    Console.WriteLine($"{service.FullName} {service.ServiceState} {service.Type}");
}

// Worth checking. A warning usually means an agent couldn't be reached,
// so the list is short rather than wrong.
foreach (string warning in result.Warnings)
{
    Console.WriteLine("warning: " + warning);
}
```

## Two ids, and they're not the same thing

Every service comes back with two ids.

* `FullName` (`agent\instance`, like `APPSERVER01\215-1`) is what `service stop` and `service explain` want.
* `Id` is the service *type* id. Every instance of a type has the same one, and the instance name usually has it in there too. If you pass it to `-service` the tool rejects it.

## When things go wrong

Failures throw typed exceptions with the exit code and the exact command line, so you can paste it into a console and run it yourself.

| Exception | When |
| --- | --- |
| `CommandFailedException` | The tool returned a failing exit code |
| `InvalidRequestException` | The request doesn't make sense, caught before anything runs |
| `CommandTimeoutException` | The tool didn't finish in time |
| `SchemaVersionMismatchException` | The XML says it's a different schema version to the one this was built for |
| `ResponseParseException` | The output couldn't be read as XML |
| `UnexpectedErrorException` | The tool returned an error in place of the XML |

Only *positive* exit codes count as failures. Negative ones are the tool telling you about state, like `MaintenanceMode` or `PlatformSuspended`, so they come back to you instead of throwing. Asking a question and getting an answer shouldn't be treated as an error.

## Schema versions

The library pins the schema versions it was written against and rejects anything else, unless you turn off `ValidateSchemaVersions` (`-SkipSchemaVersionCheck` on the cmdlets).

| Document | Version |
| --- | --- |
| `ServiceList` | 2307.0 |
| `AgentList` | 641.0 |
| `QueueitemList` | 2201.0 |

The XSDs the tool exports are in [`schemas/`](schemas/) for reference. After a Dimension upgrade, export them again over that folder and diff them before changing any of the models. The tests will already be pointing at whichever version moved.

```powershell
& '\\dimension-share\DEVT1\bin\ScdSvcCtl.exe' exportschemas -dest:'.\schemas' -create
```

## Commands that aren't wrapped yet

The tool does a lot more than this covers: maintenance mode, suspending and resuming the platform, dependency models, blacklists, blocking service types and so on. `ServiceControlClient.RunCommand` gets you to any of them and still goes through the same process handling, timeout and exit code checks.

```csharp
var arguments = new CommandArgumentBuilder("maintenance", "getmode");
CommandResult result = client.RunCommand(arguments, throwOnFailure: false);

if (result.ExitCode == ServiceControlExitCode.MaintenanceMode)
{
    Console.WriteLine("The platform is in maintenance mode.");
}
```

The commands that produce XML (`agent explain`, `service explain`, `model list`, `blacklist list`, `servicetype list` and the rest) can be turned into typed methods by adding a response model next to the existing ones.

## Licence

GPL-3.0 for the code in `src/` and `tests/`. See [LICENSE](LICENSE).

The XSDs in `schemas/` are SimCorp's, exported from their tool, and aren't covered by that licence.
