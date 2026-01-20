using System;
using System.Collections.Generic;
using System.Text;

namespace MediaTranscription.Worker.Facade
{
    /// <summary>
    /// Abstração para serviços de transcrição.
    /// Permite trocar facilmente entre implementações (local, cloud, etc) apenas seguindo esse contrato.
    /// </summary>
    public interface ITranscriptionFacade
    {
        /// <summary>
        /// Transcreve um arquivo de áudio ou vídeo.
        /// </summary>
        /// <param name="filePath">Caminho completo do arquivo</param>
        /// <param name="contentType">MIME type do arquivo (audio/* ou video/*)</param>
        /// <returns>Texto transcrito</returns>
        Task<string> TranscribeAsync(string filePath, string contentType);
    }
}
