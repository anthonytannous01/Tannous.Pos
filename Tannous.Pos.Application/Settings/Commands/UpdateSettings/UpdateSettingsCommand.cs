using MediatR;
using Tannous.Pos.Application.DTOs.Settings;

namespace Tannous.Pos.Application.Settings.Commands.UpdateSettings;

public class UpdateSettingsCommand : IRequest<SettingsDto>
{
    public UpdateSettingsDto Settings { get; set; } = null!;
}
