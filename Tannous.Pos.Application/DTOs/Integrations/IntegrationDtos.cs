namespace Tannous.Pos.Application.DTOs.Integrations;

public class WebhookSubscriptionDto
{
    public Guid     Id                      { get; set; }
    public string   Name                    { get; set; } = string.Empty;
    public string   EndpointUrl             { get; set; } = string.Empty;
    public bool     IsActive                { get; set; }
    public Guid?    BranchId                { get; set; }
    public List<string> Events              { get; set; } = new();
    public DateTime CreatedAt               { get; set; }
    public DateTime? LastDeliveryAt         { get; set; }
    public bool?    LastDeliverySucceeded   { get; set; }
}

public class CreateWebhookResponse : WebhookSubscriptionDto
{
    /// <summary>Shown ONCE at creation. Cannot be retrieved again.</summary>
    public string Secret { get; set; } = string.Empty;
}

public class WebhookDeliveryLogDto
{
    public Guid     Id           { get; set; }
    public string   EventId      { get; set; } = string.Empty;
    public string   EventType    { get; set; } = string.Empty;
    public int?     ResponseCode { get; set; }
    public bool     IsSuccess    { get; set; }
    public string?  ErrorMessage { get; set; }
    public long     DurationMs   { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class ApiKeyDto
{
    public Guid      Id         { get; set; }
    public string    Name       { get; set; } = string.Empty;
    public string    KeyPrefix  { get; set; } = string.Empty;
    public bool      IsActive   { get; set; }
    public DateTime? ExpiresAt  { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime  CreatedAt  { get; set; }
}

public class CreateApiKeyResponse : ApiKeyDto
{
    /// <summary>The full raw API key. Shown ONCE. Store it securely.</summary>
    public string RawKey { get; set; } = string.Empty;
}

public class CreateWebhookSubscriptionDto
{
    public string Name        { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public Guid? BranchId     { get; set; }
}

public class UpdateWebhookSubscriptionDto
{
    public string Name        { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool IsActive      { get; set; } = true;
}

public class CreateApiKeyDto
{
    public string Name      { get; set; } = string.Empty;
    public Guid? BranchId   { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
