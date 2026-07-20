namespace GitDesktop.Git
{
    public class GitCommit
    {
        public string Hash { get; private set; }
        public string Author { get; private set; }
        public DateTime Date { get; private set; }
        public string Message { get; private set; }
        public string ParentHash { get; private set; }

        public GitCommit(string hash, string author, DateTime date, string message, string parentHash = "")
        {
            Hash = hash;
            Author = author;
            Date = date;
            Message = message;
            ParentHash = parentHash;
        }
    }
}
