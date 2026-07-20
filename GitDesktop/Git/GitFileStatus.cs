using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    public enum GitFileState
    {
        Unmodified,
        Modified,
        Added,
        Deleted,
        Renamed,
        Copied,
        Untracked,
        Ignored,
        Conflicted
    }

    internal static class GitFileStatus
    {
        public static GitFileState ParseState(char c)
        {
            return c switch
            {
                ' ' => GitFileState.Unmodified,
                '?' => GitFileState.Untracked,
                'M' => GitFileState.Modified,
                'A' => GitFileState.Added,
                'D' => GitFileState.Deleted,
                'R' => GitFileState.Renamed,
                'C' => GitFileState.Copied,
                'U' => GitFileState.Conflicted,
                '!' => GitFileState.Ignored,

                _ => throw new NotSupportedException($"Unknown git state '{c}'")
            };
        }
    }
}
