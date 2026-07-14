using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Queries.GetMenuItems;

public class GetMenuItemsQuery : IRequest<IEnumerable<MenuItemDto>>
{
    public Guid? CategoryId { get; set; }

    /// <summary>When true, also returns archived (IsActive=false) items. Used by the
    /// menu management "show archived" view; ordering/kiosk paths keep the default.</summary>
    public bool IncludeInactive { get; set; }
}
