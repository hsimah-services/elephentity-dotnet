using System.Reflection;

namespace Elephentity.Tsquid.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    public void The_package_assembly_is_built_and_referenced()
    {
        Assert.Equal("Elephentity.Tsquid", Assembly.Load("Elephentity.Tsquid").GetName().Name);
    }
}
