using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Whisper.net.Ggml;

namespace MediaTranscription.Worker.Configuration
{
    public class WhisperOptions
    {
        public GgmlType ModelType { get; set; } = GgmlType.Medium;
        public string ModelsDirectory { get; set; } = "models";
        public string Language { get; set; } = "pt";
        public int Threads { get; set; } = 4;
    }
}
