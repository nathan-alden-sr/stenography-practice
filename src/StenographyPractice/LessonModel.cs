using System.Collections.Generic;
using Newtonsoft.Json;

namespace NathanAlden.StenographyPractice
{
    public class LessonModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("terms")]
        public IEnumerable<TermModel> Terms { get; set; }
    }
}