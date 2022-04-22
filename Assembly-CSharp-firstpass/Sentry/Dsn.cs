using System;

namespace Sentry
{
    public class Dsn
    {
        private Uri _uri;

        public Uri callUri;

        public string secretKey;

        public string publicKey;

        public Dsn(string dsn)
        {
            if (dsn == "")
            {
                throw new ArgumentException("invalid argument - DSN cannot be empty");
            }
            _uri = new Uri(dsn);
            if (string.IsNullOrEmpty(_uri.UserInfo))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            string[] array = _uri.UserInfo.Split(':');
            publicKey = array[0];
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            secretKey = null;
            if (array.Length > 1)
            {
                secretKey = array[1];
            }
            string arg = _uri.AbsolutePath.Substring(0, _uri.AbsolutePath.LastIndexOf('/'));
            string text = _uri.AbsoluteUri.Substring(_uri.AbsoluteUri.LastIndexOf('/') + 1);
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Invalid DSN: A Project Id is required.");
            }
            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = _uri.Scheme,
                Host = _uri.DnsSafeHost,
                Port = _uri.Port,
                Path = $"{arg}/api/{text}/store/"
            };
            callUri = uriBuilder.Uri;
        }
    }
}
