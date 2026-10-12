# Where the C# runtime differs from the PHP one

`Elephentity.Runtime` is a port of `elephentity/packages/runtime`, type by type and test by
test. The generated code and the builders name these types as strings, so a type keeps its PHP
name unless a rule below says otherwise. This file lists **every** intended difference. Anything
not listed here is a bug in one of the two runtimes.

## The intended semantic difference

**Async-first.** Every operation that can reach storage returns a `Task<T>` and takes a
`CancellationToken`: the storage port (`IStorageAdaptor`), and later the gateway, the unit of
work and queries. The Cosmos SDK is asynchronous, and a synchronous port would block a thread
per request. Pure value types and policy evaluation stay synchronous: a policy decides about an
entity that is already loaded.

## Names

The mapping is mechanical so generated code can compute it.

| PHP | C# | Why |
|---|---|---|
| an interface, `StorageAdaptor` | `IStorageAdaptor` | .NET convention; the plan fixes the `I` prefix |
| a method, `get()` that reaches storage | `GetAsync()` | .NET convention for `Task`-returning methods |
| an exception, `AccessDenied`, `UnsupportedCapability` | `AccessDeniedException`, `UnsupportedCapabilityException` | .NET convention; analyzers enforce it |
| `Filter::equals`, `Filter::notEquals`, `Comparison::Equals`, `Comparison::NotEquals` | `Filter.Equal`, `Filter.NotEqual`, `Comparison.Equal`, `Comparison.NotEqual` | a member called `Equals` collides with `object.Equals` |
| `Criteria->order` | `Criteria.Orders` | a property named `Order` would hide the `Order` type inside `Criteria` |
| `IDeletionRules::for()` | `IDeletionRules.RulesFor()` | `For` is a reserved word in Visual Basic (CA1716) |
| `Page::empty()` | `Page.Empty<T>()` | a static member on a generic type is an analyzer error (CA1000) |
| `Cursor` | `ICursor`, with `Cursor`, `OffsetCursor` and `KeysetCursor` | the cursor is opaque; one interface, a token a client sent back, and the two kinds a runtime issues |
| `Offset` | `OffsetCursor` | it is a cursor, and `Offset.fromCursor()` becomes `OffsetCursor.From()` |
| a string-backed PHP enum | a C# enum plus `ToWire()` and `FromWire()` in `<Enum>Extensions` | never rely on `ToString()` |
| a PHP closure parameter | a `Func<...>` | |
| `OutOfBoundsException` | `KeyNotFoundException` | the nearest .NET exception |
| `RuntimeException` from `NullProcessorRegistry` | `InvalidOperationException` | likewise |

A type that shares its name with a sibling namespace (`Capability`, `Verification`) is reached
as `Capability.Capability` or `Verification.Verification` from inside `Elephentity.Runtime.*`,
because C# looks for a namespace before it looks at a `using`. Application code outside
`Elephentity.Runtime` is not affected. In a test project, `Xunit.Record` hides
`Elephentity.Runtime.Storage.Record`; alias it.

## Types

- **`EntityId`** holds a `long` or a `string`, never a number in a string. `EntityId.Of(7)` and
  `EntityId.Of("7")` are different ids, as `7 === "7"` is false in PHP. It is a sealed record, so
  equality is by value and `default` cannot exist.
- **Processors are untyped at the registry**, as in PHP: `IReadProcessor.Read(object)` and
  `IWriteProcessor.Write(object)`. A generated processor casts to the types it was written for.
- **`Filter` values** are `object` checked at construction: a string, a number or a bool
  (`int`, `short` and the like widen to `long`, `float` to `double`), or a list of those for `In`
  and `NotIn`. PHP spells this `string|int|float|bool|array|null`.
- **`PendingId`** counts with `Interlocked.Increment`, so ids made on several threads stay unique.
- **`WriteResult`** keys by reference identity (`ReferenceEqualityComparer`), the equivalent of
  `SplObjectStorage`.

## Additions

- **`KeysetCursor`** has no PHP counterpart. It is the position an entity with read policies is
  paged by (step 11); see its documentation for the token format.
- **A whole-valued float in a keyset cursor reads back as an integer.** JSON writes `2.0` as `2`.
  The two compare equal, which is all a resumed sort needs.

## Pulled forward from step 11

Step 10's gates and processors name four types the plan files under step 11, so their minimum
is here and step 11 completes them: `IEntityCatalogue` (the two policy accessors only),
`IMutationContext` and `ActionCall`, and `Verification` and `Violation`.
