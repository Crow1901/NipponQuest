using Microsoft.AspNetCore.Mvc;

namespace NipponQuest.Controllers
{
    public class LearningController : Controller
    {
        // GET: /Learning/Hiragana
        public IActionResult Hiragana()
        {
            ViewData["Title"] = "Hiragana Mastery";
            return View();
        }

        // GET: /Learning/Katakana
        public IActionResult Katakana()
        {
            ViewData["Title"] = "Katakana Mastery";
            return View();
        }
    }
}