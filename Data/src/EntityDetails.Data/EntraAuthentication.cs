using Azure.Core;
using Azure.Identity;

namespace EntityDetails.Data;

/// <summary>
/// Microsoft Entra ID token authentication for Azure Database for PostgreSQL, used when
/// <see cref="EntityDetailsDataOptions.UseEntraAuthentication"/> is on.
/// </summary>
internal static class EntraAuthentication
{
    /// <summary>
    /// The token scope Azure Database for PostgreSQL accepts access tokens for.
    /// </summary>
    public const string Scope = "https://ossrdbms-aad.database.windows.net/.default";

    /// <summary>
    /// Chooses the credential that supplies access tokens.
    /// </summary>
    /// <param name="options">The data layer options.</param>
    /// <returns>
    /// <see cref="EntityDetailsDataOptions.TokenCredential"/> when set; otherwise a
    /// <see cref="ManagedIdentityCredential"/> for
    /// <see cref="EntityDetailsDataOptions.ManagedIdentityClientId"/> when set; otherwise a
    /// <see cref="DefaultAzureCredential"/>.
    /// </returns>
    /// <remarks>
    /// Hosts on Azure set the managed identity's client ID, so they never go through
    /// <see cref="DefaultAzureCredential"/>'s chain of credential sources. That chain is only the
    /// fallback for a developer connecting to an Azure database after <c>az login</c>.
    /// </remarks>
    public static TokenCredential CreateCredential(EntityDetailsDataOptions options)
    {
        if (options.TokenCredential is not null)
        {
            return options.TokenCredential;
        }

        return string.IsNullOrWhiteSpace(options.ManagedIdentityClientId)
            ? new DefaultAzureCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(options.ManagedIdentityClientId));
    }

    /// <summary>
    /// Requests an access token for Azure Database for PostgreSQL, used as the connection password.
    /// </summary>
    /// <param name="credential">The credential to request the token from.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>The access token.</returns>
    public static async ValueTask<string> GetAccessTokenAsync(TokenCredential credential, CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext([Scope]), cancellationToken);
        return token.Token;
    }
}
