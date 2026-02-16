namespace DevBoard.IntegrationTests;

public class SmokeTests
{
    [Fact]
    public void Api_assembly_is_loadable()
    {
        var type = typeof(DevBoard.Api.AssemblyReference);

        Assert.NotNull(type);
    }
}
