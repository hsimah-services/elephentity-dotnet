namespace Elephentity.Runtime.Policy;

/// <summary>An entity's read policies, evaluated in order: the first that is not a skip decides.</summary>
public interface IEntityReadPolicies
{
    /// <summary>Decides whether <paramref name="viewer" /> may read <paramref name="entity" />.</summary>
    /// <param name="entity">The hydrated entity.</param>
    /// <param name="viewer">Who is asking.</param>
    /// <returns>The decision; anything but an allow refuses.</returns>
    PolicyDecision Decide(object entity, IViewer viewer);

    /// <summary>Whether the entity has no read policies, so reads need no gate.</summary>
    /// <returns><see langword="true" /> when there are none.</returns>
    bool IsEmpty();
}
