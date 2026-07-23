using System.Collections.Generic;

namespace GitDesktop.Git
{
    /// <summary>
    /// Represents the result of a merge operation with conflicts
    /// </summary>
    public class MergeConflictResult
    {
        /// <summary>
        /// List of files that have merge conflicts
        /// </summary>
        public List<ConflictedFile> ConflictedFiles { get; set; } = [];

        /// <summary>
        /// True if the merge operation resulted in conflicts
        /// </summary>
        public bool HasConflicts => ConflictedFiles.Count > 0;

        /// <summary>
        /// Error message from git merge, if any
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a single file with merge conflicts
    /// </summary>
    public class ConflictedFile
    {
        /// <summary>
        /// Path to the conflicted file
        /// </summary>
        public required string Path { get; init; }

        /// <summary>
        /// User's chosen resolution: "ours" for local version, "theirs" for incoming version, null if unresolved
        /// </summary>
        public string? Resolution { get; set; }
    }
}
