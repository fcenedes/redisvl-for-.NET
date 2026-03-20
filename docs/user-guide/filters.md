# Filter Expressions

Filters narrow search results without affecting scoring. They can be combined with boolean operators and used with any query type.

## Filter Types

### Tag Filter

Exact-match filtering on tag fields.

```csharp
// Equals
var filter = Tag.Field("category") == "electronics";
// → @category:{electronics}

// Not equals
var filter = Tag.Field("status") != "deleted";
// → -@status:{deleted}

// IN (multiple values)
var filter = Tag.Field("role").In("user", "admin", "moderator");
// → @role:{user|admin|moderator}
```

### Numeric Filter

Range and comparison filtering on numeric fields.

```csharp
// Comparisons
var filter = Num.Field("price") > 10;
var filter = Num.Field("price") >= 10;
var filter = Num.Field("price") < 100;
var filter = Num.Field("price") <= 100;
var filter = Num.Field("price") == 50;
var filter = Num.Field("price") != 50;

// Range (inclusive)
var filter = Num.Field("price").Between(10, 100);
// → @price:[10 100]
```

### Timestamp Filter

Date/time filtering that auto-converts to Unix timestamps.

```csharp
// DateTime comparisons
var filter = Timestamp.Field("created_at") >= new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
// → @created_at:[1704067200 +inf]

var filter = Timestamp.Field("updated_at") < DateTime.UtcNow;

// DateTimeOffset
var filter = Timestamp.Field("ts") == new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

// Unix timestamp (long)
var filter = Timestamp.Field("ts") > 1718457600L;

// Date-only equality matches the entire day
var filter = Timestamp.Field("created_at") == new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
// → @created_at:[1718409600 1718495999]  (start-of-day to end-of-day)

// Range
var filter = Timestamp.Field("ts").Between(
    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc)
);
```

### Geo Filter

Geographic radius filtering.

```csharp
var filter = Geo.Field("location").WithinRadius(
    longitude: -73.935242,
    latitude: 40.730610,
    radius: 10,
    unit: GeoUnit.Kilometers
);
// → @location:[-73.935242 40.73061 10 km]
```

### Text Filter

Full-text matching within a filter context.

```csharp
var filter = Text.Field("description").Match("fast database");
// → @description:(fast database)
```

## Combining Filters

Use `&` (AND), `|` (OR), and `~` (NOT) to compose filters:

```csharp
// AND
var filter = (Tag.Field("category") == "electronics") & (Num.Field("price") < 100);
// → (@category:{electronics} @price:[-inf (100])

// OR
var filter = (Tag.Field("brand") == "Apple") | (Tag.Field("brand") == "Samsung");
// → (@brand:{Apple} | @brand:{Samsung})

// NOT
var filter = ~(Tag.Field("status") == "deleted");
// → -(@status:{deleted})

// Complex
var filter = (Tag.Field("category") == "laptop")
    & (Num.Field("price").Between(500, 2000))
    & ~(Tag.Field("status") == "discontinued")
    & (Timestamp.Field("listed_at") >= DateTime.UtcNow.AddDays(-30));
```

## Using Filters with Queries

Every query type accepts a `FilterExpression`:

```csharp
// Vector query with filter
var vq = new VectorQuery(vector, "embedding", 10)
{
    FilterExpression = Tag.Field("category") == "tech"
};

// Text query with filter
var tq = new TextQuery("redis", "content")
{
    FilterExpression = Num.Field("year") >= 2024
};

// Count with filter
var count = await index.CountAsync(new CountQuery(
    Tag.Field("status") == "active"
));

// Filter-only query
var fq = new FilterQuery(Num.Field("price") < 50)
{
    SortBy = "price",
    SortAscending = true
};
```

## Filter Query String Reference

| Expression | Redis Query String |
|---|---|
| `Tag.Field("f") == "v"` | `@f:{v}` |
| `Tag.Field("f") != "v"` | `-@f:{v}` |
| `Tag.Field("f").In("a","b")` | `@f:{a\|b}` |
| `Num.Field("f") == 5` | `@f:[5 5]` |
| `Num.Field("f") != 5` | `-@f:[5 5]` |
| `Num.Field("f") > 5` | `@f:[(5 +inf]` |
| `Num.Field("f") >= 5` | `@f:[5 +inf]` |
| `Num.Field("f") < 5` | `@f:[-inf (5]` |
| `Num.Field("f") <= 5` | `@f:[-inf 5]` |
| `Num.Field("f").Between(1,10)` | `@f:[1 10]` |
| `Timestamp.Field("f") > dt` | `@f:[(unix +inf]` |
| `Geo.Field("f").WithinRadius(...)` | `@f:[lon lat r unit]` |
| `Text.Field("f").Match("x")` | `@f:(x)` |
| `a & b` | `(a b)` |
| `a \| b` | `(a \| b)` |
| `~a` | `-(a)` |

