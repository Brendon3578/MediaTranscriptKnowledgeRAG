using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events
{
    public class MediaTranscribedEvent
    {
        public Guid MediaId { get; init; }
        public Guid TranscriptionId { get; init; }
        public string TranscriptionText { get; init; } = string.Empty;
        public int WordCount { get; init; }
        public TimeSpan ProcessingDuration { get; init; }
        public DateTime TranscribedAt { get; init; }
    }
}
