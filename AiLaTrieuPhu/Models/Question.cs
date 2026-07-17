using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    public partial class Question : ObservableObject
    {
        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; }

        [JsonPropertyName("options")]
        public List<string> Options { get; set; }

        [JsonPropertyName("correctAnswerIndex")]
        public int CorrectAnswerIndex { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [ObservableProperty]
        [property: JsonPropertyName("isOptionHidden")]
        private ObservableCollection<bool> isOptionHidden = new ObservableCollection<bool> { false, false, false, false };

        // Thuộc tính tiện ích để lấy đáp án đúng
        [JsonIgnore]
        public string CorrectAnswer => Options.Count > CorrectAnswerIndex ? Options[CorrectAnswerIndex] : null;
    }
}
