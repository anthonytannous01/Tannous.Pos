using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.GoodsReceipts.Commands.CreateGoodsReceipt;

public class CreateGoodsReceiptCommand : IRequest<GoodsReceiptDto>
{
    public CreateGoodsReceiptDto GoodsReceipt { get; set; } = new();
}
