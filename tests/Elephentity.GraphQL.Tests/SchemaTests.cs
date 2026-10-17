using Elephentity.GraphQL.Manifest;
using Elephentity.GraphQL.Schema;

namespace Elephentity.GraphQL.Tests;

public sealed class SchemaTests
{
    [Fact]
    public void TheSdlIsByteForByteWhatThePhpPackagePrintsForTheSameModel()
    {
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "book-author.graphql")).ReplaceLineEndings("\n");
        var actual = ElephentitySchemaPrinter.Print(ElephentitySchema.Build(BookModel.Manifest()));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ACommittedSchemaThatMatchesChecksOutAndOneThatDriftedSaysWhere()
    {
        var schema = ElephentitySchema.Build(BookModel.Manifest());
        var sdl = ElephentitySchemaPrinter.Print(schema);

        Assert.Null(ElephentitySchemaPrinter.Check(schema, sdl));
        Assert.Null(ElephentitySchemaPrinter.Check(schema, sdl.ReplaceLineEndings("\r\n")));

        var drifted = ElephentitySchemaPrinter.Check(schema, sdl.Replace("title: String!", "title: String", StringComparison.Ordinal));

        Assert.NotNull(drifted);
        Assert.Contains("title: String!", drifted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmptyDescriptionsAreEmptyStringsInIntrospectionNotNull()
    {
        var host = new Host();
        var result = await host.Run("{ author: __type(name: \"Author\") { description } book: __type(name: \"Book\") { description } connection: __type(name: \"BookToAuthorConnection\") { description } }");

        Assert.Equal(string.Empty, result.GetProperty("data").GetProperty("author").GetProperty("description").GetString());
        Assert.Equal("A book.", result.GetProperty("data").GetProperty("book").GetProperty("description").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, result.GetProperty("data").GetProperty("connection").GetProperty("description").ValueKind);
    }

    [Fact]
    public void AQueryOnlyManifestHasNoMutationRoot()
    {
        var manifest = new GraphQLManifest(
            objects: new Dictionary<string, ObjectTypeEntry>
            {
                ["Note"] = new("Note", "Note", new Dictionary<string, FieldEntry> { ["body"] = new("body", new("String"), source => source) }, source => source is string, interfaces: []),
            });

        var schema = ElephentitySchema.Build(manifest);

        Assert.Null(schema.Mutation);
        Assert.StartsWith("schema {\n  query: RootQuery\n}", ElephentitySchemaPrinter.Print(schema), StringComparison.Ordinal);
    }

    [Fact]
    public void ATypeTheManifestDefinesTwiceIsReportedByName()
    {
        // Two connections that both come out as BookToAuthorConnection.
        var manifest = BookModel.Manifest();
        var book = manifest.Objects["Book"];
        var twice = new GraphQLManifest(
            objects: new Dictionary<string, ObjectTypeEntry>(manifest.Objects)
            {
                ["Book"] = new(
                    book.Name,
                    book.Entity,
                    book.Fields,
                    book.IsInstance,
                    new Dictionary<string, ConnectionEntry>(book.Connections)
                    {
                        ["writers"] = new("writers", "Book", "Author", "authors", source => ((BookModel.Book)source).Authors()),
                    },
                    book.Description),
            },
            enums: manifest.Enums,
            roots: manifest.Roots);

        var failure = Assert.Throws<InvalidOperationException>(() => ElephentitySchema.Build(twice));

        Assert.Contains("BookToAuthorConnection", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADescriptionThatCannotBeABlockStringIsQuotedAndAMultiLineOneIsABlock()
    {
        var manifest = new GraphQLManifest(
            objects: new Dictionary<string, ObjectTypeEntry>
            {
                ["Note"] = new("Note", "Note", new Dictionary<string, FieldEntry>
                {
                    ["body"] = new("body", new("String"), source => source, "First line.\nSecond \"line\"."),
                    ["tab"] = new("tab", new("String"), source => source, "has\ttab\u0001control"),
                }, source => source is string, null, "A note.", []),
            });

        var sdl = ElephentitySchemaPrinter.Print(ElephentitySchema.Build(manifest));

        Assert.Contains("  \"\"\"\n  First line.\n  Second \"line\".\n  \"\"\"\n  body: String", sdl, StringComparison.Ordinal);
        Assert.Contains("  \"has\\ttab\\u0001control\"\n  tab: String", sdl, StringComparison.Ordinal);
    }
}
