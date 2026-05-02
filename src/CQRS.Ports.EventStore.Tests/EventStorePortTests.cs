using CQRS.Ports.EventStore;
using Shouldly;

namespace CQRS.Ports.EventStore.Tests;

// --- EventStreamId ---

public sealed class EventStreamIdTests
{
    [Fact]
    public void Create_FromEmptyString_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => { var _ = (EventStreamId)""; });
    }

    [Fact]
    public void Create_FromWhitespace_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => { var _ = (EventStreamId)"   "; });
    }

    [Fact]
    public void Create_FromValidString_Succeeds()
    {
        Should.NotThrow(() => { var _ = (EventStreamId)"inventory-stream-1"; });
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsUnderlyingValue()
    {
        var id = (EventStreamId)"inventory-stream-1";
        ((string)id).ShouldBe("inventory-stream-1");
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        var id = (EventStreamId)"inventory-stream-1";
        id.ToString().ShouldBe("inventory-stream-1");
    }
}

// --- EventStreamVersion ---

public sealed class EventStreamVersionTests
{
    [Fact]
    public void InitialVersion_IsZero()
    {
        ((long)EventStreamVersion.InitialVersion).ShouldBe(0L);
    }

    [Fact]
    public void ExplicitCast_WrapsValue()
    {
        var version = (EventStreamVersion)42L;
        ((long)version).ShouldBe(42L);
    }

    [Fact]
    public void ImplicitLongConversion_ReturnsUnderlyingValue()
    {
        var version = (EventStreamVersion)7L;
        long value = version;
        value.ShouldBe(7L);
    }

    [Fact]
    public void ToString_ReturnsStringOfUnderlyingValue()
    {
        ((EventStreamVersion)99L).ToString().ShouldBe("99");
    }
}

// --- EventMetadata ---

public sealed class EventMetadataTests
{
    [Fact]
    public void NewEventMetadata_SetsEventTypeFullName()
    {
        var metadata = EventMetadata.NewEventMetadata("MyApp.Events.OrderCreated", DateTime.UtcNow);
        metadata.EventTypeFullName.ShouldBe("MyApp.Events.OrderCreated");
    }

