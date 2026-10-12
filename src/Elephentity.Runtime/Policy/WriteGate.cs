using Elephentity.Runtime.Catalogue;

namespace Elephentity.Runtime.Policy;

/// <summary>
/// Where write policies are enforced: before a mutation is applied, it is permitted or refused.
/// </summary>
/// <remarks>
/// An entity with no write policies is never asked. The policies see the entity as it was before
/// the write, or <see langword="null" /> on create.
/// </remarks>
public sealed class WriteGate
{
    private readonly IEntityCatalogue catalogue;
    private readonly IViewerProvider viewers;

    /// <summary>A gate over <paramref name="catalogue" />, deciding for the viewer <paramref name="viewers" /> supplies.</summary>
    /// <param name="catalogue">Where the entities' policies come from.</param>
    /// <param name="viewers">Who is asking.</param>
    public WriteGate(IEntityCatalogue catalogue, IViewerProvider viewers)
    {
        this.catalogue = catalogue;
        this.viewers = viewers;
    }

    /// <summary>Lets the write through, or throws when the viewer may not make it.</summary>
    /// <param name="entity">The entity.</param>
    /// <param name="original">The entity as it was before the write; <see langword="null" /> on create.</param>
    /// <param name="context">What the write is.</param>
    /// <exception cref="AccessDeniedException">A policy refused.</exception>
    public void Permit(string entity, object? original, IWriteContext context)
    {
        var policies = catalogue.WritePolicies(entity);

        if (policies.IsEmpty())
        {
            return;
        }

        var decision = policies.Decide(original, context, viewers.Viewer());

        if (decision.Outcome != PolicyOutcome.Allow)
        {
            throw new AccessDeniedException(entity, decision.Policy, decision.Reason);
        }
    }
}
