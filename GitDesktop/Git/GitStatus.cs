using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    internal class GitStatus
    {
        public List<GitFile> Files { get; } = [];

        public bool IsClean => Files.Count == 0;
    }
}
