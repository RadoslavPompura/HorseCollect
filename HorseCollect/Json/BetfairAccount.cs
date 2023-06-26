using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HorseCollect.Json
{
    public class BetfairAccount
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public BetfairAccount(string _username, string _password)
        {
            this.UserName = _username;
            this.Password = _password;
        }
    }
}