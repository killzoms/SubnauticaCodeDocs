using Oculus.Newtonsoft.Json;

namespace AssemblyCSharp.Oculus.Platform.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserProof
    {
        [JsonProperty("nonce")]
        private string _Nonce;

        public string Value => _Nonce;
    }
}
