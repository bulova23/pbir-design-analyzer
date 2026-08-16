namespace PowerBIModelingService.Services.Discovery;

internal interface IPbirDeployableMaterializationFileSystem
{
    string GetFullPath(string path);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    void CreateDirectory(string path);
    IEnumerable<string> EnumerateEntries(string path);
    FileAttributes GetAttributes(string path);
    byte[] ReadAllBytes(string path);
    void WriteAllBytesCreateNew(string path, byte[] content);
    void WriteAllBytesReplace(string path, byte[] content);
    IDisposable OpenExclusiveLock(string path);
    void MoveDirectory(string source, string destination);
    void MoveFile(string source, string destination, bool overwrite);
    void DeleteFile(string path);
}
