namespace Elephentity.GraphQL.Manifest;

/// <summary>
/// One field on a GraphQL object type.
/// </summary>
/// <remarks>
/// Ported from <c>Eleph\GraphQL\Manifest\FieldEntry</c>. PHP names an accessor method and calls it by name; C# has no
/// reflection to spare, so the generated manifest carries the accessor itself, as a delegate, and the two ends still cannot drift
/// because the compiler checks the delegate against the read model.
/// </remarks>
/// <param name="Name">The GraphQL field name.</param>
/// <param name="Type">The GraphQL type.</param>
/// <param name="Read">Reads the field's domain value from the read model.</param>
/// <param name="Description">The description, or <see langword="null" />.</param>
/// <param name="Encoding">How the value travels.</param>
/// <param name="ValueType">The declared type whose processor unwinds the value. <see cref="FieldEncoding.Processor" /> only.</param>
public sealed record FieldEntry(
    string Name,
    GraphQLType Type,
    Func<object, object?> Read,
    string? Description = null,
    FieldEncoding Encoding = FieldEncoding.Value,
    string? ValueType = null);
