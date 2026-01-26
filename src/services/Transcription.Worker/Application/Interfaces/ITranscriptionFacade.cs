namespace MediaTranscription.Worker.Application.Interfaces
{
    /// <summary>
    /// Abstração para serviços de transcrição.
    /// Permite trocar facilmente entre implementações (local, cloud, etc) apenas seguindo esse contrato.
    /// </summary>
    public interface ITranscriptionFacade
    {
        Task<TranscriptionResultDto> TranscribeAsync(string filePath, string contentType, CancellationToken cancellationToken);
    }

    public record TranscriptionSegmentDto(
        int SegmentIndex,
        string Text,
        double StartSeconds,
        double EndSeconds
    );

    public record TranscriptionResultDto(
        string TranscriptionText,
        IReadOnlyList<TranscriptionSegmentDto> Segments,
        string Language
    );
}
