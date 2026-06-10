using MediatR;
using Tannous.Pos.Application.DTOs.Integrations;

namespace Tannous.Pos.Application.Integrations.ApiKeys.Commands.CreateApiKey;

public class CreateApiKeyCommand : IRequest<CreateApiKeyResponse>
{
    public CreateApiKeyDto ApiKey { get; set; } = new();
}
