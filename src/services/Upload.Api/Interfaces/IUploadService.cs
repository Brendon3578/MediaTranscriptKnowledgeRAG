using Microsoft.AspNetCore.Mvc;
using Upload.Api.Models.DTOs;

namespace Upload.Api.Interfaces
{
    public interface IUploadService
    {
        Task <MediaUploadDto> UploadFileAsync(IFormFile file, CancellationToken ct);
        Task<MediaUploadDto?> GetStatus(Guid id, CancellationToken ct);
    }
}
