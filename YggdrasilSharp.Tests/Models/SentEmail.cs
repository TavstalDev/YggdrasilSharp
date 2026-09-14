namespace Tavstal.YggdrasilSharp.Tests.Models;

/// <summary>
/// Represents an email that was "sent" by the fake or test email service.
/// Used by tests to inspect the recipient, subject, body and optional action link/button.
/// </summary>
public class SentEmail
{
    /// <summary>
    /// Gets the recipient email address.
    /// </summary>
    public string To { get; init; }

    /// <summary>
    /// Gets the username associated with the recipient (optional).
    /// This can be used by templates or assertions in tests.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the subject line of the email.
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Gets the HTML or plain-text body of the email.
    /// </summary>
    public string Body { get; init; }

    /// <summary>
    /// Gets the URL for an optional call-to-action contained in the email (for example: confirm, reset).
    /// May be <c>null</c> when no action link was provided.
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Gets the text for an optional action button associated with <see cref="ActionUrl"/>.
    /// May be <c>null</c> when no action button text was provided.
    /// </summary>
    public string? ButtonText { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SentEmail"/> class for a plain message
    /// without an action link or button.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="username">Optional username related to the recipient (may be <c>null</c>).</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body content (HTML or plain text).</param>
    public SentEmail(string to, string? username, string subject, string body)
    {
        To = to;
        Username = username;
        Subject = subject;
        Body = body;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SentEmail"/> class for a message
    /// that includes an action link and button text (for example: verification or password reset).
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="username">Optional username related to the recipient (may be <c>null</c>).</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body content (HTML or plain text).</param>
    /// <param name="actionUrl">The URL the user should be directed to when clicking the action button. May be <c>null</c>.</param>
    /// <param name="buttonText">The text to display on the action button. May be <c>null</c>.</param>
    public SentEmail(string to, string? username, string subject, string body, string? actionUrl, string? buttonText)
    {
        To = to;
        Username = username;
        Subject = subject;
        Body = body;
        ActionUrl = actionUrl;
        ButtonText = buttonText;
    }
}
