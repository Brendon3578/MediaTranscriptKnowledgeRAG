using Microsoft.AspNetCore.Mvc;
using Upload.Api.Domain.DTOs;

namespace Upload.Api.Application.Interfaces
{
    public interface IUploadService
    {
        Task<MediaUploadDto> UploadFileAsync(IFormFile file, string? model, CancellationToken ct);
        Task<MediaUploadDto?> GetStatus(Guid id, CancellationToken ct);
        Task<IReadOnlyList<MediaListItemDto>> GetAllMediaAsync(CancellationToken ct);
        Task<PagedResponseDto<TranscribedMediaDto>> GetTranscribedMediaAsync(int page, int pageSize, CancellationToken ct);
    }
}
