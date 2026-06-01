namespace Tannous.Pos.Domain.Interfaces;

public interface IETagService
{
    string GenerateETag(string entityType, DateTime? maxUpdatedAt = null, int? rowCount = null, byte[]? version = null);
    bool IsETagValid(string etag, string ifNoneMatch);
}
