using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Tavstal.YggdrasilSharp.Models;

namespace Tavstal.YggdrasilSharp.Services;

/// <summary>
/// Service responsible for sending emails using SMTP.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly AppConfiguration _appConfiguration;
    private readonly Dictionary<string, string> _emailTemplates = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="environment">The web host environment used to access application-specific paths.</param>
    /// <param name="logger">The logger instance for logging messages.</param>
    /// <param name="appConfiguration">The settings containing email configuration details.</param>
    public EmailService(IWebHostEnvironment environment, ILogger<EmailService> logger, AppConfiguration appConfiguration)
    {
        _logger = logger;
        _appConfiguration = appConfiguration;

        // Load email templates

        string templateDir = Path.Combine(environment.WebRootPath, "templates");
        if (!Directory.Exists(templateDir))
        {
            _logger.LogError("Email template directory not found at path: {Path}", templateDir);
            return;
        }

        foreach (var file in Directory.GetFiles(templateDir, "*.html"))
        {
            string templateName = Path.GetFileNameWithoutExtension(file);
            _emailTemplates[templateName] = File.ReadAllText(file);
        }
    }

    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="body">The body content of the email.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_appConfiguration.Email.Address));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        smtp.Timeout = _appConfiguration.Email.Timeout;
        await smtp.ConnectAsync(_appConfiguration.Email.Provider,  _appConfiguration.Email.Port, SecureSocketOptions.Auto, cancellationToken);
        try
        {
            try
            {
                await smtp.AuthenticateAsync(_appConfiguration.Email.Address, _appConfiguration.Email.Password, cancellationToken);
            }
            catch (AuthenticationException ex)
            {
                // SMTP server might not require authentication, log and continue
                _logger.LogWarning(ex, "SMTP authentication failed - server may not require authentication");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during SMTP authentication");
                return;
            }
            await smtp.SendAsync(email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
        }
        finally
        {
            await smtp.DisconnectAsync(true, cancellationToken);
        }
    }

    /// <summary>
    /// Sends an email using the "blank" HTML template. The template placeholders:
    /// <br/>- {{TITLE}} will be replaced with <paramref name="subject"/>,
    /// <br/>- {{MESSAGE_BODY}} will be replaced with <paramref name="body"/>,
    /// <br/>- {{USERNAME}} will be replaced with <paramref name="username"/>.
    /// <br/>
    /// If the template was not successfully loaded, the method will still call the raw
    /// <see cref="SendEmailAsync(string,string,string,CancellationToken)"/> with a best-effort constructed body.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="username">Username to insert into the template.</param>
    /// <param name="subject">Email subject/title.</param>
    /// <param name="body">Plain text or HTML message body to insert into the template.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    public async Task SendEmailAsync(string to, string username, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_emailTemplates.TryGetValue("emailBlank", out var emailBlankDoc))
        {
            _logger.LogWarning("Email 'blank' template not found. Falling back to raw send.");
            await SendEmailAsync(to, subject, body, cancellationToken);
            return;
        }

        string finalBody = emailBlankDoc.Replace("{{TITLE}}", subject)
            .Replace("{{MESSAGE_BODY}}", body)
            .Replace("{{USERNAME}}", username);
        await SendEmailAsync(to, subject, finalBody, cancellationToken);
    }

    /// <summary>
    /// Sends an email using the action-style HTML template which contains an action button.
    /// Template placeholders:
    /// <br/>- {{TITLE}} -> <paramref name="subject"/>,
    /// <br/>- {{MESSAGE_BODY}} -> <paramref name="body"/>,
    /// <br/>- {{USERNAME}} -> <paramref name="username"/>,
    /// <br/>- {{ACTION_URL}} -> <paramref name="actionUrl"/>,
    /// <br/>- {{BUTTON_TEXT}} -> <paramref name="buttonText"/>.
    /// <br/>
    /// As with the other template-based overload, if the template is not available the method will
    /// fall back to constructing a body string (which may be empty) and call the raw send method.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="username">Username to insert into the template.</param>
    /// <param name="subject">Email subject/title.</param>
    /// <param name="body">Plain text or HTML message body to insert into the template.</param>
    /// <param name="actionUrl">URL to use for the primary action button in the template.</param>
    /// <param name="buttonText">Text to display on the action button.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    public async Task SendEmailAsync(string to, string username, string subject, string body, string actionUrl,
        string buttonText, CancellationToken cancellationToken = default)
    {
        if (!_emailTemplates.TryGetValue("emailAction", out var emailActionDoc))
        {
            _logger.LogWarning("Email 'action' template not found. Falling back to raw send.");
            await SendEmailAsync(to, subject, body, cancellationToken);
            return;
        }

        string finalBody = emailActionDoc.Replace("{{TITLE}}", subject)
            .Replace("{{MESSAGE_BODY}}", body)
            .Replace("{{USERNAME}}", username)
            .Replace("{{ACTION_URL}}", actionUrl)
            .Replace("{{BUTTON_TEXT}}", buttonText);
        await SendEmailAsync(to, subject, finalBody, cancellationToken);
    }
}
