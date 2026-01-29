using System;
using System.Collections.Generic;
using System.Text;
using Whisper.net.Ggml;

namespace MediaTranscription.Worker.Infrastructure.Interfaces
{
    public interface IWhisperModelProvider
    {
        Task<string> EnsureModelAsync(GgmlType modelType, CancellationToken cancellationToken);
    }
}
