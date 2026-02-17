using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

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
                throw new ValidationException("A pergunta não pode estar vazia.");
            }

            if (request.TimeRanges == null || request.TimeRanges.Count == 0)
            {
                throw new ValidationException("É necessário especificar intervalos de tempo para a consulta.");
            }

            var response = await _ragFacade.ProcessQueryAsync(request);
            return Ok(response);
        }
    }
}
