namespace Tannous.Pos.Infrastructure.Services.Notifications;

public class NotificationSettings
{
    public const string Section = "Notifications";

    public bool   Enabled  { get; set; } = false;
    /// <summary>Sms or WhatsApp</summary>
    public string Provider { get; set; } = "Sms";

    public TwilioSettings Twilio { get; set; } = new();
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken   { get; set; } = string.Empty;
    /// <summary>
    /// For SMS: a Twilio phone number (e.g. +12345678900).
    /// For WhatsApp: whatsapp:+14155238886 (Twilio sandbox or registered number).
    /// </summary>
    public string FromNumber  { get; set; } = string.Empty;
}
