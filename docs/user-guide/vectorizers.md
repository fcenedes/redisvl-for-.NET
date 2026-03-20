# Vectorizers

Vectorizers convert text into embedding vectors for similarity search. All vectorizers implement `ITextVectorizer` and extend `BaseTextVectorizer`.

## Common Interface

```csharp
public interface ITextVectorizer
{
    string Model { get; }
    int Dims { get; }
    Task<float[]> EmbedAsync(string text, string? inputType = null);
    Task<IList<float[]>> EmbedManyAsync(IList<string> texts, string? inputType = null);
}
```

## Available Vectorizers

### OpenAI

```csharp
var vectorizer = new OpenAITextVectorizer(
    model: "text-embedding-3-small",   // default
    apiKey: "sk-...",                   // or OPENAI_API_KEY env var
    dims: 1536                          // optional
);
```

| Parameter | Default | Env Var |
|-----------|---------|---------|
| `model` | `"text-embedding-3-small"` | — |
| `apiKey` | — | `OPENAI_API_KEY` |
| `apiUrl` | OpenAI API | — |

### Azure OpenAI

```csharp
var vectorizer = new AzureOpenAITextVectorizer(
    deploymentName: "my-embedding-deployment",
    resourceName: "my-resource",        // {name}.openai.azure.com
    apiKey: "...",                       // or AZURE_OPENAI_API_KEY env var
    apiVersion: "2024-02-01"            // optional
);
```

| Parameter | Default | Env Var |
|-----------|---------|---------|
| `deploymentName` | — | — |
| `resourceName` | — | `AZURE_OPENAI_RESOURCE_NAME` |
| `apiKey` | — | `AZURE_OPENAI_API_KEY` |
| `apiVersion` | `"2024-02-01"` | — |

### Cohere

```csharp
var vectorizer = new CohereTextVectorizer(
    model: "embed-english-v3.0",        // default
    apiKey: "...",                       // or COHERE_API_KEY env var
    inputType: "search_document"        // or "search_query"
);
```

| Parameter | Default | Env Var |
|-----------|---------|---------|
| `model` | `"embed-english-v3.0"` | — |
| `apiKey` | — | `COHERE_API_KEY` |
| `inputType` | `"search_document"` | — |

### HuggingFace

```csharp
var vectorizer = new HuggingFaceTextVectorizer(
    model: "sentence-transformers/all-MiniLM-L6-v2",  // default
    apiKey: "hf_...",                                   // or HUGGINGFACE_API_KEY env var
);
```

| Parameter | Default | Env Var |
|-----------|---------|---------|
| `model` | `"sentence-transformers/all-MiniLM-L6-v2"` | — |
| `apiKey` | — | `HUGGINGFACE_API_KEY` |

### Vertex AI (Google)

```csharp
var vectorizer = new VertexAITextVectorizer(
    model: "textembedding-gecko",       // default
    apiKey: "...",                       // or GOOGLE_API_KEY env var
    dims: 768                           // optional
);
```

Uses Google's Generative Language API. Each text is embedded individually.

| Parameter | Default | Env Var |
|-----------|---------|---------|
| `model` | `"textembedding-gecko"` | — |
| `apiKey` | — | `GOOGLE_API_KEY` |

### Mistral

```csharp
var vectorizer = new MistralTextVectorizer(
    model: "mistral-embed",            // default
    apiKey: "...",                      // or MISTRAL_API_KEY env var
    dims: 1024                         // optional
);
```

Uses the same request/response format as OpenAI.

| Parameter | Default | Env Var |
|-----------|---------|---------|
| `model` | `"mistral-embed"` | — |
| `apiKey` | — | `MISTRAL_API_KEY` |

### VoyageAI

```csharp
var vectorizer = new VoyageAITextVectorizer(
    model: "voyage-3-large",           // default
    apiKey: "...",                      // or VOYAGE_API_KEY env var
    dims: 1024                         // optional
);

// With input type
var embedding = await vectorizer.EmbedAsync("text", inputType: "document");
```

Supports `input_type` parameter (`"document"`, `"query"`).

| Parameter | Default | Env Var |
|-----------|---------|---------|
| `model` | `"voyage-3-large"` | — |
| `apiKey` | — | `VOYAGE_API_KEY` |

### Custom Vectorizer

Bring your own embedding function:

```csharp
var vectorizer = new CustomTextVectorizer(
    embedFunc: async text =>
    {
        // Your embedding logic here
        return new float[] { 0.1f, 0.2f, 0.3f };
    },
    dims: 3,
    model: "my-model"
);
```

Useful for testing with deterministic fake embeddings:

```csharp
var fakeVectorizer = new CustomTextVectorizer(
    embedFunc: text =>
    {
        var hash = text.GetHashCode();
        var emb = new float[8];
        for (int i = 0; i < 8; i++)
            emb[i] = ((hash >> i) & 1) == 1 ? 1f : -1f;
        return Task.FromResult(emb);
    },
    dims: 8,
    model: "fake-test"
);
```

## Batch Embedding

All vectorizers support batch embedding via `EmbedManyAsync`:

```csharp
var texts = new[] { "first document", "second document", "third document" };
IList<float[]> embeddings = await vectorizer.EmbedManyAsync(texts);
```

> **Note**: Some providers (Vertex AI) embed one text per API call. Others (OpenAI, Mistral, VoyageAI) batch all texts in a single request.

