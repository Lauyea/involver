using System.Text.Json;

namespace Involver.Common;

public static class JsonConfig
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
