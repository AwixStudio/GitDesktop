using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitDesktop.Git.Providers
{
    internal class Provider_Github : IRepositoryProvider
    {
        private readonly string repositoryName;
        private readonly string owner;

        internal Provider_Github(string repositoryName, string owner)
        {
            this.repositoryName = repositoryName;
            this.owner = owner;
        }

        public void CreatePullRequest(string repositoryPath, string title, string sourceBranch, string targetBranch)
        {
            Task<string?> task = Task.Run(async () =>
            {
                using HttpClient client = new();

                string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("GitGui");

                CreatePullRequestRequest request = new()
                {
                    Title = title,
                    Head = sourceBranch,
                    Base = targetBranch,
                    Body = ""
                };

                StringContent content = new(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response =
                    await client.PostAsync(
                        $"https://api.github.com/repos/{owner}/{repositoryName}/pulls",
                        content);

                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception(json);

                PullRequestResponse? pr = JsonSerializer.Deserialize<PullRequestResponse>(json);

                return pr?.Url;
            });
        }

        internal class CreatePullRequestRequest
        {
            [JsonPropertyName("title")]
            public required string Title { get; set; }

            [JsonPropertyName("head")]
            public required string Head { get; set; }

            [JsonPropertyName("base")]
            public required string Base { get; set; }

            [JsonPropertyName("body")]
            public required string Body { get; set; }
        }

        internal class PullRequestResponse
        {
            [JsonPropertyName("html_url")]
            public required string Url { get; set; }
        }
    }
}
