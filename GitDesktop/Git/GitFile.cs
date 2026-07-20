using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    internal class GitFile
    {
        public required string Path { get; init; }

        public GitFileState IndexState { get; init; }
        public GitFileState WorkingTreeState { get; init; }
    }
}
