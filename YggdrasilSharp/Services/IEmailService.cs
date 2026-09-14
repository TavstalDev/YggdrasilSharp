namespace Tavstal.YggdrasilSharp.Services;

/// <summary>
/// Abstraction for sending emails from the application.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a simple email containing a subject and body to a recipient.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The HTML or plain-text body of the email.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous send operation.</returns>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an email that includes the recipient's username. Useful for templated messages
    /// where the display name or username should be injected into the message content.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="username">The username associated with the recipient.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The HTML or plain-text body of the email.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous send operation.</returns>
    Task SendEmailAsync(string to, string username, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an email containing an action link (for example: password reset, account verification)
    /// with text for a call-to-action button. Implementations should include the URL and button text
    /// in the rendered message (typically within the HTML body).
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="username">The username associated with the recipient.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The primary HTML or plain-text body of the email.</param>
    /// <param name="actionUrl">A URL that the recipient can click to perform an action (e.g. confirm, reset).</param>
    /// <param name="buttonText">Text to display for the action button (e.g. "Reset password").</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous send operation.</returns>
    Task SendEmailAsync(string to, string username, string subject, string body, string actionUrl, string buttonText, CancellationToken cancellationToken = default);
}
