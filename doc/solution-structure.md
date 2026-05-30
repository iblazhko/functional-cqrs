# Solution Structure

Solution uses .NET 10 / C# and has following major parts:

-   _Core domain_ components. Only types and pure functions are allowed here.
    -   `CQRS.Domain`: Implements domain model and domain services.
    -   `CQRS.DTO`: Commands and Events [DTO](./dto.md)s that are shared across
        domain and application services projects, and stored in event streams.
        DTOs define shape of messages that go over the wire, they designed to be
        implementation-neutral and serialization-friendly. Essentially they are
        [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object)s that only use
        primitive CLR types.
    -   `CQRS.Mapping`: Implements mapping between DTOs and domain model.
-   _Application_ services that are aware of the outside world and intended
    to have effects - consume and publish messages, read and write DB data,
    make external HTTP/GRPC calls etc.
    -   `CQRS.Application`: implements application services; handles Commands sent
        from the API.
    -   `CQRS.Projections`: projects domain Events to view models.
        persisted in a document-based _ProjectionStore_.
    -   `CQRS.Projections.Repositories`: retrieve document by Id.
-   _API_ components
    -   `CQRS.API`: sends Commands to _Application_ (command API),
        uses _ProjectionStore_ to retrieve view models (query API).
-   _Ports_
    -   `CQRS.Ports.EventStore`: _EventStore_ abstraction for event sourcing /
        event store
    -   `CQRS.Ports.MessageBus`: message bus abstraction for sending commands and
        publishing events. Note that registration of messages consumers is not
        a part of this abstraction - it is a part of _Application_ host that rely
        on a specific _MessageBus_ adapter
    -   `CQRS.Ports.ProjectionStore`: abstraction for persisting event
        projections in denormalized form (document store). Normalized form
        (relational database) projections are not a part of this abstraction
        at the moment, but could be added if needed.
-   _Adapters_
    -   `CQRS.Adapters.InMemoryEventStore`: in-memory adapter for _EventStore_ port
    -   `CQRS.Adapters.InMemoryProjectionStore`: in-memory adapter for
        _ProjectionStore_ port
    -   `CQRS.Adapters.MartenDbEventStore`: [MartenDB](https://martendb.io/) adapter
        for _EventStore_ port
    -   `CQRS.Adapters.MartenDbProjectionStore`: [MartenDB](https://martendb.io/)
        adapter for _ProjectionStore_ port
    -   `CQRS.Adapters.WolverineMessageBus`:
        [Wolverine](https://wolverinefx.net/) /
        [RabbitMQ](https://www.rabbitmq.com/) adapter for _MessageBus_ port
-   Client (`CQRS.CLI`): example of an external client that interacts with
    the system via API
-   Benchmark tests (`CQRS.Benchmarks`). (WIP)

For educational purposes _API_ and _Application_ have separate hosts
(`CQRS.API.Host` and `CQRS.Application.Host` respectively). In a real life
projects these two hosts can be combined into one, or _Application_ host
can be split into smaller parts for better scalability (e.g. to separate
commands handling and maintaining projections).

As you have probably noticed, the solution has large number of projects, and
many projects contain only a few files (sometimes only one). Main reason
for using this approach is that it allows to track dependencies inside the
codebase easily and make design mistakes more visible; this also allows us
to avoid including “dead code” in an executable host project.
See [“Solution Dependencies Rules”](./dependencies.md) document
for more information.
