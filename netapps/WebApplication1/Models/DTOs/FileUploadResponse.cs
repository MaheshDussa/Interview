namespace WebApplication1.Models.DTOs;

public class FileUploadResponse
{
    public string FileName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string BlobUri { get; set; } = string.Empty;
}