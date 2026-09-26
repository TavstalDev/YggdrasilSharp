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
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EmailService> _logger;
    private readonly AppConfiguration _appConfiguration;
    private string _emailBlankDoc = string.Empty;
    private string _emailActionDoc = string.Empty;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="environment">The web host environment used to access application-specific paths.</param>
    /// <param name="logger">The logger instance for logging messages.</param>
    /// <param name="appConfiguration">The settings containing email configuration details.</param>
    public EmailService(IWebHostEnvironment environment, ILogger<EmailService> logger, AppConfiguration appConfiguration)
    {
        _environment = environment;
        _logger = logger;
        _appConfiguration = appConfiguration;
        // Load templates asynchronously in background. Email sending will use empty templates if loading fails.
        _ = InitAsync();
    }

    /// <summary>
    /// Asynchronously loads email templates from the web root "templates" folder.
    /// <br/><br/>
    /// Expected files:
    /// <br/>- {webroot}/templates/emailBlank.html
    /// <br/>- {webroot}/templates/emailAction.html
    /// <br/><br/>
    /// If the templates directory or files are missing, the method logs an error and leaves
    /// the corresponding template fields as empty strings. This method is private and invoked
    /// by the constructor in the background.
    /// </summary>
    /// <returns>A task that completes when template loading has finished.</returns>
    private async Task InitAsync()
    {
        string templateDir = Path.Combine(_environment.WebRootPath, "templates");
        if (!Directory.Exists(templateDir))
        {
            _logger.LogError("Email template directory not found at path: {Path}", templateDir);
            return;
        }
        
        string templatePath = Path.Combine(templateDir, "emailBlank.html");
        if (!File.Exists(templatePath))
        {
            _logger.LogError("Email document not found at path: {Path}", templatePath);
            return;
        }
        _emailBlankDoc = await File.ReadAllTextAsync(templatePath);
        
        templatePath = Path.Combine(templateDir, "emailAction.html");
        if (!File.Exists(templatePath))
        {
            _logger.LogError("Email document not found at path: {Path}", templatePath);
            return;
        }
        _emailActionDoc = await File.ReadAllTextAsync(templatePath);
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
        await smtp.ConnectAsync(_appConfiguration.Email.Provider,  _appConfiguration.Email.Port, SecureSocketOptions.None, cancellationToken);
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
                // Unexpected error - log but continue
                _logger.LogError(ex, "Unexpected error during SMTP authentication");
            }

            await smtp.SendAsync(email, cancellationToken);
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
        string finalBody = _emailBlankDoc.Replace("{{TITLE}}", subject)
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
        string finalBody = _emailActionDoc.Replace("{{TITLE}}", subject)
            .Replace("{{MESSAGE_BODY}}", body)
            .Replace("{{USERNAME}}", username)
            .Replace("{{ACTION_URL}}", actionUrl)
            .Replace("{{BUTTON_TEXT}}", buttonText);
        await SendEmailAsync(to, subject, finalBody, cancellationToken);
    }
}