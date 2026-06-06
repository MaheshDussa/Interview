using Microsoft.AspNetCore.Http;
using WebApplication1.Models.DTOs;

namespace WebApplication1.Services;

public interface IBlobStorageService
{
    Task<FileUploadResponse> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
}