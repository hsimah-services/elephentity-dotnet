using System.Reflection;

namespace Elephentity.GraphQL.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    public void The_package_assembly_is_built_and_referenced()
    {
        Assert.Equal("Elephentity.GraphQL", Assembly.Load("Elephentity.GraphQL").GetName().Name);
    }
}
