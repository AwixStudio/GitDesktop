using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            string url = $"https://github.com/{owner}/{repositoryName}/compare/{targetBranch}...{sourceBranch}?expand=1";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
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
