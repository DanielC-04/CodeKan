namespace DevBoard.Domain.Tests;

public class SmokeTests
{
    [Fact]
    public void Domain_assembly_is_loadable()
    {
        var type = typeof(DevBoard.Domain.AssemblyReference);

        Assert.NotNull(type);
    }
}
