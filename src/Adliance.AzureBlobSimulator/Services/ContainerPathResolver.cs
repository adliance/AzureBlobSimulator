using Adliance.AzureBlobSimulator.Models;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Services;

public interface IContainerPathResolver
{
    string GetContainerPath(string containerName);
    bool IsContainerConfigured(string containerName);
    IEnumerable<string> GetConfiguredContainers();
}

public class ContainerPathResolver : IContainerPathResolver
{
    private readonly StorageOptions _storageOptions;
    private readonly Dictionary<string, string> _containerPaths;

    public ContainerPathResolver(IOptions<StorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
        _containerPaths = _storageOptions.Containers.ToDictionary(c => c.Name, c => c.LocalPath);
    }

    public string GetContainerPath(string containerName)
    {
        // Check if container has a specific configured path
        if (_containerPaths.TryGetValue(containerName, out var containerPath))
        {
            return containerPath;
        }

        // Fall back to base path + container name structure
        return Path.Combine(_storageOptions.LocalPath, containerName);
    }

    public bool IsContainerConfigured(string containerName)
    {
        return _containerPaths.ContainsKey(containerName);
    }

    public IEnumerable<string> GetConfiguredContainers()
    {
        return _containerPaths.Keys;
    }
}
