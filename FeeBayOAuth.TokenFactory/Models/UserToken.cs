using System;
using System.Collections.Generic;
using System.Text;

namespace FeeBayOAuth.TokenFactory.Models
{
    internal class UserToken
    {
        
        internal string AccessToken { get; set; } = "";

        internal DateTime ExpiresUtc { get; set; }

        internal bool IsValid =>
                DateTime.UtcNow < ExpiresUtc;
        internal bool ExpiresSoon => ExpiresUtc < DateTime.UtcNow.AddMinutes(5);
    }
}
