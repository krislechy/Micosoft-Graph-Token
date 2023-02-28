using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using PemUtils;
using JWT.Algorithms;
using JWT;
using JWT.Serializers;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Yolva.MicrosoftServices
{
    public interface IJwtTokenRequest
    {
        string CreateToken(string pemPublicKey, string pemPrivateKey);
    }
    public class JwtTokenRequest: IJwtTokenRequest
    {
        private long createdDt { get => dateTimeToUnix(DateTime.Now); }

        private readonly Dictionary<string, object> header;
        private readonly Dictionary<string, object> payload;

        public JwtTokenRequest(
            DateTime expirationDt,
            DateTime whenStartDt,
            Guid tenantId,
            Guid clientId,
            string thumprint
            )
        {
            header = new Dictionary<string, object>
            {
                //{ "alg", "RS256" },
                { "typ", "JWT" },
                { "x5t", Convert.ToBase64String(HexStringToHex(thumprint)) },
            };

            payload = new Dictionary<string, object>
            {
                { "aud", $"https://login.microsoftonline.com/{tenantId}/oauth2/V2.0/token" },
                { "exp", dateTimeToUnix(expirationDt) },
                { "iss", clientId },
                { "jti", Guid.NewGuid() },
                { "nbf", dateTimeToUnix(whenStartDt) },
                { "sub", clientId },
                { "iat", createdDt }
            };
        }
        public string CreateToken(string pemPublicKey,string pemPrivateKey)
        {
            IJwtAlgorithm algorithm = new RS256Algorithm(getRSA(pemPublicKey), getRSA(pemPrivateKey));
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            var token = encoder.Encode(header, payload, getKey(Guid.NewGuid()));
            return token;
        }
        private byte[] getKey(Guid guid)=> Encoding.UTF8.GetBytes(Convert.ToBase64String(guid.ToByteArray()));
        private long dateTimeToUnix(DateTime dt)=> ((DateTimeOffset)dt).ToUnixTimeSeconds();

        private byte[] HexStringToHex(string inputHex)
        {
            var resultantArray = new byte[inputHex.Length / 2];
            for (var i = 0; i < resultantArray.Length; i++)
            {
                resultantArray[i] = System.Convert.ToByte(inputHex.Substring(i * 2, 2), 16);
            }
            return resultantArray;
        }

        private RSA getRSA(string key)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(key.ToCharArray());
            return rsa;
        }
    }
}