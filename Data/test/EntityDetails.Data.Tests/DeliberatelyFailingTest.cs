namespace EntityDetails.Data.Tests;

/// <summary>
/// A test that always fails, used only in a throwaway PR to show that a failing test blocks merging
/// into main. This PR must never be merged.
/// </summary>
public class DeliberatelyFailingTest
{
    [Fact]
    public void AlwaysFails()
    {
        Assert.Equal(1, 2);
    }
}
