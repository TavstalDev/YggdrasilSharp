namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the Swagger/OpenAPI documentation settings loaded from the configuration.
/// </summary>
public class SwaggerConfig
{
    /// <summary>
    /// Gets or sets the name of the Swagger/OpenAPI document.
    /// </summary>
    public string Name { get; set; } = "Yggdrasil.API";
    
    /// <summary>
    /// Gets or sets the description of the Swagger/OpenAPI document.
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the version of the Swagger/OpenAPI document.
    /// </summary>
    public string Version { get; set; } = "0.0.0";
    
    /// <summary>
    /// Gets or sets the contact name shown in the Swagger/OpenAPI document.
    /// </summary>
    public string ContactName { get; set; } = "Issues";

    /// <summary>
    /// Gets or sets the contact link (URL) shown in the Swagger/OpenAPI document.
    /// </summary>
    public string ContactLink { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the license name shown in the Swagger/OpenAPI document.
    /// </summary>
    public string LicenseName { get; set; } = "MIT License";
    
    /// <summary>
    /// Gets or sets the license link (URL) shown in the Swagger/OpenAPI document.
    /// </summary>
    public string LicenseLink { get; set; } = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerConfig"/> class.
    /// </summary>
    public SwaggerConfig() {}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerConfig"/> class with explicit values.
    /// </summary>
    /// <param name="name">The name of the Swagger/OpenAPI document.</param>
    /// <param name="description">The description of the Swagger/OpenAPI document.</param>
    /// <param name="version">The version of the Swagger/OpenAPI document.</param>
    /// <param name="contactName">The contact name to display in the Swagger/OpenAPI document.</param>
    /// <param name="contactLink">The contact link (URL) to display in the Swagger/OpenAPI document.</param>
    /// <param name="licenseName">The license name to display in the Swagger/OpenAPI document.</param>
    /// <param name="licenseLink">The license link (URL) to display in the Swagger/OpenAPI document.</param>
    public SwaggerConfig(string name, string description, string version, string contactName, string contactLink, string licenseName, string licenseLink)
    {
        Name = name;
        Description = description;
        Version = version;
        ContactName = contactName;
        ContactLink = contactLink;
        LicenseName = licenseName;
        LicenseLink = licenseLink;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="InvalidOperationException">Thrown if a required Swagger configuration value is missing.</exception>
    public SwaggerConfig(IConfiguration configuration)
    {
        Name = Settings.GetString(configuration, Constants.ConfigurationKeys.SwaggerName);
        Description = Settings.GetString(configuration, Constants.ConfigurationKeys.SwaggerDescription);
        Version = Settings.GetString(configuration, Constants.ConfigurationKeys.SwaggerVersion);
        ContactName = Settings.GetString(configuration, Constants.ConfigurationKeys.SwaggerContactName);
        ContactLink = Settings.GetString(configuration, Constants.ConfigurationKeys.SwaggerContactLink);
        LicenseName = Settings.GetString(configuration, Constants.ConfigurationKeys.SwaggerLicenseName);
        LicenseLink = Settings.GetString(configuration, Constants.ConfigurationKeys.SwaggerLicenseLink);
    }
}