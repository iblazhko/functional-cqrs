# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

All automation uses the PowerShell build script:

```bash
./build.ps1                              # Full build (clean, restore, build, test, publish)
./build.ps1 -Target Dotnet.Build         # Build only
./build.ps1 -Target Dotnet.Test          # Run all tests
./build.ps1 -Target Dotnet.Restore       # Restore NuGet packages

./build.ps1 -Target DockerCompose.Start          # Start full stack
./build.ps1 -Target DockerCompose.StartDetached  # Start full stack (background)
./build.ps1 -Target DockerCompose.Stop           # Stop stack
./build.ps1 -Target Docker.Build                 # Build Docker images (requires Dotnet.Publish first)

./build.ps1 -Target Prune               # Full cleanup (artifacts + Docker)
```

You can also run `dotnet` commands directly from within `src/`:

```bash
dotnet build src/CQRS.slnx
dotnet test src/CQRS.slnx
dotnet test src/CQRS.Domain.Tests/CQRS.Domain.Tests.csproj  # Single test project
```

## Architecture

This is a C# implementation of **CQRS + Event Sourcing + DDD** using functional programming principles (LanguageExt). It's structured in four tiers enforced by architecture tests (ArchUnitNET).

### Layer Structure

```
/0_Core/          — Domain layer (pure functions only, no side effects)
/1_Server/
  /1.1_Application/  — Command handling, business logic
  /1.2_Projections/  — Event-driven read model (view models)
  /1.3_API/          — REST endpoints (ASP.NET Minimal APIs)
  /1.4_Ports/        — Abstraction interfaces (EventStore, MessageBus, ProjectionStore)
  /1.5_Adapters/     — Implementations (MartenDB, MassTransit, InMemory)
  /1.6_Hosts/        — DI wiring and infrastructure
/2_Client/        — CLI (WIP)
/3_SystemTests/   — Architecture, Docker integration, E2E tests
```

### Dependency Rules (enforced by `CQRS.Architecture.Tests`)

- **Core** must not depend on Server or Adapters
- **Application** must not depend on Adapters or API
- **Projections** must not depend on Adapters, Application, or API
- **API** must not depend on Adapters
- **Ports** must not depend on Core, Application, or Projections
- Each **Adapter** implements exactly one Port

### Request Flow

1. HTTP request → **API.Host** (port 17322)
2. Command DTO → **MessageBus** (RabbitMQ via MassTransit)
3. **Application.Host** (port 17321) consumes command → runs domain logic → emits events
4. Events persisted to **MartenDB** (PostgreSQL) event store
5. Events published to bus → **Projections.Host** updates view models in MartenDB
6. API queries **ProjectionStore** for read responses

### Domain Pattern

Aggregates handle commands using pure static functions: `(State, Command) → Either<IError, Seq<IEvent>>`

```csharp
// src/CQRS.Domain/Inventory/InventoryAggregate.cs — example shape
public static Either<IError, Seq<IEvent>> Handle(InventoryState state, CreateInventory cmd) { ... }
```

Events produced by the aggregate are stored in the corresponding event stream.

State is reconstructed from events via `InventoryStateProjection`. No mutable state in domain.

### Key Technologies

- **.NET 10 / C# 13**, `global.json` pins SDK version
- **LanguageExt 5.x** — `Either<L,R>`, `Option<T>`, `Seq<T>`, discriminated unions
- **MartenDB 9.x** — PostgreSQL-backed event store and document projection store
- **Wolverine 6.x** + **RabbitMQ** — message bus for inter-process commands/events
- **ASP.NET Core Minimal APIs** — no controllers
- **xUnit v3** + **Shouldly** + **Bogus**  — test stack
  - Architectural dependencies rules verified with **ArchUnitNET**
  - System level end-to-end tests implemented with **TestContainers**
- **Serilog** — structured logging

### NuGet Package Management

Versions are centrally managed in `src/Directory.Packages.props`. Do not specify versions in individual `.csproj` files.

### Infrastructure (Docker)

- **PostgreSQL 18.x** on port 5432
- **RabbitMQ 4.2** on port 5672 (management UI on 15672)
- Default credentials in `.env` (dev only — `POSTGRES_PASSWORD=changeit`, `RABBITMQ_PASSWORD=changeit`)

Use `docker-compose.yaml` for the full stack, or split compose files for app vs infrastructure separately.

### Error Handling Convention

Domain and application code uses `Either<IError, T>` rather than exceptions. Domain error types are in `CQRS.Domain.Failures`. Do not introduce exception-based control flow in Core or Application layers.
