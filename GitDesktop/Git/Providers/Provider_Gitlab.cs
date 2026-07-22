using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git.Providers
{
    internal class Provider_Gitlab : IRepositoryProvider
    {
        public void CreatePullRequest(string repositoryPath, string title, string sourceBranch, string targetBranch)
        {
            throw new NotImplementedException();
        }
    }
}
