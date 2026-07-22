using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    public class GitBranch
    {
        public string Name { get; private set; }

        public GitBranch(string name)
        {
            Name = name;
        }
    }
}
