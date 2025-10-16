using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AiLaTrieuPhu.Models
{
    public class Question
    {
        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; }

        [JsonPropertyName("options")]
        public List<string> Options { get; set; }

        [JsonPropertyName("correctAnswerIndex")]
        public int CorrectAnswerIndex { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }

        // Thuộc tính để hỗ trợ logic 50/50 trong game
        [JsonIgnore]
        public List<bool> IsOptionHidden { get; set; } = new List<bool> { false, false, false, false };
    }
}
