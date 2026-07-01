using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tannous.Pos.Application.Accounting.Commands.CompleteQuickBooksOAuth;
using Tannous.Pos.Application.Accounting.Commands.DisconnectAccounting;
using Tannous.Pos.Application.Accounting.Commands.TriggerAccountingSync;
using Tannous.Pos.Application.Accounting.Queries.GetAccountingStatus;
using Tannous.Pos.Application.DTOs.Accounting;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Infrastructure.Services.Accounting;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/accounting")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanManageSettings)]
public class AccountingController : ControllerBase
{
    private readonly IMediator           _mediator;
    private readonly AccountingSettings  _settings;

    public AccountingController(IMediator mediator, IOptions<AccountingSettings> settings)
    {
        _mediator  = mediator;
        _settings  = settings.Value;
    }

    [HttpGet("quickbooks/connect")]
    public ActionResult<object> ConnectQuickBooks([FromQuery] Guid? branchId = null)
    {
        var redirectUri = Uri.EscapeDataString(
            $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/accounting/quickbooks/callback");
        // Intuit rejects an empty `state` param as "missing", so fall back to a
        // non-empty sentinel that intentionally won't parse as a branch GUID.
        // The callback's Guid.TryParse(state, ...) already treats an unparseable
        // value as "no branch", so this preserves existing behavior for the
        // single-branch case without touching CompleteQuickBooksOAuthCommandHandler.
        var state = branchId?.ToString() ?? "none";
        var clientId = Uri.EscapeDataString(_settings.QuickBooks.ClientId);

        var url = "https://appcenter.intuit.com/connect/oauth2"
            + $"?client_id={clientId}"
            + "&scope=com.intuit.quickbooks.accounting"
            + "&response_type=code"
            + $"&redirect_uri={redirectUri}"
            + $"&state={Uri.EscapeDataString(state)}";

        return Ok(new { authorizationUrl = url });
    }

    [HttpGet("quickbooks/callback")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> QuickBooksCallback(
        [FromQuery] string code,
        [FromQuery] string? state,
        [FromQuery] string? realmId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { success = false, error = "Missing authorization code" });

        var success = await _mediator.Send(new CompleteQuickBooksOAuthCommand
        {
            Code    = code,
            State   = state,
            RealmId = realmId
        }, cancellationToken);

        if (!success)
            return BadRequest(new { success = false });

        return Ok(new { success = true });
    }

    [HttpGet("xero/connect")]
    public ActionResult<object> ConnectXero([FromQuery] Guid? branchId = null)
        => Ok(new { authorizationUrl = "", message = "Xero integration coming soon" });

    [HttpGet("status")]
    public async Task<ActionResult<List<AccountingConnectionStatusDto>>> GetStatus(
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAccountingStatusQuery { BranchId = branchId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SyncTriggerResultDto>> TriggerSync(
        [FromQuery] DateTime? date = null,
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new TriggerAccountingSyncCommand
        {
            Date     = date,
            BranchId = branchId
        }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{provider}")]
    public async Task<IActionResult> Disconnect(
        string provider,
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AccountingProvider>(provider, ignoreCase: true, out var parsed))
            return BadRequest(new { error = $"Unknown provider '{provider}'" });

        await _mediator.Send(new DisconnectAccountingCommand
        {
            Provider = parsed,
            BranchId = branchId
        }, cancellationToken);

        return NoContent();
    }
}
