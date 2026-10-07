using nClam;
using Tavstal.YggdrasilSharp.Models;

namespace Tavstal.YggdrasilSharp.Services.AntiVirus;

/// <inheritdoc/>
public class ClamAvService : IAntiVirusService
{
    private readonly ILogger _logger;
    private readonly ClamClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClamAvService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging messages.</param>
    /// <param name="configuration">The settings containing the ClamAV host and port.</param>
    public ClamAvService(ILogger<ClamAvService> logger, AppConfiguration configuration)
    {
        _logger = logger;
        _client = new ClamClient(configuration.ClamAvHost, configuration.ClamAvPort);
    }

    /// <inheritdoc/>
    public async Task<bool> IsInfectedAsync(Stream stream)
    {
        try
        {
            var result = await _client.SendAndScanFileAsync(stream);
            stream.Position = 0;
            return result.Result == ClamScanResults.VirusDetected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured while scanning file.");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsInfectedAsync(IFormFile file)
    {
        try
        {
            await using Stream stream = file.OpenReadStream();
            var result = await _client.SendAndScanFileAsync(stream);
            return result.Result == ClamScanResults.VirusDetected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured while scanning file.");
            return false;
        }
    }
}
