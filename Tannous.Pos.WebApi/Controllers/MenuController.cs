using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Tannous.Pos.Application.DTOs.Menu;
using Tannous.Pos.Application.Menu.Queries.GetPublicMenu;

namespace Tannous.Pos.WebApi.Controllers;

/// <summary>
/// Public (unauthenticated) digital menu endpoints — used by QR code scanning customers.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/menu")]
[ApiVersion("1.0")]
[AllowAnonymous]
public class MenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuController(IMediator mediator) => _mediator = mediator;

    /// <summary>Full active menu as JSON — for API/app consumers.</summary>
    [HttpGet("public")]
    public async Task<ActionResult<PublicMenuDto>> GetMenuJson(CancellationToken ct)
        => Ok(await _mediator.Send(new GetPublicMenuQuery(), ct));

    /// <summary>Full active menu as a mobile-friendly HTML page — served when a customer scans the QR code.</summary>
    [HttpGet("public/html")]
    [Produces("text/html")]
    public async Task<ContentResult> GetMenuHtml(CancellationToken ct)
    {
        var menu = await _mediator.Send(new GetPublicMenuQuery(), ct);
        var html = BuildHtml(menu);
        return Content(html, "text/html", Encoding.UTF8);
    }

    // ── HTML builder ────────────────────────────────────────────────────────
    private static string BuildHtml(PublicMenuDto menu)
    {
        var sb = new StringBuilder();
        var showLbp = menu.ExchangeRateLbpPerUsd > 0;

        sb.Append("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
              <title>Menu</title>
              <style>
                *{box-sizing:border-box;margin:0;padding:0}
                body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;
                     background:#f5f5f5;color:#1a1a1a;padding-bottom:32px}
                .header{background:#1F497D;color:#fff;padding:24px 16px 20px;text-align:center}
                .header h1{font-size:1.6rem;font-weight:700;margin-bottom:4px}
                .header p{font-size:.85rem;opacity:.85;margin-top:4px}
                .category{margin:16px 12px 0}
                .category-title{font-size:1.05rem;font-weight:700;color:#1F497D;
                                padding:10px 4px 6px;border-bottom:2px solid #1F497D;margin-bottom:8px}
                .item{background:#fff;border-radius:10px;padding:14px 16px;margin-bottom:8px;
                      display:flex;justify-content:space-between;align-items:flex-start;
                      box-shadow:0 1px 3px rgba(0,0,0,.08)}
                .item-name{font-weight:600;font-size:.95rem;flex:1;padding-right:12px}
                .item-desc{font-size:.78rem;color:#666;margin-top:3px;line-height:1.4}
                .item-price{font-weight:700;font-size:.95rem;color:#1F497D;white-space:nowrap}
                .item-lbp{font-size:.72rem;color:#888;text-align:right;margin-top:2px}
                .footer{text-align:center;margin-top:24px;font-size:.75rem;color:#aaa}
              </style>
            </head>
            <body>
            """);

        // Header
        sb.Append($"""
            <div class="header">
              <h1>{Encode(menu.BusinessName)}</h1>
            """);
        if (!string.IsNullOrEmpty(menu.Address))
            sb.Append($"<p>{Encode(menu.Address)}</p>");
        if (!string.IsNullOrEmpty(menu.Phone))
            sb.Append($"<p>{Encode(menu.Phone)}</p>");
        sb.Append("</div>");

        // Categories + items
        foreach (var cat in menu.Categories)
        {
            sb.Append($"""
                <div class="category">
                  <div class="category-title">{Encode(cat.Name)}</div>
                """);

            foreach (var item in cat.Items)
            {
                var lbpPrice = showLbp
                    ? $"{(item.Price * menu.ExchangeRateLbpPerUsd):N0} LBP"
                    : null;

                sb.Append($"""
                    <div class="item">
                      <div>
                        <div class="item-name">{Encode(item.Name)}</div>
                    """);
                if (!string.IsNullOrEmpty(item.Description))
                    sb.Append($"""<div class="item-desc">{Encode(item.Description)}</div>""");
                sb.Append("</div>");

                sb.Append($"""
                    <div>
                      <div class="item-price">{menu.Currency} {item.Price:N2}</div>
                    """);
                if (lbpPrice != null)
                    sb.Append($"""<div class="item-lbp">{lbpPrice}</div>""");
                sb.Append("</div></div>");
            }

            sb.Append("</div>"); // .category
        }

        sb.Append("""
            <div class="footer">Powered by Tannous POS</div>
            </body></html>
            """);

        return sb.ToString();
    }

    private static string Encode(string? text)
        => System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
}
