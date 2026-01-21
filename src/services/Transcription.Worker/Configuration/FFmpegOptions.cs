using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MediaTranscription.Worker.Configuration
{
    public class FFmpegOptions
    {
        [Required]
        public string ExecutablePath { get; set; } = "ffmpeg";
    }
}
