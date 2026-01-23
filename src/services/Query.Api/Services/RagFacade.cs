using Query.Api.DTOs;
using Query.Api.Repositories;
using Query.Api.Services;

namespace Query.Api.Services
{
    public class RagFacade
    {
        private readonly EmbeddingGeneratorService _embeddingService;
        private readonly VectorSearchRepository _vectorRepo;
        private readonly RagChatService _chatService;
        private readonly IConfiguration _configuration;

        public RagFacade(
            EmbeddingGeneratorService embeddingService,
            VectorSearchRepository vectorRepo,
            RagChatService chatService,
            IConfiguration configuration)
        {
            _embeddingService = embeddingService;
            _vectorRepo = vectorRepo;
            _chatService = chatService;
            _configuration = configuration;
        }

        public async Task<QueryResponse> ProcessQueryAsync(QueryRequest request)
        {
            // 1. Gerar embedding da pergunta
            var queryVector = await _embeddingService.GenerateQueryEmbeddingAsync(request.Question);

            // 2. Buscar segmentos relevantes
            var modelName = _configuration["Embedding:Model"] ?? "nomic-embed-text";
            var sources = await _vectorRepo.SearchAsync(queryVector, modelName, request.Filters, request.TopK);

            if (!sources.Any())
            {
                return new QueryResponse 
                { 
                    Answer = "Não encontrei informações relevantes no contexto disponível para responder sua pergunta.", 
                    Sources = new() 
                };
            }

            // 3. Gerar resposta com LLM
            var answer = await _chatService.GenerateAnswerAsync(request.Question, sources);

            return new QueryResponse
            {
                Answer = answer,
                Sources = sources
            };
        }
    }
}
