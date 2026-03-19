namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Text vectorizer that delegates to user-provided embedding functions.
/// Allows plugging in custom embedding logic without depending on external APIs.
/// </summary>
public class CustomTextVectorizer : ITextVectorizer
{
    private readonly Func<string, Task<float[]>> _embedFunc;
    private readonly Func<IList<string>, Task<IList<float[]>>>? _embedManyFunc;

    /// <inheritdoc />
    public string Model { get; }

    /// <inheritdoc />
    public int Dims { get; }

    /// <summary>
    /// Creates a custom text vectorizer with user-provided embedding functions.
    /// </summary>
    /// <param name="embedFunc">Function to embed a single text string.</param>
    /// <param name="dims">The embedding dimensions produced by the function.</param>
    /// <param name="model">Model name identifier. Default: "custom".</param>
    /// <param name="embedManyFunc">
    /// Optional function to embed multiple texts at once. If not provided,
    /// <paramref name="embedFunc"/> will be called for each text individually.
    /// </param>
    public CustomTextVectorizer(
        Func<string, Task<float[]>> embedFunc,
        int dims,
        string model = "custom",
        Func<IList<string>, Task<IList<float[]>>>? embedManyFunc = null)
    {
        _embedFunc = embedFunc ?? throw new ArgumentNullException(nameof(embedFunc));
        Dims = dims > 0 ? dims : throw new ArgumentOutOfRangeException(nameof(dims), "Dims must be positive.");
        Model = model ?? "custom";
        _embedManyFunc = embedManyFunc;
    }

    /// <inheritdoc />
    public Task<float[]> EmbedAsync(string text, string? inputType = null)
    {
        return _embedFunc(text);
    }

    /// <inheritdoc />
    public async Task<IList<float[]>> EmbedManyAsync(IList<string> texts, string? inputType = null)
    {
        if (_embedManyFunc != null)
        {
            return await _embedManyFunc(texts);
        }

        // Fallback: call embedFunc for each text individually
        var results = new float[texts.Count][];
        for (int i = 0; i < texts.Count; i++)
        {
            results[i] = await _embedFunc(texts[i]);
        }

        return results;
    }
}

