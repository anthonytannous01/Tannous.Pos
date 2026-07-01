using MediatR;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Scheduling.Queries.ListScheduleStaff;

public class ListScheduleStaffQueryHandler
    : IRequestHandler<ListScheduleStaffQuery, List<StaffMemberDto>>
{
    private readonly IUserRepository _userRepository;

    public ListScheduleStaffQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<StaffMemberDto>> Handle(
        ListScheduleStaffQuery request,
        CancellationToken cancellationToken)
    {
        // Return up to 200 active staff members; staff lists are small in practice.
        var users = await _userRepository.SearchAsync(request.Search, skip: 0, take: 200);

        return users.Select(u => new StaffMemberDto
        {
            Id        = u.Id,
            Username  = u.Username,
            Email     = u.Email,
            FirstName = u.FirstName,
            LastName  = u.LastName,
            Role      = u.Role.ToString()
        }).ToList();
    }
}
