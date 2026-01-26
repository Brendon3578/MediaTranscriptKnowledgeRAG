using Microsoft.Extensions.AI;
using System.Text;

namespace Query.Api.Application
{

    public class GenerateAnswerUseCase
    {
        private readonly IChatClient _chatClient;

        public GenerateAnswerUseCase(IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async Task<string> GenerateAnswerAsync(string question, List<ResultSource> sources)
        {
            var contextBuilder = new StringBuilder();

            var orderedSourcered = sources
                .OrderBy(s => s.MediaId)
                .ThenBy(s => s.Start);

            foreach (var source in orderedSourcered)
            {
                var timeRange = $"[{TimeSpan.FromSeconds(source.Start):mm\\:ss} - {TimeSpan.FromSeconds(source.End):mm\\:ss}]";
                contextBuilder.AppendLine($"{timeRange} {source.Text}");
            }

            // ordenando os sources pelo tempo e mídia:



            var prompt = $@"
            Você é um assistente útil que responde perguntas com base APENAS no contexto fornecido abaixo.
            Se a resposta não estiver no contexto, diga que não sabe. Não invente informações.
            Responda de forma direta e concisa.

            Contexto:
            {contextBuilder}

            Pergunta: {question}
            ";

            var response = await _chatClient.CompleteAsync(prompt);

            return response.Message.Text ?? "Não foi possível gerar uma resposta.";
        }
    }
}
