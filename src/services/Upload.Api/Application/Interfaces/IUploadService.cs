using Microsoft.AspNetCore.Mvc;
using Upload.Api.Domain.DTOs;

namespace Upload.Api.Application.Interfaces
{
    public interface IUploadService
    {
        Task <MediaUploadDto> UploadFileAsync(IFormFile file, CancellationToken ct);
        Task<MediaUploadDto?> GetStatus(Guid id, CancellationToken ct);
    }
}
