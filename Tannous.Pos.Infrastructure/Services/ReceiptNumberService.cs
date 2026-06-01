using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class ReceiptNumberService : IReceiptNumberService
{
    private readonly PosDbContext _context;

    public ReceiptNumberService(PosDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        return await GenerateSequenceNumberAsync("Order", "ORD");
    }

    public async Task<string> GenerateReceiptNumberAsync()
    {
        return await GenerateSequenceNumberAsync("Receipt", "RCP");
    }

    public async Task<string> GenerateShiftNumberAsync()
    {
        return await GenerateSequenceNumberAsync("Shift", "SHF");
    }

    public async Task<string> GeneratePurchaseOrderNumberAsync()
    {
        return await GenerateSequenceNumberAsync("PurchaseOrder", "PO");
    }

    private async Task<string> GenerateSequenceNumberAsync(string sequenceType, string prefix)
    {
        var sequence = await _context.ReceiptSequences
            .FirstOrDefaultAsync(s => s.SequenceType == sequenceType);

        if (sequence == null)
        {
            sequence = new ReceiptSequence
            {
                SequenceType = sequenceType,
                Prefix = prefix,
                CurrentNumber = 0,
                NextNumber = 1,
                IsActive = true,
                LastUsed = DateTime.UtcNow
            };
            _context.ReceiptSequences.Add(sequence);
        }

        var number = sequence.NextNumber;
        sequence.CurrentNumber = number;
        sequence.NextNumber = number + 1;
        sequence.LastUsed = DateTime.UtcNow;

        // Join an ambient transaction (e.g. order finalize) instead of flushing mid-flight.
        if (_context.Database.CurrentTransaction == null)
            await _context.SaveChangesAsync();

        return $"{sequence.Prefix}{number:D6}";
    }
}
