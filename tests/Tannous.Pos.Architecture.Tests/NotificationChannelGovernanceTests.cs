using Xunit;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Pins the notification decisions: WhatsApp is the only channel, there is exactly one path to
/// Twilio, and a message that does not go out says so.
///
/// The service used to carry an SMS/WhatsApp switch and four copies of the same send block, one
/// per message type. That is the shape every defect found by using this app has had: a rule
/// implemented in several places, with nothing comparing the copies.
/// </summary>
public class NotificationChannelGovernanceTests
{
    private static string RepoRoot() => ObservabilitySourceGovernanceTests.RepoRoot();

    private static string ServiceSource() => File.ReadAllText(Path.Combine(
        RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "Notifications", "TwilioNotificationService.cs"));

    private static string SettingsSource() => File.ReadAllText(Path.Combine(
        RepoRoot(), "Tannous.Pos.Infrastructure", "Services", "Notifications", "NotificationSettings.cs"));

    [Fact]
    public void Notifications_are_addressed_as_whatsapp()
    {
        Assert.Contains("whatsapp:", ServiceSource(), StringComparison.Ordinal);
    }

    [Fact]
    public void No_channel_switch_is_reintroduced()
    {
        Assert.DoesNotContain("Provider", SettingsSource(), StringComparison.Ordinal);
        Assert.DoesNotContain("_settings.Provider", ServiceSource(), StringComparison.Ordinal);
    }

    [Fact]
    public void There_is_exactly_one_path_to_the_twilio_api()
    {
        // Four copies of the request-building block is how one of them drifts. If a fifth message
        // type is added, it routes through SendAsync rather than pasting the block again.
        var occurrences = ServiceSource().Split("api.twilio.com").Length - 1;
        Assert.True(
            occurrences == 1,
            $"Expected a single Twilio request site in TwilioNotificationService, found {occurrences}. " +
            "New message types should build a body and call SendAsync.");
    }

    [Fact]
    public void A_confirmation_that_was_not_sent_is_logged()
    {
        // SendOrderConfirmationAsync never throws; it returns false. Without this warning, a
        // disabled channel and a delivered message look identical from the outside.
        var path = Path.Combine(
            RepoRoot(), "Tannous.Pos.Application", "Orders", "Commands", "FinalizeOrder",
            "FinalizeOrderCommandHandler.cs");
        var text = File.ReadAllText(path);

        Assert.Contains("var confirmationSent = await _notificationService.SendOrderConfirmationAsync", text, StringComparison.Ordinal);
        Assert.Contains("Notification observability:", text, StringComparison.Ordinal);
    }
}
