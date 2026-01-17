using Microsoft.AspNetCore.Mvc;
using Upload.Api.Infrastructure.DTOs;

namespace Upload.Api.Services
{
    public interface IUploadService
    {
        Task <MediaUploadDto> UploadFileAsync(IFormFile file, CancellationToken ct);
        Task<MediaUploadDto?> GetStatus(Guid id, CancellationToken ct);
    }
}
