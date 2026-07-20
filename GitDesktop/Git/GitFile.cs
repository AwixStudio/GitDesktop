using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    public class GitFile
    {
        public required string Path { get; init; }
        public GitFileState IndexState { get; init; }
        public GitFileState WorkingTreeState { get; init; }
        public bool MarkedForCommit { get; set; }

        // Cache for diff - lazy loaded on demand
        private string? cachedDiff;
        private bool diffLoaded;

        public string GetCachedDiff()
        {
            return cachedDiff ?? "Diff not loaded";
        }

        public void SetCachedDiff(string diff)
        {
            cachedDiff = diff;
            diffLoaded = true;
        }

        public bool IsDiffLoaded => diffLoaded;
    }
}

