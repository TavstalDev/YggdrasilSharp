using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the database settings loaded from the configuration.
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// Gets or sets the database provider type.
    /// Determines which Entity Framework Core database provider to use.
    /// </summary>
    [JsonPropertyName("Provider")]
    public string Provider { get; set; }
    
    /// <summary>
    /// Gets or sets the database version.
    /// Used to configure database-specific behavior and compatibility options.
    /// </summary>
    [JsonPropertyName("Version")]
    public string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    [JsonPropertyName("ConnectionString")]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConfig"/> class with explicit values.
    /// </summary>
    /// <param name="provider">The database provider type (e.g., "MySql", "Sqlite", "PostgreSql").</param>
    /// <param name="version">The database version to use for compatibility.</param>
    /// <param name="connectionString">The database connection string.</param>
    public DatabaseConfig(string provider, string version, string connectionString)
    {
        Provider = provider;
        Version = version;
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="InvalidOperationException">Thrown if a required database configuration value is missing.</exception>
    public DatabaseConfig(IConfiguration configuration)
    {
        Provider = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.DatabaseProvider);
        Version = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.DatabaseVersion);
        ConnectionString = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.DatabaseConnectionString);
    }
}
