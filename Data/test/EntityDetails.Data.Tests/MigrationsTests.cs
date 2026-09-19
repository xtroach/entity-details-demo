using Microsoft.EntityFrameworkCore;

namespace EntityDetails.Data.Tests;

/// <summary>
/// Tests that the EF Core migrations stay in step with the model.
/// </summary>
public class MigrationsTests
{
    [Fact]
    public void Model_HasNoPendingChanges()
    {
        // Compares the model with the migrations' snapshot; no database connection is opened.
        using var dbContext = new AppDbContextDesignTimeFactory().CreateDbContext([]);

        Assert.False(
            dbContext.Database.HasPendingModelChanges(),
            "The model has changes that no migration covers. Add one with: "
                + "dotnet ef migrations add <Name> --project Data/src/EntityDetails.Data --output-dir Migrations");
    }
}
