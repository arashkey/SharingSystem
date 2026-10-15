using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Shares;
using SharingSystem.Web.Infrastructure;

namespace SharingSystem.Web.Controllers.Api;

/// <summary>
/// JSON endpoints used by the upload page: create a draft share, upload chunks, complete the share.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/shares")]
public sealed class SharesApiController(IShareUploadService uploadService) : ControllerBase
{
    [HttpGet("available")]
    public async Task<ActionResult<IdAvailabilityResponse>> IsAvailable([FromQuery] string id, CancellationToken cancellationToken) =>
        new IdAvailabilityResponse(await uploadService.IsIdAvailableAsync(id, cancellationToken));

    [HttpPost("")]
    public async Task<ActionResult<CreatedShareDto>> Create(CreateShareRequest request, CancellationToken cancellationToken) =>
        await uploadService.CreateAsync(request, cancellationToken);

    /// <summary>Receives one chunk of a file as the raw request body.</summary>
    [HttpPost("{id}/files")]
    public async Task<ActionResult<UploadChunkResult>> UploadChunk(
        string id,
        [FromQuery] Guid? fileId,
        [FromQuery] string name,
        [FromQuery] int chunkIndex,
        [FromQuery] int totalChunks,
        CancellationToken cancellationToken)
    {
        var request = new UploadChunkRequest
        {
            ShareId = id,
            UploadToken = Request.Headers[WebConstants.UploadTokenHeader],
            FileId = fileId,
            FileName = name,
            ChunkIndex = chunkIndex,
            TotalChunks = totalChunks,
            Content = Request.Body
        };

        return await uploadService.UploadChunkAsync(request, cancellationToken);
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<CompletedShareDto>> Complete(string id, CancellationToken cancellationToken) =>
        await uploadService.CompleteAsync(id, Request.Headers[WebConstants.UploadTokenHeader], cancellationToken);
}

public sealed record IdAvailabilityResponse(bool IsAvailable);
