using Newtonsoft.Json;

namespace NathanAlden.StenographyPractice
{
    public class TermModel
    {
        [JsonProperty("steno")]
        public string Steno { get; set; }

        [JsonProperty("english")]
        public string English { get; set; }
    }
}