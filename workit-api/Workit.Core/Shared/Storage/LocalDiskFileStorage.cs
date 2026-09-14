using Workit.Core.Shared.EnvironmentUtils;

namespace Workit.Core.Shared.Storage;

/// <summary>
/// Writes uploaded files to a directory on the API host. Storage keys are
/// <c>{containerName}/{fileName}</c>, relative to <see cref="WorkitSettings.Storage"/>'s
/// root path; callers never see or control the absolute path.
/// </summary>
public sealed class LocalDiskFileStorage(WorkitSettings settings) : IFileStorage
{
    private readonly string rootPath = Path.IsPathRooted(settings.Storage.RootPath)
        ? settings.Storage.RootPath
        : Path.Combine(Directory.GetCurrentDirectory(), settings.Storage.RootPath);

    public async Task<string> SaveAsync(
        Stream content,
        string containerName,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        var storageKey = $"{containerName}/{Guid.NewGuid():N}{fileExtension}";
        var absolutePath = ResolvePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(ResolvePath(storageKey));
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        File.Delete(ResolvePath(storageKey));
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        // storageKey segments are always a developer-chosen container name plus a generated
        // GUID file name (see SaveAsync); there is no user-controlled input to traverse with.
        return Path.Combine(rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
    }
}
