using System.Runtime.InteropServices;
using Tavstal.YggdrasilSharp.Models;

namespace Tavstal.YggdrasilSharp.Services.AntiVirus;

/// <summary>
/// Scans content for malware using the Windows Antimalware Scan Interface (AMSI).
/// </summary>
public class DefenderService: IAntiVirusService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IntPtr _context;
    private readonly IntPtr _session;

    [DllImport("amsi.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern int AmsiInitialize(string appName, out IntPtr amsiContext);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern int AmsiOpenSession(IntPtr amsiContext, out IntPtr amsiSession);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern int AmsiScanBuffer(IntPtr amsiContext, byte[] buffer, uint length, string contentName, IntPtr amsiSession, out AMSI_RESULT result);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern void AmsiCloseSession(IntPtr amsiContext, IntPtr amsiSession);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern void AmsiUninitialize(IntPtr amsiContext);

    private enum AMSI_RESULT
    {
        AMSI_RESULT_CLEAN = 0,
        AMSI_RESULT_NOT_DETECTED = 1,
        AMSI_RESULT_BLOCKED_BY_ADMIN_START = 16384,
        AMSI_RESULT_BLOCKED_BY_ADMIN_END = 20479,
        AMSI_RESULT_DETECTED = 32768
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefenderService"/> class and opens an AMSI session.
    /// </summary>
    /// <param name="logger">The logger instance for logging messages.</param>
    /// <param name="appConfiguration">The settings providing the application name registered with AMSI.</param>
    /// <exception cref="InvalidOperationException">Thrown when AMSI initialization fails.</exception>
    public DefenderService(ILogger<DefenderService> logger, AppConfiguration appConfiguration)
    {
        _logger = logger;
        if (AmsiInitialize(appConfiguration.Swagger.Name, out _context) != 0)
            throw new InvalidOperationException("Failed to initialize AMSI.");
        AmsiOpenSession(_context, out _session);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_session != IntPtr.Zero)
            AmsiCloseSession(_context, _session);
        if (_context != IntPtr.Zero)
            AmsiUninitialize(_context);
    }

    /// <inheritdoc/>
    public async Task<bool> IsInfectedAsync(Stream stream)
    {
        try
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            byte[] data = ms.ToArray();
            int result = AmsiScanBuffer(_context, data, (uint)data.Length, "stream.bin", _session, out AMSI_RESULT scanResult);
            if (result != 0)
            {
                _logger.LogError($"AMSI scan failed with error code: {result}");
                return false;
            }
            stream.Position = 0;
            return scanResult >= AMSI_RESULT.AMSI_RESULT_DETECTED;
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
            return await IsInfectedAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured while scanning file.");
            return false;
        }
    }
}
