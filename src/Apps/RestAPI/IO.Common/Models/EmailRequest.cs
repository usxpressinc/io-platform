namespace IO.Common.Models;

/// <summary>
/// Request model for sending an email
/// </summary>
public class EmailRequest
{
    /// <summary>
    /// Recipient email address
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Email subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML content of the email
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Plain text content of the email (optional)
    /// </summary>
    public string? TextContent { get; set; }
}
