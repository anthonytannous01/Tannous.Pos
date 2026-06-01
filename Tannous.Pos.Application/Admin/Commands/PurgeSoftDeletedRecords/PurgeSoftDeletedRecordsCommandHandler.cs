using MediatR;
using Tannous.Pos.Application.DTOs.Admin;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Admin.Commands.PurgeSoftDeletedRecords;

public class PurgeSoftDeletedRecordsCommandHandler
    : IRequestHandler<PurgeSoftDeletedRecordsCommand, PurgeSoftDeletedResultDto>
{
    private readonly IAdminPurgeRepository _adminPurgeRepository;
    private readonly IAuditService         _auditService;

    public PurgeSoftDeletedRecordsCommandHandler(
        IAdminPurgeRepository adminPurgeRepository,
        IAuditService auditService)
    {
        _adminPurgeRepository = adminPurgeRepository;
        _auditService         = auditService;
    }

    public async Task<PurgeSoftDeletedResultDto> Handle(
        PurgeSoftDeletedRecordsCommand request,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-request.Days);

        var customersToPurge  = await _adminPurgeRepository
            .GetSoftDeletedCustomersAsync(cutoffDate, cancellationToken);
        var menuItemsToPurge  = await _adminPurgeRepository
            .GetSoftDeletedMenuItemsAsync(cutoffDate, cancellationToken);
        var addOnsToPurge     = await _adminPurgeRepository
            .GetSoftDeletedAddOnsAsync(cutoffDate, cancellationToken);

        var totalPurged = customersToPurge.Count + menuItemsToPurge.Count + addOnsToPurge.Count;

        if (totalPurged > 0)
        {
            await _adminPurgeRepository.PurgeAsync(
                customersToPurge, menuItemsToPurge, addOnsToPurge, cancellationToken);

            await _auditService.LogEventAsync("PurgeSoftDeleted", "System", null, new
            {
                DaysOld         = request.Days,
                CustomersPurged = customersToPurge.Count,
                MenuItemsPurged = menuItemsToPurge.Count,
                AddOnsPurged    = addOnsToPurge.Count,
                TotalPurged     = totalPurged
            });
        }

        return new PurgeSoftDeletedResultDto
        {
            Message         = $"Purged {totalPurged} soft-deleted records older than {request.Days} days",
            CustomersPurged = customersToPurge.Count,
            MenuItemsPurged = menuItemsToPurge.Count,
            AddOnsPurged    = addOnsToPurge.Count,
            TotalPurged     = totalPurged,
            CutoffDate      = cutoffDate
        };
    }
}
