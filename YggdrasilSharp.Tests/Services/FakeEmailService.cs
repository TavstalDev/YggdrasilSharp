using System.Collections.Concurrent;
using Tavstal.YggdrasilSharp.Services;
using Tavstal.YggdrasilSharp.Tests.Models;

namespace Tavstal.YggdrasilSharp.Tests.Services;

/// <summary>
/// A lightweight, test-friendly email service that records sent emails in-memory.
/// </summary>
public class FakeEmailService : IEmailService
{
    /// <summary>
    /// Internal, thread-safe storage of sent emails.
    /// </summary>
    private readonly ConcurrentBag<SentEmail> _sent = new();

    /// <summary>
    /// A snapshot of the emails that have been sent.
    /// </summary>
    public IReadOnlyCollection<SentEmail> SentEmails => _sent.ToArray();
    
    /// <summary>
    /// Records a simple email (recipient, subject and body).
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Email body (HTML or plain text).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A completed <see cref="Task"/>; no asynchronous I/O is performed.</returns>
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _sent.Add(new SentEmail(to, null, subject, body));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records an email that includes the recipient's username (useful for templated messages).
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="username">Optional username related to the recipient.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Email body (HTML or plain text).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A completed <see cref="Task"/>; no asynchronous I/O is performed.</returns>
    public Task SendEmailAsync(string to, string username, string subject, string body, CancellationToken cancellationToken = default)
    {
        _sent.Add(new SentEmail(to, username, subject, body));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records an email that contains an action link and button text (for example: password reset or confirmation).
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="username">Optional username related to the recipient.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Email body (HTML or plain text).</param>
    /// <param name="actionUrl">URL for a call-to-action included in the message (may be <c>null</c>).</param>
    /// <param name="buttonText">Text displayed for the action button (may be <c>null</c>).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A completed <see cref="Task"/>; no asynchronous I/O is performed.</returns>
    public Task SendEmailAsync(string to, string username, string subject, string body, string actionUrl, string buttonText, CancellationToken cancellationToken = default)
    {
        _sent.Add(new SentEmail(to, username, subject, body, actionUrl, buttonText));
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Clears recorded sent emails from the internal storage.
    /// </summary>
    public void Clear() => _sent.Clear();
}
