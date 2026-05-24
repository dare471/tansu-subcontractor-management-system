namespace Tansu.Worker.Options;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool UseStartTls { get; set; }
    public string From { get; set; } = "no-reply@tansu.local";
    public string FromName { get; set; } = "Tansu";
    public string? User { get; set; }
    public string? Password { get; set; }
}
