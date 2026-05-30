using CQRS.Ports.EventStore;
using Shouldly;

namespace CQRS.Adapters.InMemoryEventStore.Tests;

public sealed class InMemoryEventStoreDBTests
{
    private readonly InMemoryEventStoreDB _db = new();

    [Fact]
    public void Open_NewStream_ReturnsEmptyStream()
    {
        var stream = _db.Open("test-stream");

        stream.Events.ShouldBeEmpty();
        stream.Version.ShouldBe(0);
    }

    [Fact]
    public void Open_ExistingStream_ReturnsPreviouslyAppendedEvents()
    {
        _db.AppendEvents("test-stream", [new SerializedEvent("SomeType", [1, 2, 3])], 0);

        var stream = _db.Open("test-stream");

        stream.Events.Length.ShouldBe(1);
        stream.Version.ShouldBe(1);
    }

    [Fact]
    public void Contains_ForUnknownStream_ReturnsFalse()
    {
        _db.Contains("unknown-stream").ShouldBeFalse();
    }

    [Fact]
    public void Contains_AfterAppendingEvents_ReturnsTrue()
    {
        _db.AppendEvents("my-stream", [new SerializedEvent("SomeType", [])], 0);

        _db.Contains("my-stream").ShouldBeTrue();
    }

    [Fact]
    public void AppendEvents_WithWrongExpectedVersion_ThrowsConcurrencyException()
    {
        _db.AppendEvents("stream", [new SerializedEvent("Type1", [])], 0);

        Should.Throw<ConcurrencyException>(() =>
            _db.AppendEvents("stream", [new SerializedEvent("Type2", [])], 0)
        );
    }

    [Fact]
    public void Delete_ExistingStream_ReturnsTrueAndStreamIsRemoved()
    {
        _db.AppendEvents("to-delete", [new SerializedEvent("T", [])], 0);

        var deleted = _db.Delete("to-delete");

        deleted.ShouldBeTrue();
        _db.Contains("to-delete").ShouldBeFalse();
    }

    [Fact]
    public void Delete_NonExistingStream_ReturnsFalse()
    {
        _db.Delete("non-existing").ShouldBeFalse();
    }

    [Fact]
    public void AppendEvents_MultipleEvents_IncrementsVersionByCount()
    {
        _db.AppendEvents(
            "stream",
            [
                new SerializedEvent("Type1", []),
                new SerializedEvent("Type2", []),
                new SerializedEvent("Type3", []),
            ],
            0
        );

        var stream = _db.Open("stream");

        stream.Events.Length.ShouldBe(3);
        stream.Version.ShouldBe(3);
    }

    [Fact]
    public void AppendEvents_SequentialAppendsWithCorrectVersion_Succeed()
    {
        _db.AppendEvents("stream", [new SerializedEvent("Type1", [])], 0);
        _db.AppendEvents("stream", [new SerializedEvent("Type2", [])], 1);

        var stream = _db.Open("stream");

        stream.Events.Length.ShouldBe(2);
        stream.Version.ShouldBe(2);
    }
}

public sealed class SerializedEventStreamTests
{
    [Fact]
    public void Empty_HasZeroVersionAndNoEvents()
    {
        var empty = SerializedEventStream.Empty;

        empty.Version.ShouldBe(0);
        empty.Events.ShouldBeEmpty();
    }
}
