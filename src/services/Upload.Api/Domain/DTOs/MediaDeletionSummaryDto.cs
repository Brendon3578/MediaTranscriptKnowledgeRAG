namespace Upload.Api.Domain.DTOs
{
    public class MediaDeletionSummaryDto
    {
        public Guid MediaId { get; set; }
        public int EmbeddingsDeleted { get; set; }
        public int SegmentsDeleted { get; set; }
        public int TranscriptionsDeleted { get; set; }
        public bool FileDeleted { get; set; }
    }
}
