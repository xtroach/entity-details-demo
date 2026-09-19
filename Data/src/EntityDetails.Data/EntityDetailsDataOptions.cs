using Azure.Core;

namespace EntityDetails.Data;

/// <summary>
/// Optional settings for <see cref="ServiceCollectionExtensions.AddEntityDetailsData"/>.
/// </summary>
/// <remarks>
/// Everything here is opt-in: with the defaults, the data layer connects with whatever the
/// connection string contains, which works on any host. Microsoft Entra ID authentication is for
/// Azure Database for PostgreSQL, where it removes the need for a database password.
/// </remarks>
public class EntityDetailsDataOptions
{
    /// <summary>
    /// Whether to authenticate to PostgreSQL with Microsoft Entra ID access tokens instead of a
    /// password. The connection string's <c>Username</c> must then be the Entra principal's name
    /// (for a managed identity, its resource name), and it must not contain a <c>Password</c>.
    /// </summary>
    public bool UseEntraAuthentication { get; set; }

    /// <summary>
    /// The client ID of the user-assigned managed identity to request tokens for, when
    /// <see cref="UseEntraAuthentication"/> is on. Hosts on Azure should always set it; without it
    /// (and without <see cref="TokenCredential"/>), tokens come from the developer's own Azure
    /// sign-in through <c>DefaultAzureCredential</c>.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// A credential that overrides the one chosen from <see cref="ManagedIdentityClientId"/>, for
    /// hosts with their own credential setup and for tests. Only used when
    /// <see cref="UseEntraAuthentication"/> is on.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }
}
