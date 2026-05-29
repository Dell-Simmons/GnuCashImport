using System;
using System.Collections.Generic;
using System.Text;

namespace FeeBayOAuth.TokenService.Utilities
{
    /// <summary>
    /// Shared utility methods for eBay OAuth HTTP requests
    /// </summary>
    public static class OAuthHttpHelpers
    {
        /// <summary>
        /// Creates Basic Authorization header value from app ID and cert ID
        /// </summary>
        /// <param name="appId">eBay App ID (Client ID)</param>
        /// <param name="certId">eBay Cert ID (Client Secret)</param>
        /// <returns>Base64 encoded authorization header value</returns>
        public static string CreateAuthorizationHeader(string appId, string certId)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(appId).Append(":");
            stringBuilder.Append(certId);
            var plainTextBytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
            string encodedText = Convert.ToBase64String(plainTextBytes);
            return "Basic " + encodedText;
        }

        /// <summary>
        /// Creates URL-encoded request payload from dictionary of parameters
        /// </summary>
        /// <param name="payloadParams">Dictionary of key-value pairs to encode</param>
        /// <returns>URL-encoded payload string</returns>
        public static string CreateRequestPayload(Dictionary<string, string> payloadParams)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in payloadParams)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append(entry.Key).Append("=").Append(entry.Value);
            }
            return sb.ToString();
        }
    }
}
