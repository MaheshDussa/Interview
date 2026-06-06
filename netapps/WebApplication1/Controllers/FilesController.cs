using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.ServiceCollections;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;

    public FilesController(IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (!HttpContext.RequestServices.GetRequiredService<IConfiguration>().IsBlobStorageConfigured())
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Blob Storage is not configured for this environment."
            });
        }

        var result = await _blobStorageService.UploadAsync(file, cancellationToken);
        return Ok(result);
    }
}