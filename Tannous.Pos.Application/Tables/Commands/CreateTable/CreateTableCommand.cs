using MediatR;
using Tannous.Pos.Application.DTOs.Tables;

namespace Tannous.Pos.Application.Tables.Commands.CreateTable;

public class CreateTableCommand : IRequest<TableDto>
{
    public CreateTableDto Table { get; set; } = null!;
}
