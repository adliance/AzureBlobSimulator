# Adliance.AzureBlobSimulator

![Container Build](https://github.com/adliance/AzureBlobSimulator/actions/workflows/docker-build-push.yml/badge.svg)

Simple ASP.NET Core (Kestrel) server that uses a local directory and simulates an API 
that is compatible with Azure Blob Storage API to read/write files and directories on this local directory.

This tool is not intended to provide a fully compatible emulator for Azure Storage (use Azurite for that),
but can be used to make certain "cloud native" applications that require Azure Blobs work with local directories.

## Supported operations
- Get account properties
- Get containers
- Get container properties
- List blobs in container
- Get blob

## Unsupported operations
The folloowing operations are not supported yet, but it is planned to support them in the future.
- Create Containers
- Upload blobs

Also not supported are blobs with a `/` in the name,

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
