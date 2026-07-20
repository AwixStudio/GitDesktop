namespace GitDesktop.Git
{
    public class GitCommit
    {
        public string Hash { get; set; }
        public string Author { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public string ParentHash { get; set; }

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
