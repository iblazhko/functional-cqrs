using CQRS.Ports.EventStore;
using LanguageExt;
using static LanguageExt.Prelude;
using Shouldly;

namespace CQRS.Ports.EventStore.Tests;

public sealed class EventSourcedRepositoryTests
{
    // --- Minimal test doubles ---

    private sealed record TestState(string Label)
    {
        public static TestState Empty => new("empty");
    }

    private sealed record TestEvent(string Name);
    private sealed record TestEventDto(string Name);

    private sealed class IdentityMapper : IEventMapper<TestEvent, TestEventDto>
    {
        public Either<EventDeserializationError, TestEvent> ToDomainEvent(TestEventDto dto) =>
            Right<EventDeserializationError, TestEvent>(new TestEvent(dto.Name));
        public TestEventDto ToEventDto(TestEvent domain) => new(domain.Name);
    }

    private sealed class ConstantProjection(TestState state) : IEventStreamProjection<TestState, TestEvent>
    {
        public TestState GetInitialState(EventStreamId streamId) => state;
        public TestState Apply(TestState s, TestEvent e) => s;
    }

    private sealed class SpySession : IEventStreamSession<TestState, TestEvent>
    {
        public TestState ReturnState { get; init; } = TestState.Empty;

        public List<TestEvent>? AppendedEvents { get; private set; }
        public Guid? AppendedCorrelationId { get; private set; }
        public Guid? AppendedCausationId { get; private set; }
        public bool SaveCalled { get; private set; }
        public bool Disposed { get; private set; }

        public Task<Either<EventDeserializationError, TestState>> GetState(
            IEventStreamProjection<TestState, TestEvent> projection,
            TimeSpan deadline = default,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Right<EventDeserializationError, TestState>(ReturnState));

        public void AppendEvents(IEnumerable<TestEvent> events, Guid? correlationId, Guid? causationId)
        {
            AppendedEvents = events.ToList();
            AppendedCorrelationId = correlationId;
            AppendedCausationId = causationId;
        }

        public Task Save(TimeSpan deadline = default, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }

        public Task<EventStream<TestEvent>> GetAllEvents(
            TimeSpan deadline = default,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new EventStream<TestEvent>((EventStreamId)"test", EventStreamVersion.InitialVersion, []));

        public EventStream<TestEvent> GetNewEvents() =>
            new((EventStreamId)"test", EventStreamVersion.InitialVersion, []);

        public void AppendEvents(IEnumerable<EventWithMetadata<TestEvent>> events) { }
        public void Dispose() { }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }

    private sealed class SpyStore(SpySession session) : IEventStore<TestState, TestEvent, TestEventDto>
    {
        public IEventStreamSession<TestState, TestEvent> Open(
            EventStreamId streamId,
            IEventMapper<TestEvent, TestEventDto> eventMapper,
            IEventSerializer? eventSerializer = null
        ) => session;

        public Task<bool> Contains(EventStreamId streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // --- Helpers ---

    private static EventSourcedRepository<TestState, TestEvent, TestEventDto> MakeRepository(SpySession session) =>
        new(new SpyStore(session));

    private static EventStreamId AnyStreamId() => (EventStreamId)"test-stream";

    // --- GetState ---

    [Fact]
    public async Task GetState_ReturnsStateProvidedBySession()
    {
        var expected = new TestState("loaded");
        var session = new SpySession { ReturnState = expected };

        var result = await MakeRepository(session).GetState(AnyStreamId(), new IdentityMapper(), new ConstantProjection(expected));

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => TestState.Empty, Right: s => s).ShouldBe(expected);
    }

    [Fact]
    public async Task GetState_DisposesSessionAfterUse()
    {
        var session = new SpySession();

        await MakeRepository(session).GetState(AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty));

        session.Disposed.ShouldBeTrue();
    }

    // --- AddEvents: events produced ---

    [Fact]
    public async Task AddEvents_WhenActionProducesEvents_AppendsThemToSession()
    {
        var session = new SpySession();
        var newEvents = new[] { new TestEvent("A"), new TestEvent("B") };

        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => newEvents
        );

        session.AppendedEvents.ShouldBe(newEvents);
    }

    [Fact]
    public async Task AddEvents_WhenActionProducesEvents_SavesSession()
    {
        var session = new SpySession();

        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => [new TestEvent("A")]
        );

        session.SaveCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task AddEvents_WhenActionProducesEvents_PassesCorrelationIdToSession()
    {
        var session = new SpySession();
        var corrId = Guid.NewGuid();

        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => [new TestEvent("A")],
            correlationId: corrId
        );

        session.AppendedCorrelationId.ShouldBe(corrId);
    }

    [Fact]
    public async Task AddEvents_WhenActionProducesEvents_PassesCausationIdToSession()
    {
        var session = new SpySession();
        var causId = Guid.NewGuid();

        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => [new TestEvent("A")],
            causationId: causId
        );

        session.AppendedCausationId.ShouldBe(causId);
    }

    [Fact]
    public async Task AddEvents_WhenActionProducesEvents_ReturnsThoseEvents()
    {
        var session = new SpySession();
        var newEvents = new[] { new TestEvent("X"), new TestEvent("Y") };

        var result = await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => newEvents
        );

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => [], Right: s => s).ShouldBe(newEvents);
    }

    [Fact]
    public async Task AddEvents_PassesStateFromSessionToAction()
    {
        var sessionState = new TestState("current");
        var session = new SpySession { ReturnState = sessionState };

        TestState? receivedState = null;
        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(sessionState),
            state => { receivedState = state; return []; }
        );

        receivedState.ShouldBe(sessionState);
    }

    // --- AddEvents: no events produced ---

    [Fact]
    public async Task AddEvents_WhenActionProducesNoEvents_DoesNotAppendToSession()
    {
        var session = new SpySession();

        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => []
        );

        session.AppendedEvents.ShouldBeNull();
    }

    [Fact]
    public async Task AddEvents_WhenActionProducesNoEvents_DoesNotSaveSession()
    {
        var session = new SpySession();

        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => []
        );

        session.SaveCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task AddEvents_WhenActionProducesNoEvents_ReturnsEmptyCollection()
    {
        var session = new SpySession();

        var result = await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => []
        );

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => [], Right: s => s).ShouldBeEmpty();
    }

    [Fact]
    public async Task AddEvents_DisposesSessionAfterUse()
    {
        var session = new SpySession();

        await MakeRepository(session).AddEvents(
            AnyStreamId(), new IdentityMapper(), new ConstantProjection(TestState.Empty),
            _ => []
        );

        session.Disposed.ShouldBeTrue();
    }
}
