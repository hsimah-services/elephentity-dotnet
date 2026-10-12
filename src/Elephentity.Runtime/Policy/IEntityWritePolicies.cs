namespace Elephentity.Runtime.Policy;

/// <summary>An entity's write policies, evaluated in order: the first that is not a skip decides.</summary>
public interface IEntityWritePolicies
{
    /// <summary>Decides whether <paramref name="viewer" /> may make the write described by <paramref name="context" />.</summary>
    /// <param name="entity">The entity as it was before the write; <see langword="null" /> on create.</param>
    /// <param name="context">What the write is.</param>
    /// <param name="viewer">Who is asking.</param>
    /// <returns>The decision; anything but an allow refuses.</returns>
    PolicyDecision Decide(object? entity, IWriteContext context, IViewer viewer);

    /// <summary>Whether the entity has no write policies, so writes need no gate.</summary>
    /// <returns><see langword="true" /> when there are none.</returns>
    bool IsEmpty();
}
