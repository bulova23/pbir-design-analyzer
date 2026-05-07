namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Thrown when a PBIR write operation is attempted on a file that is exclusively
/// locked by another process (e.g. Power BI Desktop has the file open).
/// </summary>
public sealed class PbirFileLockedException : IOException
{
    /// <summary>Gets the path of the file that was found to be locked.</summary>
    public string FilePath { get; }

    /// <summary>Initializes a new instance of <see cref="PbirFileLockedException"/>.</summary>
    /// <param name="filePath">The locked file path.</param>
    public PbirFileLockedException(string filePath)
        : base($"PBIR file is locked and cannot be written: '{filePath}'. Close Power BI Desktop and retry.")
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Guards against writing to PBIR files that are exclusively locked by another process.
/// Called by all write-path services before any mutation is committed.
/// </summary>
public static class PbirFileWriteGuard
{
    /// <summary>
    /// Checks whether <paramref name="filePath"/> can be exclusively opened for writing.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to probe.</param>
    /// <exception cref="PbirFileLockedException">
    /// Thrown when the file exists and is locked by another process.
    /// </exception>
    /// <remarks>
    /// If the file does not yet exist (new report creation), the check succeeds immediately.
    /// The guard uses a zero-byte probe: it opens the file with <see cref="FileShare.None"/>
    /// and disposes immediately — no data is read or written.
    /// </remarks>
    public static void CheckAndGuard(string filePath)
    {
        if (!File.Exists(filePath))
        {
            // New file; no lock possible.
            return;
        }

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            // File opened successfully — no lock detected; dispose immediately.
        }
        catch (IOException)
        {
            throw new PbirFileLockedException(filePath);
        }
    }
}
