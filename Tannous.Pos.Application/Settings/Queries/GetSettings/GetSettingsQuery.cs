using MediatR;
using Tannous.Pos.Application.DTOs.Settings;

namespace Tannous.Pos.Application.Settings.Queries.GetSettings;

public class GetSettingsQuery : IRequest<SettingsDto>
{
}
