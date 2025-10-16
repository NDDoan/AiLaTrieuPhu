using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    public interface IQuestionDataService
    {
        Task<List<Question>> GetRandomGameQuestionsAsync();
    }
}
