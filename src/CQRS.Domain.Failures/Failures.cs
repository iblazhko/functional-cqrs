using LanguageExt;

namespace CQRS.Domain.Failures;

// ReSharper disable NotAccessedPositionalProperty.Global

/*
 When it comes to terminology for representing issues with process execution, there are multiple
 (sometimes overlapping / conflicting) definitions. This example assumes the following definitions:

 * Failure: System was not able to perform what it was expected from it. This is the problem we observe.
 * Fault: The cause of the failure.
 * Error: The condition which caused the fault to occur. e.g, missing or incorrectly formatted properties.

 Saying "failure" means we know something is wrong, but we may not know the cause.
 Saying "fault" means we know the cause category, but may not know exactly why the fault occurred.
 Saying "error" means we know why the fault occurred.

 https://stackoverflow.com/a/47963772

 Note that in distributed systems the line between failure and fault can be blurry:
 a process failure may be caused by failures in external activities, and each of those failures
 can be considered a fault from the point of view of the top level process.
*/

public sealed record Failure(string Message, Seq<Fault> Faults);

public record Fault(string Message, Seq<Error> Errors);

public sealed record ValidationFault(string Message, Seq<Error> Errors) : Fault(Message, Errors);

public sealed record OperationFault(string Message, Seq<Error> Errors) : Fault(Message, Errors);

public sealed record Error(string Message);
