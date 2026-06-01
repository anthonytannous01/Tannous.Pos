using MediatR;

namespace Tannous.Pos.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}


