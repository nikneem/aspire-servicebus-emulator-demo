using System.Text.Json;

namespace HexMaster.AspireDemo.WebApi.Messages;

public static class MessageExtensions
{
    public static string SerializeToJson<T>(this T message) where T : class
    {
        return JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
}