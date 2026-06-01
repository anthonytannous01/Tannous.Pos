namespace Tannous.Pos.Domain.Interfaces;

public interface IReceiptNumberService
{
    Task<string> GenerateOrderNumberAsync();
    Task<string> GenerateReceiptNumberAsync();
    Task<string> GenerateShiftNumberAsync();
    Task<string> GeneratePurchaseOrderNumberAsync();
}
