using Azure.Core;
using Azure.Identity;

namespace EntityDetails.Data.Tests;

/// <summary>
/// Tests for <see cref="EntraAuthentication"/>, the opt-in Microsoft Entra ID database login.
/// </summary>
public class EntraAuthenticationTests
{
    [Fact]
    public void CreateCredential_UsesExplicitCredential_WhenSet()
    {
        var explicitCredential = new FakeTokenCredential();
        var options = new EntityDetailsDataOptions
        {
            TokenCredential = explicitCredential,
            ManagedIdentityClientId = "00000000-0000-0000-0000-000000000001",
        };

        Assert.Same(explicitCredential, EntraAuthentication.CreateCredential(options));
    }

    [Fact]
    public void CreateCredential_UsesManagedIdentity_WhenClientIdIsSet()
    {
        var options = new EntityDetailsDataOptions { ManagedIdentityClientId = "00000000-0000-0000-0000-000000000001" };

        Assert.IsType<ManagedIdentityCredential>(EntraAuthentication.CreateCredential(options));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateCredential_FallsBackToDefaultCredential_WithoutClientId(string? clientId)
    {
        var options = new EntityDetailsDataOptions { ManagedIdentityClientId = clientId };

        Assert.IsType<DefaultAzureCredential>(EntraAuthentication.CreateCredential(options));
    }

    [Fact]
    public async Task GetAccessTokenAsync_RequestsPostgresScope_AndReturnsToken()
    {
        var credential = new FakeTokenCredential();

        var token = await EntraAuthentication.GetAccessTokenAsync(credential, CancellationToken.None);

        Assert.Equal(FakeTokenCredential.Token, token);
        Assert.Equal(["https://ossrdbms-aad.database.windows.net/.default"], credential.RequestedScopes);
    }

    /// <summary>
    /// A credential that hands out a fixed token and records the scopes it was asked for.
    /// </summary>
    internal sealed class FakeTokenCredential : TokenCredential
    {
        public const string Token = "fake-access-token";

        public string[] RequestedScopes { get; private set; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            RequestedScopes = requestContext.Scopes;
            return new AccessToken(Token, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }
}
