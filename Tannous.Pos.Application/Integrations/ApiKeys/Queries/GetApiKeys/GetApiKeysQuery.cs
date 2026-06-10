using MediatR;
using Tannous.Pos.Application.DTOs.Integrations;

namespace Tannous.Pos.Application.Integrations.ApiKeys.Queries.GetApiKeys;

public class GetApiKeysQuery : IRequest<List<ApiKeyDto>>
{
}
