using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Printing;
using Tannous.Pos.Application.Interfaces;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/v{version:apiVersion}/print")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class PrintingController : ControllerBase
{
    private readonly IPrintingService _printingService;

    public PrintingController(IPrintingService printingService)
    {
        _printingService = printingService;
    }

    [HttpGet("receipt-template")]
    public ActionResult<ReceiptTemplateDto> GetReceiptTemplate()
    {
        var template = _printingService.GetReceiptTemplate();
        return Ok(template);
    }

    [HttpGet("kitchen-template")]
    public ActionResult<KitchenTemplateDto> GetKitchenTemplate()
    {
        var template = _printingService.GetKitchenTemplate();
        return Ok(template);
    }

    [HttpPost("receipt/render")]
    public async Task<ActionResult<RenderResultDto>> RenderReceipt([FromBody] RenderReceiptRequest request)
    {
        try
        {
            var template = _printingService.GetReceiptTemplate();
            var lineWidth = request.LineWidth ?? template.LineWidth;
            
            var result = await _printingService.RenderReceiptAsync(request.OrderId, lineWidth);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("kitchen/render")]
    public async Task<ActionResult<RenderResultDto>> RenderKitchen([FromBody] RenderReceiptRequest request)
    {
        try
        {
            var template = _printingService.GetKitchenTemplate();
            var lineWidth = request.LineWidth ?? template.LineWidth;
            
            var result = await _printingService.RenderKitchenAsync(request.OrderId, lineWidth);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
