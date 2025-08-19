# Resume Screening POC

Minimal path to score one resume against one job description using embeddings. This mirrors the future Greenhouse flow but uses a test-only endpoint and local Docker.

## Stack

* ASP.NET Core minimal API
* n8n for orchestration
* OpenAI Embeddings API (`text-embedding-3-small`)
* Docker Compose for local dev
* Qdrant optional later for vector search

## Prerequisites

* Docker and Docker Compose
* .NET 8 SDK (only if you want to run the API outside Docker)
* OpenAI API key

## Repo layout (suggested)

```
/                      # repo root
  docker-compose.yml
  .env                 # secrets and env values
  api/
    Program.cs
    App_Data/
      test-resumes/
        123.txt
  n8n/
    (empty; runtime data stored in a volume)
```

## Environment

Create a `.env` file in the repo root:

```
OPENAI_API_KEY=sk-...
ASPNETCORE_ENVIRONMENT=Development
```

## Docker Compose

```yaml
version: "3.8"
services:
  api:
    build: ./api
    container_name: resume-api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
    volumes:
      - ./api/App_Data:/app/App_Data

  n8n:
    image: n8nio/n8n:latest
    container_name: n8n
    ports:
      - "5678:5678"
    environment:
      - N8N_HOST=localhost
      - N8N_PROTOCOL=http
      - N8N_PORT=5678
      - OPENAI_API_KEY=${OPENAI_API_KEY}
    volumes:
      - n8n_data:/home/node/.n8n

  # qdrant:               # optional, for later top-k search
  #   image: qdrant/qdrant:latest
  #   container_name: qdrant
  #   ports:
  #     - "6333:6333"
  #   volumes:
  #     - qdrant_storage:/qdrant/storage

volumes:
  n8n_data:
  # qdrant_storage:
```

## Minimal API

`api/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = WebApplication.Create(builder);

// Test-only endpoint to mimic ATS (returns resume text from a local file)
app.MapGet("/test/greenhouse/candidates/{id}/resume", async (string id) =>
{
    var path = Path.Combine(AppContext.BaseDirectory, "App_Data", "test-resumes", $"{id}.txt");
    if (!System.IO.File.Exists(path)) return Results.NotFound();
    var text = await System.IO.File.ReadAllTextAsync(path);
    return Results.Ok(new { candidateId = id, resumeText = text });
}).WithName("GetTestResume").Produces(200).Produces(404);

app.Run();
```

Place a sample file at `api/App_Data/test-resumes/123.txt`.

## Run

```bash
docker compose up -d --build
# n8n: http://localhost:5678
# API: http://localhost:5000/test/greenhouse/candidates/123/resume
```

## n8n workflow (single resume)

1. **Manual Trigger**.
2. **HTTP Request** node "Get Resume".

   * Method: GET
   * URL inside Docker: `http://resume-api:8080/test/greenhouse/candidates/123/resume`
   * URL from host (for quick checks): `http://localhost:5000/test/greenhouse/candidates/123/resume`
3. **HTTP Request** node "Embed Resume".

   * Method: POST
   * URL: `https://api.openai.com/v1/embeddings`
   * Headers: `Authorization: Bearer {{$env.OPENAI_API_KEY}}`, `Content-Type: application/json`
   * Body (JSON):

     ```json
     {
       "model": "text-embedding-3-small",
       "input": "={{$json[\"resumeText\"]}}"
     }
     ```
4. (Optional) Add a Set node with a hardcoded `jobText`, call embeddings again, and compute cosine similarity in a small Function node.

## Testing

* Hit the API test endpoint in a browser to confirm it serves the file.
* Run the n8n workflow and check `data[0].embedding` and `usage` in the response.

## Notes

* File uploads are not wired yet. Current flow reads text from fixtures.
* The earlier temporary `/embed` endpoint used during early testing has been removed.
* Route and config names can be finalized after team discussion.
* Qdrant is optional. Skip it for the single resume path. Add it when storing many embeddings and doing top-k search.

## Next steps

* Replace the fixture call with the real ATS call when ready.
* Add job description input and cosine similarity scoring.
* Persist scores to a DB or sheet.
* Add basic error handling and limits.
