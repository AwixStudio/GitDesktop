using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git.Providers
{
    /// <summary>
    /// Provider for local Git repositories without remote.
    /// </summary>
    internal class Provider_Local : IRepositoryProvider
    {
        public void CreatePullRequest(string repositoryPath, string title, string sourceBranch, string targetBranch)
        {
            throw new NotSupportedException("Pull requests are not supported for local repositories without a remote.");
        }
    }
}
