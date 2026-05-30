using EbaySharp.Entities.Develop.KeyManagement.SigningKey;
using LocalDBConnections.StampDataDB.StampDataEntities;

namespace FeeBayConnectionTester.Extensions
{
    public static class SigningKeyExtensions
    {
        /// <summary>
        /// Convert EbaySharp API entity to database entity
        /// </summary>
        public static FeeBaySigningKeys ToFeeBaySigningKey(this SigningKey apiKey)
        {
            if (apiKey == null) return null;

            return new FeeBaySigningKeys
            {
                SigningKeyId = apiKey.SigningKeyId,
                SigningKeyCipher = apiKey.SigningKeyCipher,
                PublicKey = apiKey.PublicKey,
                PrivateKey = apiKey.PrivateKey,
                JWE = apiKey.JWE,
                CreationTime = apiKey.CreationTime,
                ExpirationTime = apiKey.ExpirationTime
            };
        }
       // var dt = DateTimeOffset.FromUnixTimeSeconds(1604750712).DateTime;

        /// <summary>
        /// Convert database entity back to EbaySharp API entity
        /// </summary>
        public static SigningKey ToSigningKey(this FeeBaySigningKeys dbKey)
        {
            if (dbKey == null) return null;

            return new SigningKey
            {
                SigningKeyId = dbKey.SigningKeyId,
                SigningKeyCipher = dbKey.SigningKeyCipher,
                PublicKey = dbKey.PublicKey,
                PrivateKey = dbKey.PrivateKey,
                JWE = dbKey.JWE,
                CreationTime = dbKey.CreationTime,
                ExpirationTime = dbKey.ExpirationTime
            };
        }
    }
}
