namespace RedisVL.Exceptions;

/// <summary>
/// Base exception for all RedisVL errors.
/// </summary>
public class RedisVLException : Exception
{
    public RedisVLException() { }
    
    public RedisVLException(string message) : base(message) { }
    
    public RedisVLException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when schema validation fails.
/// </summary>
public class SchemaValidationException : RedisVLException
{
    public SchemaValidationException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when index operations fail.
/// </summary>
public class IndexException : RedisVLException
{
    public IndexException(string message) : base(message) { }
    public IndexException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when vectorization fails.
/// </summary>
public class VectorizationException : RedisVLException
{
    public VectorizationException(string message) : base(message) { }
    public VectorizationException(string message, Exception innerException) 
        : base(message, innerException) { }
}
