using Query.Api.Infrastructure;
using Query.Api.Infrastructure.Persistence;

namespace Query.Api.Application
{
    public class RagFacade
    {
        private readonly EmbeddingGeneratorService _embeddingService;
        private readonly TranscriptionSegmentVectorSearchRepository _vectorRepo;
        private readonly GenerateAnswerUseCase _chatService;
        private readonly IConfiguration _configuration;

        public RagFacade(
            EmbeddingGeneratorService embeddingService,
            TranscriptionSegmentVectorSearchRepository vectorRepo,
            GenerateAnswerUseCase chatService,
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
