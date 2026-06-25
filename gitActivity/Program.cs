using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gitActivity
{

    public record Actor(
        int id, 
        string login, 
        string display_login, 
        string gravatar_id, 
        string url, 
        string avatar_url
    );

    public record Repo(
        int id, 
        string name, 
        string url
    );

    public record Org(
        int id, 
        string login, 
        string gravatar_id, 
        string url, 
        string avatar_url
    );

    public record Event(
        string id,
        string type, 
        Actor actor, 
        Repo repo, 
        JsonElement? payload,
        [property: JsonPropertyName("public")] bool IsPublic, // mapping reserved word
        string created_at, 
        Org? org
    );

    class Program
    {
        private static HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("One argument required: dotnet run -- <username> ");
                return;
            }

            var username = args[0].Trim();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GitActivity-CLI"); // Git requires a git agent otherwise throws 403 Forbidden

            try
            {
                var events = await GetGitActivityAsync(username);
                foreach (var gitEvent in events) {
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Other error occured: {e} {e.Message}");
            }
        }

        static async Task<List<Event>?> GetGitActivityAsync(string username)
        {
            var url = $"https://api.github.com/users/{username}/events";

            using HttpResponseMessage response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"User: {username} could not be found.");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            List<Event>? events = JsonSerializer.Deserialize<List<Event>>(jsonResponse);
            if (events.Count == 0)
            {
                Console.WriteLine($"{username} has no recent events");
                return null;
            }

            return events;
        }
    }
}
