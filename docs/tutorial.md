# Interactive Tutorial App

RedisVL for .NET includes an interactive desktop tutorial built with [Avalonia UI](https://avaloniaui.net/). It demonstrates the core extensions — Semantic Cache, Embeddings Cache, and Message History — with a live Redis connection.

> **Source code**: [`samples/RedisVL.Tutorial/`](https://github.com/fcenedes/redisvl-for-.NET/tree/main/samples/RedisVL.Tutorial)

## Prerequisites

- .NET 10 SDK
- Redis 8+ running on `localhost:6379`

```bash
# Start Redis (Docker)
docker run -d --name redis-stack -p 6379:6379 redis/redis-stack-server:latest
```

## Running the Tutorial

```bash
cd samples/RedisVL.Tutorial
dotnet run
```

This launches a desktop window with three tabs:

---

## Tab 1: Semantic Cache

Demonstrates caching LLM responses by semantic similarity using `SemanticCache`.

### What it does

1. **Store** — Enter a prompt and response, click **Store** to cache it
2. **Check** — Enter a query prompt, click **Check** to find semantically similar cached responses
3. **Clear** — Remove all cached entries

### How it works

The demo uses a `CustomTextVectorizer` with SHA-256 hash-based embeddings (64 dimensions). This produces deterministic embeddings without needing an API key — identical inputs always produce identical vectors.

```csharp
var vectorizer = new CustomTextVectorizer(
    embedFunc: text =>
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToLowerInvariant().Trim()));
        var floats = new float[64];
        for (int i = 0; i < 64; i++)
            floats[i] = (hash[i % hash.Length] - 128f) / 128f;
        return Task.FromResult(floats);
    },
    dims: 64,
    model: "demo-hash-vectorizer");

var cache = new SemanticCache(
    name: "tutorial-cache",
    vectorizer: vectorizer,
    distanceThreshold: 0.3);
```

### Try it

1. Store: prompt = `What is Redis?`, response = `Redis is an in-memory data store`
2. Check: query = `What is Redis?` → **Cache HIT** (exact match, distance ≈ 0)
3. Check: query = `Tell me about Redis` → **Cache MISS** (hash-based embeddings don't capture semantic similarity — swap in a real vectorizer like OpenAI to see semantic matching)

---

## Tab 2: Embeddings Cache

Demonstrates caching raw embedding vectors using `EmbeddingsCache`.

### What it does

1. **Set** — Cache an embedding for a text + model pair
2. **Get** — Retrieve a cached embedding
3. **Exists** — Check if an embedding is cached
4. **Drop** — Remove a cached embedding
5. **MSet / MGet** — Batch operations (comma-separated texts)
6. **Clear** — Remove all cached embeddings

### How it works

```csharp
var cache = new EmbeddingsCache(prefix: "tutorial-emb");

// Set: generates a demo embedding via SHA-256 hash and caches it
await cache.SetAsync("hello world", "demo-model", embedding);

// Get: retrieves the cached embedding
var entry = await cache.GetAsync("hello world", "demo-model");
// entry.Text, entry.ModelName, entry.Embedding

// Batch: comma-separated input → MSet/MGet
await cache.MSetAsync(texts, "demo-model", embeddings);
var results = await cache.MGetAsync(texts, "demo-model");
```

### Try it

1. Set text = `hello world`, model = `demo-model` → stores 32-dim embedding
2. Get text = `hello world` → shows dims and first 5 values
3. Batch: enter `apple, banana, cherry` → MSet stores 3 embeddings, MGet retrieves them

---

## Tab 3: Message History

Demonstrates conversation history with semantic search using `SemanticMessageHistory`.

### What it does

1. **Add** — Add a message with a role (`user`, `llm`, or `system`)
2. **Recent** — Get the 5 most recent messages
3. **Search** — Semantic search over message history
4. **Clear** — Remove all messages

### How it works

```csharp
var history = new SemanticMessageHistory(
    name: "tutorial-history",
    vectorizer: vectorizer,       // same hash-based vectorizer
    distanceThreshold: 0.9);

// Add messages
await history.AddMessagesAsync(new[]
{
    new Message { Role = "user", Content = "Tell me about Redis" }
});

// Get recent
var recent = await history.GetRecentAsync(topK: 5);

// Semantic search
var relevant = await history.GetRelevantAsync("database performance", topK: 5);
```

### Try it

1. Add several messages with different roles:
    - `[user]` "What is Redis?"
    - `[llm]` "Redis is an in-memory data store used as a database, cache, and message broker."
    - `[user]` "How fast is it?"
    - `[llm]` "Redis can handle millions of operations per second."
2. Click **Recent** → see messages in chronological order
3. Search for `What is Redis?` → finds the exact message (hash-based matching)

---

## Architecture

The tutorial follows the MVVM pattern with Avalonia UI:

```
samples/RedisVL.Tutorial/
├── Program.cs                          # Entry point
├── App.axaml / App.axaml.cs            # Avalonia app setup
├── MainWindow.axaml / .cs              # Tab container
├── ViewModels/
│   └── MainWindowViewModel.cs          # Root view model (Redis URL, status)
└── Views/
    ├── SemanticCacheDemo.axaml / .cs    # Semantic Cache tab
    ├── EmbeddingsCacheDemo.axaml / .cs  # Embeddings Cache tab
    └── MessageHistoryDemo.axaml / .cs   # Message History tab
```

### Key design decisions

- **No API keys required** — Uses `CustomTextVectorizer` with SHA-256 hashing instead of a real embedding API. This means the tutorial works offline (with Redis) and produces deterministic results.
- **Hash-based embeddings** — The demo vectorizer doesn't capture semantic similarity (only exact/near-exact matches work). Replace with `OpenAITextVectorizer` or another provider to see real semantic matching.
- **Direct Redis connection** — Connects to `redis://localhost:6379` by default. No configuration needed beyond having Redis running.

## Swapping in a Real Vectorizer

To see true semantic similarity, replace the `CustomTextVectorizer` with a real one:

```csharp
// Instead of the hash-based demo vectorizer:
var vectorizer = new OpenAITextVectorizer(
    model: "text-embedding-3-small",
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
);
```

With a real vectorizer, "Tell me about Redis" would match "What is Redis?" even though the text is different.

