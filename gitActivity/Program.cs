using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq.Expressions;
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

    class UserAction
    {
        private List<Event> _events;
        private Dictionary<string, int> _commits;
        private List<string> _starred;
        private List<string> _created;

        public UserAction(List<Event> events)
        {
            _events = events;

            _commits = new Dictionary<string, int>();
            _starred = new List<string>();
            _created = new List<string>();

            ParseEvents();
        }

        private void ParseEvents()
        {
            foreach (var userEvent in _events)
            {
                var key = userEvent.repo.name;
                switch (userEvent.type)
                {
                    case "PushEvent":
                        if (_commits.ContainsKey(userEvent.repo.name))
                        {
                            _commits[key] = (_commits.TryGetValue(key, out int c) ? c : 0) + 1;
                        }
                        else
                        {
                            _commits.Add(key, 1);
                        }
                        break;
                    case "CreateEvent":
                        _created.Add(key);
                        break;
                    case "WatchEvent":
                        _starred.Add(key);
                        break;
                    default:
                        break;
                }
            }
        }

        public override string ToString()
        {
            var commits = "";
            foreach (var (key, value) in _commits)
            {
                commits += $"   Pushed {value} commits to {key} \n";
            }

            var starred = "";
            foreach (var starredRepo in _starred)
            {
                starred += $"   Starred {starredRepo}\n";
            }

            var created = "";
            foreach (var createdRepo in _created)
            {
                created += $"   Created a new repo {createdRepo}\n";
            }
            return "Output:\n" + commits + starred + created;
        }
    }


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
                if (events == null)
                {
                    return;
                }
                var actions = new UserAction(events);
                Console.WriteLine(actions);
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
