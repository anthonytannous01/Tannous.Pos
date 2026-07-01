using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Tannous.Pos.Application.Customers.Commands.AttachCustomerToOrder;
using Tannous.Pos.Application.Customers.Commands.CreateCustomer;
using Tannous.Pos.Application.Customers.Commands.UpdateCustomer;
using Tannous.Pos.Application.Customers.Queries.GetCustomer;
using Tannous.Pos.Application.Customers.Queries.GetCustomers;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Customers;
using Tannous.Pos.WebApi.Authentication;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

// NOTE: no controller-level [Authorize] here on purpose. GetCustomers/GetCustomer (reads) accept
// either a staff JWT or a read-only API key; every mutating action keeps requiring
// CanManageCustomers alone. ASP.NET Core combines class- and method-level [Authorize] attributes
// with AND, so the looser read policy has to live only on the read actions, not the class.
[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + ApiKeyAuthenticationHandler.SchemeName,
        Policy = PolicyConstants.CanViewCustomersOrApiKey)]
    public async Task<ActionResult<PaginatedResponseDto<CustomerDto>>> GetCustomers(
        [FromQuery] PaginatedRequestDto request)
    {
        var result = await _mediator.Send(new GetCustomersQuery { Request = request });
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyConstants.CanManageCustomers)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(
        [FromBody] CreateCustomerCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCustomer), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + ApiKeyAuthenticationHandler.SchemeName,
        Policy = PolicyConstants.CanViewCustomersOrApiKey)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyConstants.CanManageCustomers)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(
        Guid id, [FromBody] UpdateCustomerCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);

        if (result.Updated == null && !result.IsConflict)
            return NotFound();

        if (result.IsConflict)
            return Conflict(new { conflict = true, serverEntity = result.ServerState });

        return Ok(result.Updated);
    }

    [HttpPut("orders/{orderId}/customer")]
    [Authorize(Policy = PolicyConstants.CanManageCustomers)]
    public async Task<ActionResult> AttachCustomerToOrder(
        Guid orderId, [FromBody] AttachCustomerToOrderCommand command)
    {
        command.OrderId = orderId;
        var result = await _mediator.Send(command);

        if (!result.OrderFound)    return NotFound("Order not found");
        if (!result.CustomerFound) return NotFound("Customer not found");

        return NoContent();
    }
}
