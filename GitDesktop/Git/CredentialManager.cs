using System;
using System.Collections.Generic;
using System.Text;

namespace GitDesktop.Git
{
    public static class CredentialManager
    {
        private static string username = "";
        private static string password = "";

        public static void StoreCredentials(string _username, string _password)
        {
            username = _username;
            password = _password;
        }

        public static (string username, string password) RetrieveCredentials()
        {                       
            return (username, password);
        }
    }
}
