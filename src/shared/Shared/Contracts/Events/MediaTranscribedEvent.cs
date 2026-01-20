using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events
{
    public record MediaTranscribedEvent
    {
        public Guid MediaId { get; init; }
        public Guid TranscriptionId { get; init; }
        public int TotalSegments { get; init; }
        public string Language { get; init; } = string.Empty;
        public TimeSpan ProcessingDuration { get; init; }
        public DateTime TranscribedAt { get; init; }
    }
}
