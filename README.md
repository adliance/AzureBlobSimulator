# Adliance.AzureBlobSimulator

![Container Build](https://github.com/adliance/AzureBlobSimulator/actions/workflows/docker-build-push.yml/badge.svg)

Simple ASP.NET Core (Kestrel) server that uses a local directory and simulates an API
that is compatible (enough) with Azure Blob Storage API to read/write files and directories on this local directory.

This tool is not intended to provide a fully compatible emulator for Azure Storage (use Azurite or other tools 
for that), but it can be used to make certain "cloud native" applications (that require Azure Blobs)
work with local directories.

For now, only "read" operations are supported, but it's planned to allow basic create/update operations
(for example, create containers or upload blobs).

Please note that the Azure Storage Manager is doing a lot of stuff under the hood, like using SAS-URLs for
downloading blobs. This is not supported yet in this tool, but it supports the official `Azure.Storage.Blobs` SDK
and all unit tests use this SDK.

## Supported operations
- Get account properties
- List containers
- Get container properties
- List blobs in container
- Get blob properties
- Download blob

## Unsupported operations
The folloowing operations are not supported (yet), but it is planned to support them at some point in the future.
- Create containers (maybe also delete containers)
- Upload and delete blobs
- Support SAS-URLs for all supported operations

Other operations may or may not be supported in the future.

Also not supported (to keep it simple for now are):
- Blobs with hierarchical namespaces (e.g. `my-container/sub-container/blob.txt`)


## Available as Docker image

`docker pull ghcr.io/adliance/azureblobsimulator:latest`

You can freely configure account keys as well as different containers (each one mapping to a different local path).

For example, use this docker-compose file:

```
services:
  Adliance.AzureBlobSimulator:
    image: ghcr.io/adliance/azureblobsimulator:latest
    ports:
      - "10000:80"
    volumes:
      -  ./storage/default:/storage       
      -  ./storage/additional1:/first-container    
      -  ./storage/additional2:/second-container       
    environment:
      - Storage__LocalPath=/storage
      - Storage__Accounts__0__Name=devstoreaccount1
      - Storage__Accounts__0__Key=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==
      - Storage__Containers__0__Name=my-first-special-container
      - Storage__Containers__0__LocalPath=/first-container
      - Storage__Containers__1__Name=my-second-special-container
      - Storage__Containers__1__LocalPath=/second-container
```
