using MediatR;

namespace Tannous.Pos.Application.Integrations.ApiKeys.Commands.RevokeApiKey;

public class RevokeApiKeyCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
