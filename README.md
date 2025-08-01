# Adliance.AzureBlobSimulator

Simple ASP.NET Core (Kestrel) server that uses a local directory and simulates an API 
that is compatible with Azure Blob Storage API to read/write files and directories on this local directory.

This tool is not intended to provide a fully compatible emulator for Azure Storage (use Azurite for that),
but can be used to make certain "cloud native" applications that require Azure Blobs work with local directories.

The supported operations have all be tested using the official C# Azure Blob SDK. But please note that some
of the operations do not work with Azure Storage Explorer yet (primarily upload and download of blobs).

## Supported Operations
### Container Operations
- **Create Container** (`PUT /{containerName}?restype=container`)
  - Creates a new container directory
  - Returns 409 Conflict if container already exists
  
- **Get Container Properties** (`GET /{containerName}?restype=container`)
  - Retrieves container metadata and properties
  - Returns 404 if container doesn't exist
  
- **Check Container Exists** (`HEAD /{containerName}`)
  - Checks if a container exists without returning content
  - Returns 404 if container doesn't exist
  
- **List Blobs** (`GET /{containerName}?comp=list&restype=container`)
  - Lists all blobs in a container with their properties
  - Recursively scans subdirectories
  - Returns blob names with forward slash separators

### Blob Operations
- **Upload Blob** (`PUT /{containerName}/{blobName}`)
  - Uploads a blob to the specified container
  - Creates container and subdirectories if they don't exist
  - Overwrites existing blobs
  
- **Download Blob** (`GET /{containerName}/{blobName}`)
  - Downloads blob content
  - Supports range requests
  - Returns appropriate content type based on file extension
  
- **Get Blob Properties** (`HEAD /{containerName}/{blobName}`)
  - Retrieves blob metadata without downloading content
  - Returns Content-Length, Content-Type, Last-Modified, ETag

### Authentication
- **Shared Key Authentication**
  - Supports multiple storage account name/key pairs
  - Configured via `appsettings.json`
  
- **SAS Token Support**
  - Basic SAS token validation (signature verification not implemented)

### More
- **Container Path Mapping**
  - Default: containers map to subdirectories under base path
  - Configurable: specific containers can map to custom local paths
  - Supports sub directories and converts them to blobs with `/` as separator
- **System Containers** (`$logs`, `$blobchangefeed`)
  - Returns empty blob lists for Azure SDK compatibility