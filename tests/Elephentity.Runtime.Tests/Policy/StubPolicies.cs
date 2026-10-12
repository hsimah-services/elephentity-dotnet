using Elephentity.Runtime.Catalogue;
using Elephentity.Runtime.Identity;
using Elephentity.Runtime.Mutation;
using Elephentity.Runtime.Policy;

namespace Elephentity.Runtime.Tests.Policy;

internal sealed class StubPolicies(Func<object, IViewer, PolicyDecision> handler) : IEntityReadPolicies
{
    public int Calls { get; private set; }

    public ValueTask<PolicyDecision> DecideAsync(object entity, IViewer viewer, CancellationToken cancellationToken = default)
    {
        Calls++;

        return ValueTask.FromResult(handler(entity, viewer));
    }

    public bool IsEmpty() => false;
}

internal sealed class StubWritePolicies(Func<object?, IWriteContext, IViewer, PolicyDecision> handler) : IEntityWritePolicies
{
    public int Calls { get; private set; }

    public ValueTask<PolicyDecision> DecideAsync(
        object? entity,
        IWriteContext context,
        IViewer viewer,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        return ValueTask.FromResult(handler(entity, context, viewer));
    }

    public bool IsEmpty() => false;
}

internal sealed class StubCatalogue : IEntityCatalogue
{
    private readonly Dictionary<string, IEntityReadPolicies> read = [];
    private readonly Dictionary<string, IEntityWritePolicies> write = [];

    public StubCatalogue Reading(string entity, IEntityReadPolicies policies)
    {
        read[entity] = policies;

        return this;
    }

    public StubCatalogue Writing(string entity, IEntityWritePolicies policies)
    {
        write[entity] = policies;

        return this;
    }

    public IEntityReadPolicies ReadPolicies(string entity) => read.GetValueOrDefault(entity) ?? new NoPolicies();

    public IEntityWritePolicies WritePolicies(string entity) => write.GetValueOrDefault(entity) ?? new NoPolicies();
}

/// <summary>A policy that has to wait for something, as one consulting a document store would.</summary>
internal sealed class SlowPolicies(Func<object, PolicyDecision> handler) : IEntityReadPolicies
{
    public async ValueTask<PolicyDecision> DecideAsync(object entity, IViewer viewer, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        return handler(entity);
    }

    public bool IsEmpty() => false;
}

/// <summary>A mutation context with nothing in it: enough for a policy to be handed one.</summary>
internal sealed class EmptyMutation(string entity, IIdentifier id, params ActionCall[] actions) : IMutationContext
{
    public string Entity => entity;

    public IReadOnlyList<ActionCall> Actions => actions;

    public object? OriginalEntity => null;

    public IIdentifier Id => id;

    public bool IsCreate => true;

    public object? Original(string field) => null;

    public object? Pending(string field) => null;

    public bool IsChanged(string field) => false;

    public IReadOnlyDictionary<string, object?> Changes() => new Dictionary<string, object?>();

    public IReadOnlyList<IIdentifier> PendingEdge(string edge) => [];

    public bool IsEdgeChanged(string edge) => false;
}
