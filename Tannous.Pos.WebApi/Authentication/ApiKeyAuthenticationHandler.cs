using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.WebApi.Authentication;

/// <summary>
/// Authenticates requests carrying an <c>X-Api-Key</c> header against the <see cref="ApiKey"/> table.
/// Long-lived, read-only credential for third-party integrators — see <see cref="ApiKey"/> doc comment.
/// Never issues a role that satisfies write-oriented policies (CanManageCustomers, CanManageCatalog, etc.);
/// it only ever produces the "ApiIntegrator" role, which is exclusively consumed by the
/// *OrApiKey policies (see AuthorizationExtensions.AddPosAuthorizationPolicies).
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    /// <summary>Claim carrying the authenticated ApiKey's own Id, in case a handler needs to audit by key.</summary>
    public const string ApiKeyIdClaimType = "ApiKeyId";

    /// <summary>Claim carrying the key's BranchId, if the key is branch-scoped. NOTE: not yet enforced by
    /// any query handler — see AUTHORIZATION_POLICIES.md "Known limitations" for the follow-up step.</summary>
    public const string BranchIdClaimType = "BranchId";

    private readonly DbContext _dbContext;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        DbContext dbContext)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
            return AuthenticateResult.NoResult();

        var rawKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawKey))
            return AuthenticateResult.NoResult();

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();

        var apiKey = await _dbContext.Set<ApiKey>()
            .FirstOrDefaultAsync(k => k.KeyHash == hash);

        if (apiKey == null)
            return AuthenticateResult.Fail("Invalid API key.");

        if (!apiKey.IsActive)
            return AuthenticateResult.Fail("API key has been revoked.");

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return AuthenticateResult.Fail("API key has expired.");

        apiKey.LastUsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new(ClaimTypes.Name, apiKey.Name),
            new(ApiKeyIdClaimType, apiKey.Id.ToString()),
            // Deliberately NOT a role recognized by any *Manage*/write policy — read-only by construction.
            new(ClaimTypes.Role, "ApiIntegrator")
        };

        if (apiKey.BranchId.HasValue)
            claims.Add(new Claim(BranchIdClaimType, apiKey.BranchId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
