using System.Collections.Generic;
using Newtonsoft.Json;

namespace NathanAlden.StenographyPractice
{
    public class DictionaryModel
    {
        [JsonProperty("lessons")]
        public IEnumerable<LessonModel> Lessons { get; set; }
    }
}