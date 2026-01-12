using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OfflineDlpWeb.Pages
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class LogsModel : PageModel
    {
        private readonly string _logFilePath;

        public List<string> Lines { get; set; } = new();

        public LogsModel(IConfiguration config)
        {
            _logFilePath = config["OfflineDlp:LogFilePath"]
                           ?? @"C:\Program Files (x86)\OfflineDLP\logs.txt";
        }

        public void OnGet()
        {
            try
            {
                if (System.IO.File.Exists(_logFilePath))
                {
                    var allLines = System.IO.File.ReadAllLines(_logFilePath);
                    Lines = allLines
                        .Reverse()
                        .Take(200)
                        .Reverse()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Lines = new List<string> { "Erreur de lecture des logs : " + ex.Message };
            }
        }
    }
}
