using Adliance.AzureBlobSimulator.Models;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Services;

public class ContainerService(IOptions<StorageOptions> options)
{
    public bool DoesContainerExist(string container)
    {
        return GetContainers().ContainsKey(container);
    }

    public bool DoesBlobExist(string container, string blob)
    {
        if (!DoesContainerExist(container)) return false;
        var filePath = Path.Combine(GetContainerPath(container), blob);
        return File.Exists(filePath);
    }

    public IEnumerable<string> GetFilePaths(string container)
    {
        if (GetContainers().TryGetValue(container, out var containerPath))
        {
            return Directory.GetFiles(containerPath, "*", SearchOption.TopDirectoryOnly);
        }

        return new List<string>();
    }

    public Dictionary<string, string> GetContainers()
    {
        var result = new Dictionary<string, string>
        {
            { Constants.BlobChangeFeedContainerName, Constants.BlobChangeFeedContainerName },
            { Constants.LogsContainerName, Constants.LogsContainerName }
        };

        foreach (var d in options.Value.Containers)
        {
            if (Directory.Exists(d.LocalPath)) result.Add(d.Name, d.LocalPath);
        }

        if (Directory.Exists(options.Value.LocalPath))
        {
            foreach (var d in Directory.GetDirectories(options.Value.LocalPath))
            {
                result.Add(Path.GetFileName(d), d);
            }
        }

        return result;
    }

    public string GetContainerPath(string container)
    {
        if (GetContainers().TryGetValue(container, out var containerPath)) return containerPath;
        throw new Exception($"Container \"{container}\" not found.");
    }

    public string GetBlobPath(string container, string blob)
    {
        if (GetContainers().TryGetValue(container, out var containerPath))
        {
            var filePath = Path.Combine(containerPath, blob);
            if (File.Exists(filePath)) return filePath;
        }
        throw new Exception($"Blob \"{container}/{blob}\" not found.");
    }
}