    [Fact]
    public void NewEventMetadata_AssignsNonEmptyEventId()
    {
        var metadata = EventMetadata.NewEventMetadata("SomeEvent", DateTime.UtcNow);
        metadata.EventId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewEventMetadata_AssignsNonEmptyCorrelationId()
    {
        var metadata = EventMetadata.NewEventMetadata("SomeEvent", DateTime.UtcNow);
        metadata.CorrelationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewEventMetadata_SetsTimestamp()
    {
        var timestamp = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var metadata = EventMetadata.NewEventMetadata("SomeEvent", timestamp);
        metadata.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void NewEventMetadata_EachCallGeneratesUniqueEventId()
    {
        var m1 = EventMetadata.NewEventMetadata("SomeEvent", DateTime.UtcNow);
        var m2 = EventMetadata.NewEventMetadata("SomeEvent", DateTime.UtcNow);
        m1.EventId.ShouldNotBe(m2.EventId);
    }
}

// --- EventWithMetadata<T> ---

public sealed class EventWithMetadataTests
{
    private record TestEvent(string Name);

    [Fact]
    public void FromEvent_WrapsEventCorrectly()
    {
        var evt = new TestEvent("created");
        var wrapped = EventWithMetadata<TestEvent>.FromEvent(evt, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        wrapped.Event.ShouldBe(evt);
    }

    [Fact]
    public void FromEvent_SetsCorrelationIdInMetadata()
    {
        var corrId = Guid.NewGuid();
        var wrapped = EventWithMetadata<TestEvent>.FromEvent(new TestEvent("x"), corrId, Guid.NewGuid(), DateTime.UtcNow);
        wrapped.Metadata.CorrelationId.ShouldBe(corrId);
    }

    [Fact]
    public void FromEvent_SetsCausationIdInMetadata()
    {
        var causId = Guid.NewGuid();
        var wrapped = EventWithMetadata<TestEvent>.FromEvent(new TestEvent("x"), Guid.NewGuid(), causId, DateTime.UtcNow);
        wrapped.Metadata.CausationId.ShouldBe(causId);
    }

    [Fact]
    public void FromEvent_SetsTimestampInMetadata()
    {
        var timestamp = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var wrapped = EventWithMetadata<TestEvent>.FromEvent(new TestEvent("x"), Guid.NewGuid(), Guid.NewGuid(), timestamp);
        wrapped.Metadata.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void FromEvent_SetsEventTypeFullNameInMetadata()
    {
        var evt = new TestEvent("x");
        var wrapped = EventWithMetadata<TestEvent>.FromEvent(evt, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        wrapped.Metadata.EventTypeFullName.ShouldBe(typeof(TestEvent).FullName);
    }
}

// --- EventTypeResolver<T> ---

public sealed class EventTypeResolverTests
{
    // Defined at file scope so its full name has no '+' nested-type separator
    private readonly EventTypeResolver<ResolverAnchorType> _resolver = new();

    [Fact]
    public void GetEventType_KnownTypeName_ReturnsCorrectType()
    {
        var typeName = typeof(ResolverAnchorType).FullName!;
        var resolved = _resolver.GetEventType(typeName);
        resolved.ShouldBe(typeof(ResolverAnchorType));
    }

    [Fact]
    public void GetEventType_UnknownTypeName_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => _resolver.GetEventType("CQRS.Ports.EventStore.Tests.NonExistentType"));
    }

    [Fact]
    public void GetEventType_SameNameCalledTwice_ReturnsSameTypeInstance()
    {
        var typeName = typeof(ResolverAnchorType).FullName!;
        var t1 = _resolver.GetEventType(typeName);
        var t2 = _resolver.GetEventType(typeName);
        t1.ShouldBeSameAs(t2);
    }
}

// --- EventJsonSerializer ---

public sealed class EventJsonSerializerTests
{
    private record SerializablePayload(string Name, int Count);

    [Fact]
    public void Serialize_ThenDeserialize_RoundtripsData()
    {
        var original = new SerializablePayload("widget", 42);
        var bytes = EventJsonSerializer.Instance.Serialize(original);
        var restored = (SerializablePayload)EventJsonSerializer.Instance.Deserialize(bytes, typeof(SerializablePayload));
        restored.ShouldBe(original);
    }

    [Fact]
    public void Deserialize_NullJson_ThrowsInvalidOperationException()
    {
        var nullJsonBytes = "null"u8.ToArray();
        Should.Throw<InvalidOperationException>(
            () => EventJsonSerializer.Instance.Deserialize(nullJsonBytes, typeof(SerializablePayload))
        );
    }

    [Fact]
    public void Instance_ReturnsSameObjectOnRepeatedAccess()
    {
        var a = EventJsonSerializer.Instance;
        var b = EventJsonSerializer.Instance;
        a.ShouldBeSameAs(b);
    }
}

// --- NoOpEventPublisher<T> ---

public sealed class NoOpEventPublisherTests
{
    private record TestEvent(string Name);

    [Fact]
    public async Task Publish_DoesNotThrow()
    {
        var publisher = new NoOpEventPublisher<TestEvent>();
        var events = new[]
        {
            EventWithMetadata<TestEvent>.FromEvent(new TestEvent("e"), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow),
        };

        await Should.NotThrowAsync(() => publisher.Publish(events));
    }

    [Fact]
    public async Task Publish_EmptyList_DoesNotThrow()
    {
        var publisher = new NoOpEventPublisher<TestEvent>();
        await Should.NotThrowAsync(() => publisher.Publish([]));
    }
}

// Anchor type used by EventTypeResolverTests — at namespace scope so
// typeof(ResolverAnchorType).FullName has no '+' nested-type separator
internal record ResolverAnchorType(string Name);
