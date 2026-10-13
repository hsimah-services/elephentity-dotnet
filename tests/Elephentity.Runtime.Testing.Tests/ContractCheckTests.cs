using Elephentity.Runtime.Conformance;
using Elephentity.Runtime.Testing;

namespace Elephentity.Runtime.Testing.Tests;

public sealed class ContractCheckTests
{
    private interface IItemVerifier;

    private interface IItemHandler;

    private sealed class Catalogue(params System.Type[] contracts) : StubEntityCatalogue
    {
        public override IReadOnlyList<System.Type> Contracts() => contracts;
    }

    private sealed class Services(params (System.Type Type, object Instance)[] bound) : IServiceProvider
    {
        public object? GetService(System.Type serviceType) =>
            bound.FirstOrDefault(entry => entry.Type == serviceType).Instance;
    }

    private sealed class Verifier(params Signal[] signals) : IVerifier
    {
        public IReadOnlyList<Signal> Verify(Func<string, string> classFor) => [.. signals, new Signal(Severity.Info, classFor("Item"))];
    }

    [Fact]
    public void AnUnboundContractFailsAndNamesTheInterface()
    {
        var check = new ContractCheck(
            new Catalogue(typeof(IItemVerifier), typeof(IItemHandler)),
            new Services((typeof(IItemHandler), new object())));

        var failure = Assert.Throws<ContractCheckException>(check.AssertConforms);

        Assert.Single(failure.Errors);
        Assert.Contains(typeof(IItemVerifier).FullName!, failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(typeof(IItemHandler).FullName!, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryUnboundContractIsReportedAtOnce()
    {
        var check = new ContractCheck(new Catalogue(typeof(IItemVerifier), typeof(IItemHandler)), new Services());

        var failure = Assert.Throws<ContractCheckException>(check.AssertConforms);

        Assert.Equal(2, failure.Errors.Count);
    }

    [Fact]
    public void BoundContractsPassAndSayHowManyResolved()
    {
        var check = new ContractCheck(
            new Catalogue(typeof(IItemVerifier)),
            new Services((typeof(IItemVerifier), new object())));

        check.AssertConforms();

        var signal = Assert.Single(check.Run());
        Assert.Equal(Severity.Info, signal.Severity);
        Assert.Equal("1 generated contract(s) resolve.", signal.Message);
    }

    [Fact]
    public void EveryRegisteredIntegrationVerifierRunsWithTheClassMapping()
    {
        var verifiers = new IVerifier[]
        {
            new Verifier(new Signal(Severity.Warning, "careful")),
            new Verifier(new Signal(Severity.Error, "GraphQL field Item.name does not resolve")),
        };
        var check = new ContractCheck(
            new Catalogue(),
            new Services((typeof(IEnumerable<IVerifier>), verifiers)),
            entity => "App." + entity);

        var signals = check.Run();

        Assert.Contains(signals, signal => signal is { Severity: Severity.Info, Message: "App.Item" });
        Assert.Contains(signals, signal => signal is { Severity: Severity.Warning, Message: "careful" });
        Assert.Throws<ContractCheckException>(check.AssertConforms);
    }

    [Fact]
    public void TheConsoleEntryPointPrintsJsonLinesAndExitsNonZeroOnAnError()
    {
        var output = new StringWriter();

        var ran = ContractCheck.TryRunConsole(
            ["--verbose", "eleph-check"],
            new Catalogue(typeof(IItemVerifier)),
            new Services(),
            output,
            out var exitCode);

        Assert.True(ran);
        Assert.Equal(1, exitCode);

        var lines = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(lines);
        Assert.StartsWith("{\"severity\":\"error\",\"message\":\"", lines[0], StringComparison.Ordinal);
        Assert.Contains("IItemVerifier", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void TheConsoleEntryPointExitsZeroWhenNothingIsWrong()
    {
        var output = new StringWriter();

        Assert.True(ContractCheck.TryRunConsole(["eleph-check"], new Catalogue(), new Services(), output, out var exitCode));
        Assert.Equal(0, exitCode);
        Assert.Contains("\"severity\":\"info\"", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheConsoleEntryPointStaysOutOfTheWayOfAnyOtherInvocation()
    {
        var output = new StringWriter();

        Assert.False(ContractCheck.TryRunConsole(["--list-tests"], new Catalogue(), new Services(), output, out _));
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void ASignalIsOneLineOfJsonWhateverItsMessageSays()
    {
        var line = ContractCheck.ToJsonLine(new Signal(Severity.Error, "has \"quotes\"\nand a newline"));

        Assert.DoesNotContain('\n', line);
        Assert.Equal("{\"severity\":\"error\",\"message\":\"has \\u0022quotes\\u0022\\nand a newline\"}", line);
    }
}
