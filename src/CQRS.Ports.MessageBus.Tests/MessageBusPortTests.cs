using CQRS.Ports.MessageBus;
using Shouldly;

namespace CQRS.Ports.MessageBus.Tests;

// --- MessagingId ---

public sealed class MessagingIdTests
{
    [Fact]
    public void NewId_ReturnsNonEmptyId()
    {
        MessagingId.NewId().Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_EachCallProducesUniqueId()
    {
        MessagingId.NewId().Id.ShouldNotBe(MessagingId.NewId().Id);
    }

    [Fact]
    public void Empty_WrapsGuidEmpty()
    {
        ((Guid)MessagingId.Empty).ShouldBe(Guid.Empty);
    }

    [Fact]
    public void ImplicitGuidConversion_ReturnsUnderlyingValue()
    {
        var g = Guid.NewGuid();
        var mid = (MessagingId)g;
        Guid result = mid;
        result.ShouldBe(g);
    }

    [Fact]
    public void ExplicitCast_FromGuid_WrapsValue()
    {
        var g = Guid.NewGuid();
        ((MessagingId)g).Id.ShouldBe(g);
    }

    [Fact]
    public void ExplicitCast_FromNullableGuid_WhenNull_ReturnsNull()
    {
        Guid? nullableGuid = null;
        ((MessagingId?)nullableGuid).ShouldBeNull();
    }

    [Fact]
    public void ExplicitCast_FromNullableGuid_WhenNonNull_ReturnsWrappedValue()
    {
        var g = Guid.NewGuid();
        Guid? nullableGuid = g;
        var result = (MessagingId?)nullableGuid;
        result.ShouldNotBeNull();
        result!.Value.Id.ShouldBe(g);
    }
}

// --- Context ---

public sealed class ContextTests
{
    [Fact]
    public void Empty_MessageId_IsEmpty()
    {
        ((Guid)Context.Empty.MessageId).ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Empty_CorrelationId_IsEmpty()
    {
        ((Guid)Context.Empty.CorrelationId).ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Empty_CausationId_IsNull()
    {
        Context.Empty.CausationId.ShouldBeNull();
    }

    [Fact]
    public void Empty_Timestamp_IsDefault()
    {
        Context.Empty.Timestamp.ShouldBe(default(DateTimeOffset));
    }

    [Fact]
    public void GetNew_MessageId_IsNonEmpty()
    {
        ((Guid)Context.GetNew(DateTimeOffset.UtcNow).MessageId).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void GetNew_CorrelationId_IsNonEmpty()
    {
        ((Guid)Context.GetNew(DateTimeOffset.UtcNow).CorrelationId).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void GetNew_CausationId_IsNull()
    {
        Context.GetNew(DateTimeOffset.UtcNow).CausationId.ShouldBeNull();
    }

    [Fact]
    public void GetNew_Timestamp_MatchesArgument()
    {
        var ts = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        Context.GetNew(ts).Timestamp.ShouldBe(ts);
    }

    [Fact]
    public void GetNew_EachCall_ProducesUniqueMessageId()
    {
        var ts = DateTimeOffset.UtcNow;
        Context.GetNew(ts).MessageId.ShouldNotBe(Context.GetNew(ts).MessageId);
    }

    [Fact]
    public void GetNew_EachCall_ProducesUniqueCorrelationId()
    {
        var ts = DateTimeOffset.UtcNow;
        Context.GetNew(ts).CorrelationId.ShouldNotBe(Context.GetNew(ts).CorrelationId);
    }
}

// --- MessageContextBuilderExtensions ---

public sealed class MessageContextBuilderExtensionsTests
{
    private static readonly DateTimeOffset FixedTimestamp = new(
        2026,
        4,
        25,
        10,
        0,
        0,
        TimeSpan.Zero
    );

    private static Context MakeContext() => Context.GetNew(DateTimeOffset.UtcNow);

    [Fact]
    public void GetResponseMetadata_PreservesCorrelationId()
    {
        var source = MakeContext();
        source.GetResponseMetadata(FixedTimestamp).CorrelationId.ShouldBe(source.CorrelationId);
    }

    [Fact]
    public void GetResponseMetadata_SetsCausationIdToSourceMessageId()
    {
        var source = MakeContext();
        source.GetResponseMetadata(FixedTimestamp).CausationId.ShouldBe(source.MessageId);
    }

    [Fact]
    public void GetResponseMetadata_GeneratesNewNonEmptyMessageId()
    {
        var source = MakeContext();
        ((Guid)source.GetResponseMetadata(FixedTimestamp).MessageId).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void GetResponseMetadata_MessageIdDiffersFromSource()
    {
        var source = MakeContext();
        source.GetResponseMetadata(FixedTimestamp).MessageId.ShouldNotBe(source.MessageId);
    }

    [Fact]
    public void GetResponseMetadata_UsesProvidedTimestamp()
    {
        MakeContext().GetResponseMetadata(FixedTimestamp).Timestamp.ShouldBe(FixedTimestamp);
    }
}
