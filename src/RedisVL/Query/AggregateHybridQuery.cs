using System.Globalization;
using RedisVL.Query.Filter;

namespace RedisVL.Query;

/// <summary>
/// Aggregate hybrid query combining text and vector search using FT.AGGREGATE.
/// Scores documents based on a weighted combination of text and vector similarity:
/// hybrid_score = alpha * vector_similarity + (1 - alpha) * text_score
/// </summary>
public class AggregateHybridQuery : BaseQuery
{
    /// <summary>
    /// The text to search for.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the text field.
    /// </summary>
    public string TextFieldName { get; set; } = "content";
    
    /// <summary>
    /// The query vector.
    /// </summary>
    public float[] Vector { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Name of the vector field.
    /// </summary>
    public string VectorFieldName { get; set; } = "embedding";
    
    /// <summary>
    /// Weight of vector similarity (0-1). 
    /// hybrid_score = alpha * vector_similarity + (1 - alpha) * text_score.
    /// </summary>
    public double Alpha { get; set; } = 0.7;
    
    /// <summary>
    /// Text scorer to use (e.g., "BM25STD", "TFIDF", "BM25", "DISMAX", "DOCSCORE").
    /// </summary>
    public string TextScorer { get; set; } = "BM25STD";
    
    /// <summary>
    /// Name for the distance field in results.
    /// </summary>
    public string DistanceFieldName { get; set; } = "vector_distance";
    
    /// <summary>
    /// Name for the vector parameter.
    /// </summary>
    public string VectorParamName { get; set; } = "vector";
    
    public AggregateHybridQuery() { }
    
    /// <summary>
    /// Creates an aggregate hybrid query.
    /// </summary>
    public AggregateHybridQuery(
        string text,
        string textFieldName,
        float[] vector,
        string vectorFieldName,
        double alpha = 0.7,
        string textScorer = "BM25STD")
    {
        Text = text;
        TextFieldName = textFieldName;
        Vector = vector;
        VectorFieldName = vectorFieldName;
        Alpha = alpha;
        TextScorer = textScorer;
    }
    
    /// <inheritdoc />
    public override string GetQueryString()
    {
        var filter = GetFilterString();
        
        // Build text search part
        string textQuery;
        if (filter == "*")
        {
            textQuery = $"@{TextFieldName}:({Text})";
        }
        else
        {
            textQuery = $"({filter} @{TextFieldName}:({Text}))";
        }
        
        // Build KNN part
        var knnQuery = $"KNN {NumResults} @{VectorFieldName} ${VectorParamName} AS {DistanceFieldName}";
        
        return $"{textQuery}=>[{knnQuery}]";
    }
    
    /// <summary>
    /// Gets the scoring expressions for the aggregate pipeline.
    /// </summary>
    public string GetScoringExpression()
    {
        var alphaStr = Alpha.ToString(CultureInfo.InvariantCulture);
        var textWeightStr = (1 - Alpha).ToString(CultureInfo.InvariantCulture);
        return $"{textWeightStr}*@text_score + {alphaStr}*@vector_similarity";
    }
    
    /// <summary>
    /// Gets the vector similarity transformation expression.
    /// </summary>
    public string GetVectorSimilarityExpression()
        => $"(2 - @{DistanceFieldName})/2";
    
    /// <summary>
    /// Gets the vector as a byte array for Redis.
    /// </summary>
    public byte[] GetVectorBytes()
    {
        var bytes = new byte[Vector.Length * sizeof(float)];
        Buffer.BlockCopy(Vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}

