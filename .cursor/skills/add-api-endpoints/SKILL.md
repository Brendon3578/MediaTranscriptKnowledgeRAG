---
name: add-api-endpoints
description: Add new endpoints to Query.Api and Upload.Api following existing controller structure, use cases, interfaces, and clean architecture. Use when extending Query.Api or Upload.Api with new routes, when implementing new API operations, or when adding controller actions.
---

# Add API Endpoints (Query.Api / Upload.Api)

## Scope

This skill applies **only** to `Query.Api` and `Upload.Api`. Do not modify Workers (Transcription, Embedding) or Shared.

---

## Architecture Overview

| Layer | Query.Api | Upload.Api |
|-------|-----------|------------|
| Controller | `Application/*Controller.cs` | `Application/UploadController.cs` |
| Orchestration | Facade in `Application/` | Interface + UseCase |
| Models/DTOs | `Application/QueryModels.cs` | `Domain/DTOs/` |
| Infrastructure | `Infrastructure/` | `Infrastructure/` |

---

## Pattern 1: Upload.Api (Interface + UseCase)

Upload.Api uses **thin controllers** that delegate to services via **interfaces**.

### 1. Add DTOs in `Domain/DTOs/`

```csharp
namespace Upload.Api.Domain.DTOs
{
    public class YourResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // ...
    }
}
```

### 2. Add method to interface in `Application/Interfaces/`

```csharp
namespace Upload.Api.Application.Interfaces
{
    public interface IUploadService  // or IYourService
    {
        Task<YourResponseDto?> YourMethodAsync(Guid id, CancellationToken ct);
    }
}
```

### 3. Implement in UseCase (or create new UseCase)

UseCase lives in `Application/`, implements the interface, and uses:

- `UploadDbContext` for persistence
- `IEventPublisher` for RabbitMQ (only when publishing events)
- `IFileStorageFacade` for file operations (when applicable)
- `ILogger<T>` for logging

### 4. Add controller action

Controller must:

- Inject the service via constructor
- Validate input before calling the service
- Return `IActionResult` (Ok, NotFound, BadRequest, Accepted, StatusCode)
- Use `ErrorResponseRequest` for 4xx when appropriate
- Catch exceptions and return 500 with generic error (do not expose stack traces)
- Pass `CancellationToken` to async calls

```csharp
[HttpGet("{id}/your-action")]
public async Task<IActionResult> YourAction(Guid id, CancellationToken ct)
{
    var result = await _uploadService.YourMethodAsync(id, ct);
    if (result == null)
        return NotFound(new { error = "Recurso não encontrado" });
    return Ok(result);
}
```

### 5. Register in `Program.cs`

```csharp
builder.Services.AddScoped<IUploadService, UploadMediaUseCase>();
// Or new service:
// builder.Services.AddScoped<IYourService, YourUseCase>();
```

---

## Pattern 2: Query.Api (Controller + Facade)

Query.Api uses **facades** to orchestrate infrastructure; controllers stay thin.

### 1. Add request/response models in `Application/`

Place in `QueryModels.cs` or a dedicated file (e.g. `YourFeatureModels.cs`):

```csharp
namespace Query.Api.Application
{
    public class YourRequest
    {
        public string Param { get; set; } = string.Empty;
    }

    public class YourResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}
```

### 2. Add method to Facade

`RagFacade` or a new facade orchestrates:

- `EmbeddingGeneratorService`
- `TranscriptionSegmentVectorSearchRepository`
- `GenerateAnswerUseCase`
- `IConfiguration`

### 3. Add controller action

Controller must:

- Inject the facade
- Validate input (e.g. `string.IsNullOrWhiteSpace`)
- Return `ActionResult<T>` or `IActionResult`
- Catch exceptions and return 500 with generic message

```csharp
[HttpPost("your-route")]
public async Task<ActionResult<YourResponse>> YourAction([FromBody] YourRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Param))
        return BadRequest("Param não pode estar vazio.");

    try
    {
        var response = await _ragFacade.YourMethodAsync(request);
        return Ok(response);
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Erro interno: {ex.Message}");
    }
}
```

### 4. Register in `Program.cs`

Facade and its dependencies are already registered; add new ones if needed:

```csharp
builder.Services.AddScoped<YourNewService>();
```

---

## Conventions

### Controller

- `[ApiController]` and `[Route("api/[controller]")]` on class
- Route params: `[HttpGet("{id}/status")]`, `[HttpPost]`
- Body: `[FromBody] RequestType request`
- Query: `[FromQuery] string? param`
- Always pass `CancellationToken ct` for async actions

### Validation

- Validate required fields in controller before calling service
- Return `BadRequest` with clear message or `ErrorResponseRequest`
- Use `NotFound` when entity is missing

### Error handling

- Upload.Api: `new ErrorResponseRequest("message")` for 4xx
- Do not expose internal details in 500 responses
- Log errors with `ILogger` where useful

### Layering (AGENTS.md)

- **Domain** → pure models, DTOs, enums
- **Application** → controllers, use cases, facades, interfaces
- **Infrastructure** → DbContext, repositories, AI, messaging

---

## Checklist

Before finishing, verify:

- [ ] DTOs/models in correct namespace and folder
- [ ] Interface extended (Upload) or facade extended (Query)
- [ ] Controller validates input and handles null/errors
- [ ] CancellationToken passed through async chain
- [ ] New services registered in `Program.cs` if needed
- [ ] No synchronous coupling between services (event-driven)
- [ ] Minimal, localized edits; no refactoring unrelated code
