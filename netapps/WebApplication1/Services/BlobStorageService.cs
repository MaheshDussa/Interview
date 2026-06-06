using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Models.DTOs;
using WebApplication1.ServiceCollections;

namespace WebApplication1.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationTelemetry _telemetry;

    public BlobStorageService(IConfiguration configuration, IApplicationTelemetry telemetry)
    {
        _configuration = configuration;
        _telemetry = telemetry;
    }

    public async Task<FileUploadResponse> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null)
        {
            throw new ValidationException("File is required.");
        }

        if (file.Length <= 0)
        {
            throw new ValidationException("File is empty.");
        }

        var containerClient = await GetContainerClientAsync(cancellationToken);
        var safeFileName = Path.GetFileName(file.FileName);
        var blobName = $"{Guid.NewGuid():N}-{safeFileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType
                }
            },
            cancellationToken);

        _telemetry.TrackEvent("FileUploaded", new Dictionary<string, string>
        {
            ["blobName"] = blobName,
            ["containerName"] = containerClient.Name,
            ["contentType"] = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
        }, new Dictionary<string, double>
        {
            ["fileSizeBytes"] = file.Length
        });

        return new FileUploadResponse
        {
            FileName = safeFileName,
            BlobName = blobName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Size = file.Length,
            BlobUri = blobClient.Uri.ToString()
        };
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsBlobStorageConfigured())
        {
            throw new InvalidOperationException("Blob Storage is not configured for this environment.");
        }

        var connectionString = _configuration.GetRequiredValue("BlobStorage:ConnectionString");
        var containerName = _configuration.GetRequiredValue("BlobStorage:ContainerName");

        var serviceClient = new BlobServiceClient(connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(containerName.ToLowerInvariant());
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        return containerClient;
    }
}