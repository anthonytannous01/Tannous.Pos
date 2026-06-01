using System.Security.Cryptography;
using System.Text;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Services;

public class ETagService : IETagService
{
    public string GenerateETag(string entityType, DateTime? maxUpdatedAt = null, int? rowCount = null, byte[]? version = null)
    {
        var content = new StringBuilder();
        content.Append(entityType);
        
        if (maxUpdatedAt.HasValue)
            content.Append(maxUpdatedAt.Value.ToString("O"));
        
        if (rowCount.HasValue)
            content.Append(rowCount.Value);
        
        if (version != null)
            content.Append(Convert.ToBase64String(version));

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()));
        return $"\"{Convert.ToBase64String(hash)}\"";
    }

    public bool IsETagValid(string etag, string ifNoneMatch)
    {
        if (string.IsNullOrEmpty(ifNoneMatch))
            return false;

        // Remove quotes if present
        var cleanEtag = etag.Trim('"');
        var cleanIfNoneMatch = ifNoneMatch.Trim('"');
        
        return cleanEtag.Equals(cleanIfNoneMatch, StringComparison.OrdinalIgnoreCase);
    }
}
