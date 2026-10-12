# Where the C# runtime differs from the PHP one

`Elephentity.Runtime` is a port of `elephentity/packages/runtime`, type by type and test by
test. The generated code and the builders name these types as strings, so a type keeps its PHP
name unless a rule below says otherwise. This file lists **every** intended difference. Anything
not listed here is a bug in one of the two runtimes.

## The intended semantic difference

**Async-first.** Every operation that can reach storage returns a `Task<T>` and takes a
`CancellationToken`: the storage port (`IStorageAdaptor`), and later the gateway, the unit of
work and queries. The Cosmos SDK is asynchronous, and a synchronous port would block a thread
per request.

**Policies are async too, as `ValueTask`.** A policy can need to consult something ("the viewer
is a member of this ticket's project" is a lookup, and on Cosmos an asynchronous one), and the
application implements the policy interfaces that step 14 generates, so the signature has to
allow it from the start: `IEntityReadPolicies.DecideAsync`, `IEntityWritePolicies.DecideAsync`,
and on the gates `ReadGate.PermitAsync`/`RetainAsync` and `WriteGate.PermitAsync`. A policy that
only reads fields of the entity completes synchronously and allocates nothing. Pure value types
stay synchronous, and so does the viewer: `IViewer` is request data already in memory.

## Names

The mapping is mechanical so generated code can compute it.

| PHP | C# | Why |
|---|---|---|
| an interface, `StorageAdaptor` | `IStorageAdaptor` | .NET convention; the plan fixes the `I` prefix |
| a method that reaches storage or a policy, `get()` or `decide()` | `GetAsync()`, `DecideAsync()` | .NET convention for `Task`- and `ValueTask`-returning methods |
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

## Step 11: mutations, unit of work, gateway, queries

- **Async all the way down.** The unit of work, `DeletionPlanner`, `UniquenessCheck`, the gateway
  and the queries are `async` and take a `CancellationToken`. Side-effect handlers are
  `Func<CancellationToken, ValueTask>` and field verifiers are `IEntityVerifiers.VerifyAsync`,
  because both may consult storage. Read and write *processors* stay synchronous: they map one
  value and touch nothing.
- **No reflection.** PHP finds `ItemFinder::byBarcode()`, `ItemMutator::rename()` and `getName()`
  by `method_exists` and a string. Here the generated catalogue does the call itself:
  `RunQueryAsync(entity, query, args)`, `RunActionAsync(entity, mutation, call)` and
  `ValuesOf(entity, instance)`. `finder()` and `mutatorFor()` do not exist.
- **Edge mutation is `Add`, `Remove`, `Replace`.** `Set` is a reserved word in other .NET languages
  (CA1716), so `Mutation.Set` is `Assign` and `Edge(..).set` is `Replace`.
- **Moments are stored as UTC ISO 8601** (`o`), which sorts as the moments do, not as
  `Y-m-d H:i:s`. `ValueDecoder.DateTime` still reads the old form, as UTC.
- **A C# `enum` with no write processor is refused** by `ValueEncoder` with a message naming the
  field. How an enum is stored is the .NET builder's decision (step 14); `ValueDecoder.Enum<T>`
  takes the generated `FromWire`.
- **`ValueDecoder` is a static class** and names the rest `Text`, `Whole` and `Number`, because
  `String`, `Int` and `Float` trip CA1720.
- **Post-commit side-effect failures** are logged through a source-generated `[LoggerMessage]`
  and swallowed, as PHP's are, except `OperationCanceledException`, which propagates.
- **`DependencySorter` is static**: it holds no state.
- **Paging an entity with read policies resumes by keyset.** PHP re-reads every earlier row on
  every page (`CHUNK` = 100). `LazyEntityQuery` reads chunks *after* the `KeysetCursor` in the
  criteria's total order (sort keys, then id), keeps what the viewer may see, and stops once the
  page is full and one more visible row proves there is a next page. The cursor it returns names
  the last visible row, so hidden rows leave no trace in it and page *n* costs what it scans, not
  everything before it. A cursor that is not a keyset position (an offset, or garbage) restarts
  the list and is logged at Debug. `Count()` of a gated entity still hydrates and filters.
  An ungated entity hands the cursor to storage untouched.
- **`BootCheck`** resolves each catalogue contract from the `IServiceProvider` and reports every
  missing one in one `MissingImplementationsException`.
