namespace Adliance.AzureBlobSimulator.Models;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string LocalPath { get; set; } = "./storage";
    public List<StorageAccountOptions> Accounts { get; set; } = [];
    public List<ContainerOptions> Containers { get; set; } = [];
}

public class StorageAccountOptions
{
    public required string Name { get; set; }
    public required string Key { get; set; }
}

public class ContainerOptions
{
    public required string Name { get; set; }
    public required string LocalPath { get; set; }
}

