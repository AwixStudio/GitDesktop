using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    internal interface IRepositoryProvider
    {
        void CreatePullRequest(string repositoryPath, string title, string sourceBranch, string targetBranch);
    }
}
