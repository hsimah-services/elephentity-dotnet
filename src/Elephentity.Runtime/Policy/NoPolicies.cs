namespace Elephentity.Runtime.Policy;

/// <summary>The policies of an entity that declares none: everything is allowed, and no gate is needed.</summary>
public sealed class NoPolicies : IEntityReadPolicies, IEntityWritePolicies
{
    /// <inheritdoc />
    public PolicyDecision Decide(object entity, IViewer viewer) => PolicyDecision.Allow();

    /// <inheritdoc />
    public PolicyDecision Decide(object? entity, IWriteContext context, IViewer viewer) => PolicyDecision.Allow();

    /// <inheritdoc />
    public bool IsEmpty() => true;
}
