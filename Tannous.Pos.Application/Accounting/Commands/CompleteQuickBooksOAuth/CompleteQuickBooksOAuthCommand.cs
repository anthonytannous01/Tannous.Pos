using MediatR;

namespace Tannous.Pos.Application.Accounting.Commands.CompleteQuickBooksOAuth;

public class CompleteQuickBooksOAuthCommand : IRequest<bool>
{
    public string  Code    { get; set; } = string.Empty;
    public string? State   { get; set; }
    public string? RealmId { get; set; }
}
