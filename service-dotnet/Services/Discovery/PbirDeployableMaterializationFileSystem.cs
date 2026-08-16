namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationFileSystem : IPbirDeployableMaterializationFileSystem
{
    public string GetFullPath(string path) => Path.GetFullPath(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public IEnumerable<string> EnumerateEntries(string path) => Directory.EnumerateFileSystemEntries(path);
    public FileAttributes GetAttributes(string path) => File.GetAttributes(path);
    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public void WriteAllBytesCreateNew(string path, byte[] content) => Write(path, content, FileMode.CreateNew);
    public void WriteAllBytesReplace(string path, byte[] content) => Write(path, content, FileMode.Create);

    public IDisposable OpenExclusiveLock(string path) =>
        new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

    public void MoveDirectory(string source, string destination) => Directory.Move(source, destination);
    public void MoveFile(string source, string destination, bool overwrite) => File.Move(source, destination, overwrite);
    public void DeleteFile(string path) => File.Delete(path);

    private static void Write(string path, byte[] content, FileMode mode)
    {
        using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None);
        stream.Write(content);
        stream.Flush(flushToDisk: true);
    }
}
