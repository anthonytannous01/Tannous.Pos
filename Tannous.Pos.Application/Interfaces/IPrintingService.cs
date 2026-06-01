using Tannous.Pos.Application.DTOs.Printing;

namespace Tannous.Pos.Application.Interfaces;

public interface IPrintingService
{
    ReceiptTemplateDto GetReceiptTemplate();
    KitchenTemplateDto GetKitchenTemplate();
    Task<RenderResultDto> RenderReceiptAsync(Guid orderId, int lineWidth);
    Task<RenderResultDto> RenderKitchenAsync(Guid orderId, int lineWidth);
}
