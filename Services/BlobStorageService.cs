using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ORUApi.Services;

public class BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
{
    private const string ContainerName = "university-files";
    private static readonly TimeSpan SasExpiry = TimeSpan.FromDays(3650);

    private BlobContainerClient GetContainer()
    {
        var cs = config["ORUKey"] ?? config["AzureStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(cs) || cs == "UseDevelopmentStorage=true")
            throw new InvalidOperationException(
                "Azure Storage connection string is not configured. Set AzureStorage:ConnectionString in App Service settings or add ORUKey to Key Vault.");
        return new BlobServiceClient(cs).GetBlobContainerClient(ContainerName);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        try
        {
            var container = GetContainer();
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobName = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blob = container.GetBlobClient(blobName);

            await blob.UploadAsync(file.OpenReadStream(), new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            });

            var sas = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(SasExpiry)
            };
            sas.SetPermissions(BlobSasPermissions.Read);
            return blob.GenerateSasUri(sas).ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blob upload failed for folder {Folder}", folder);
            throw new InvalidOperationException(
                "File upload is currently unavailable. Azure Storage is not configured or reachable.", ex);
        }
    }

    public async Task DeleteAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var blobName = string.Join("/", uri.Segments[2..]);
            await GetContainer().GetBlobClient(blobName).DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blob deletion failed for {Url}", fileUrl);
            throw new InvalidOperationException(
                "File deletion is currently unavailable. Azure Storage is not configured or reachable.", ex);
        }
    }
}