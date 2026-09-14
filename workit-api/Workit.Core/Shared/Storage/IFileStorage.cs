namespace Workit.Core.Shared.Storage;

/// <summary>
/// Stores and retrieves uploaded files. The local-disk implementation is the only one today;
/// swapping to S3/Azure Blob/Cloudinary later only means adding a new implementation and
/// changing the DI registration in <see cref="Workit.Core.DependencyInjection"/>.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves <paramref name="content"/> under <paramref name="containerName"/> and returns an
    /// opaque storage key that <see cref="OpenReadAsync"/> and <see cref="DeleteAsync"/> accept.
    /// </summary>
    Task<string> SaveAsync(
        Stream content,
        string containerName,
        string fileExtension,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
