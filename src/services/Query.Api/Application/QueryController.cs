using Microsoft.AspNetCore.Mvc;

namespace Query.Api.Application
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly RagFacade _ragFacade;

        public QueryController(RagFacade ragFacade)
        {
            _ragFacade = ragFacade;
        }

        [HttpPost]
        public async Task<ActionResult<QueryResponse>> Query([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("A pergunta não pode estar vazia.");
            }

            if (request.Filters == null || request.Filters.MediaIds == null || request.Filters.MediaIds.Count == 0)
            {
                return BadRequest("Selecione uma mídia para consultar.");
            }

            try
            {
                var response = await _ragFacade.ProcessQueryAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Em produção, logar o erro e não expor detalhes
                return StatusCode(500, $"Erro interno ao processar a query: {ex.Message}");
            }
        }
    }
}
