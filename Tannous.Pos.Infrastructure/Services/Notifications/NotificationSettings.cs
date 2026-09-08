namespace Tannous.Pos.Infrastructure.Services.Notifications;

public class NotificationSettings
{
    public const string Section = "Notifications";

    public bool Enabled { get; set; } = false;

    public TwilioSettings Twilio { get; set; } = new();
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken  { get; set; } = string.Empty;

    /// <summary>
    /// The WhatsApp sender in E.164 form, e.g. +14155238886 (Twilio's shared sandbox number) or
    /// your own approved WhatsApp Business sender. The "whatsapp:" prefix Twilio's API expects is
    /// added by the service; write the bare number here.
    /// </summary>
    public string FromNumber { get; set; } = string.Empty;
}
