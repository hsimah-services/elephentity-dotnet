using Elephentity.Runtime.Policy;

namespace Elephentity.Runtime.Catalogue;

/// <summary>
/// Everything the runtime needs to know about entities, addressed by name.
/// </summary>
/// <remarks>
/// Generated, because all of it is a walk over the spec. The runtime has no schema and should not
/// gain one: the IR is a build-time artefact and loading it per request would be paying for
/// compilation twice. Only the policy accessors are here so far, because the policy gates need
/// nothing else; the rest of the catalogue arrives with the unit of work.
/// </remarks>
public interface IEntityCatalogue
{
    /// <summary>The read policies of <paramref name="entity" />.</summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The policies; <see cref="NoPolicies" /> when it has none.</returns>
    IEntityReadPolicies ReadPolicies(string entity);

    /// <summary>The write policies of <paramref name="entity" />.</summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The policies; <see cref="NoPolicies" /> when it has none.</returns>
    IEntityWritePolicies WritePolicies(string entity);
}
