namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the email (SMTP) settings loaded from the configuration.
/// </summary>
public class EmailConfig
{
    /// <summary>
    /// Gets or sets the email provider.
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// Gets or sets the email port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the email password.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailConfig"/> class with explicit values.
    /// </summary>
    /// <param name="provider">The SMTP server hostname or provider identifier.</param>
    /// <param name="port">The SMTP port used to send email (commonly 25, 465, or 587).</param>
    /// <param name="address">The email address used as the sender for outgoing messages.</param>
    /// <param name="password">The password or app-specific secret for the sender account.</param>
    public EmailConfig(string provider, int port, string address, string password)
    {
        Provider = provider;
        Port = port;
        Address = address;
        Password = password;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="InvalidOperationException">Thrown if a required email configuration value is missing.</exception>
    public EmailConfig(IConfiguration configuration)
    {
        Provider = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.EmailProvider);
        Port = configuration.GetValue(Constants.ConfigurationKeys.EmailPort, 587); 
        Address = AppConfiguration.GetString(configuration, Constants.EnvironmentKeys.EmailAddress); 
        Password = AppConfiguration.GetString(configuration, Constants.EnvironmentKeys.EmailPassword);
    }
}