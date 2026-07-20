using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    internal class GitBranch
    {
        internal string Name { get; private set; }

        internal GitBranch(string name)
        {
            Name = name;
        }
    }
}
