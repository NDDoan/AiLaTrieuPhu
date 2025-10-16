using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;
using System.IO;
using System.Text.Json;

namespace AiLaTrieuPhu.Services
{
    public class QuestionDataService : IQuestionDataService
    {
        private const string BasePath = "Assets/Data";

        public async Task<List<Question>> GetRandomGameQuestionsAsync()
        {
            var gameQuestions = new List<Question>();

            // Dùng List of ValueTuple<(int countToPick, string filePath)>
            var filesToLoad = new List<(int countToPick, string filePath)>
            {
                (5, Path.Combine(BasePath, "questions_1_5.json")),
                (5, Path.Combine(BasePath, "questions_6_10.json")),
                (5, Path.Combine(BasePath, "questions_11_15.json"))
            };

            var random = new Random();

            foreach (var entry in filesToLoad)
            {
                int countToPick = entry.countToPick; // Lấy số lượng câu hỏi cần chọn
                string filePath = entry.filePath;     // Lấy đường dẫn file

                if (!File.Exists(filePath))
                {
                    // Trẫm khuyên nên dùng một cơ chế log lỗi ở đây
                    continue;
                }

                try
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    // Dùng Task.Run để Deserialize bất đồng bộ (giả sử dữ liệu lớn)
                    var allQuestions = await Task.Run(() => JsonSerializer.Deserialize<List<Question>>(json));

                    if (allQuestions != null)
                    {
                        // Chọn ngẫu nhiên 'countToPick' câu từ tập hợp này
                        var randomQuestions = allQuestions
                            .OrderBy(_ => random.Next()) // Sắp xếp ngẫu nhiên
                            .Take(countToPick)
                            .ToList();

                        gameQuestions.AddRange(randomQuestions);
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi: Lỗi khi đọc hoặc parse JSON
                }
            }

            return gameQuestions;
        }
    }   
}
