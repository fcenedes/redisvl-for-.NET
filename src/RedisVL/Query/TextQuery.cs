namespace RedisVL.Query;

/// <summary>
/// Full-text search query using BM25.
/// </summary>
public class TextQuery : BaseQuery
{
    /// <summary>
    /// The text to search for.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the text field to search.
    /// </summary>
    public string? TextFieldName { get; set; }
    
    /// <summary>
    /// Sort by field name.
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Sort in ascending order.
    /// </summary>
    public bool SortAscending { get; set; } = true;
    
    public TextQuery() { }
    
    public TextQuery(string text, string? textFieldName = null)
    {
        Text = text;
        TextFieldName = textFieldName;
    }
    
    public override string GetQueryString()
    {
        var filter = GetFilterString();
        var textSearch = string.IsNullOrEmpty(TextFieldName)
            ? Text
            : $"@{TextFieldName}:({Text})";
        
        return filter == "*" ? textSearch : $"({filter} {textSearch})";
    }
}
