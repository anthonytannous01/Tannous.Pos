using System.Text.RegularExpressions;
using FluentValidation;

namespace Tannous.Pos.Application.Kds.Commands.CreateKdsStation;

public class CreateKdsStationCommandValidator : AbstractValidator<CreateKdsStationCommand>
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public CreateKdsStationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Color)
            .Must(c => c == null || HexColorRegex.IsMatch(c))
            .WithMessage("Color must be a hex value like #FF6B35");
    }
}
