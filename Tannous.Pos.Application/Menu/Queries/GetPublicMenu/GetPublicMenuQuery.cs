using MediatR;
using Tannous.Pos.Application.DTOs.Menu;

namespace Tannous.Pos.Application.Menu.Queries.GetPublicMenu;

/// <summary>Returns the full active menu for unauthenticated public display (QR Digital Menu).</summary>
public class GetPublicMenuQuery : IRequest<PublicMenuDto> { }
