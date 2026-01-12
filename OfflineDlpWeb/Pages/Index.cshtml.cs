using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Linq;

namespace OfflineDlpWeb.Pages
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class IndexModel : PageModel
    {
        private readonly string _agentPath;
        private readonly string _agentProcessName;

        public bool AgentRunning { get; set; }

        public IndexModel(IConfiguration config)
        {
            _agentPath = config["OfflineDlp:AgentPath"]
                         ?? @"C:\Program Files\OfflineDLP\DlpUsbAgent.exe";

            _agentProcessName = Path.GetFileNameWithoutExtension(_agentPath);
        }

        public void OnGet()
        {
            AgentRunning = IsAgentRunning();
        }

        public IActionResult OnPostStartAgent()
        {
            if (!IsAgentRunning())
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = _agentPath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return RedirectToPage();
        }

        public IActionResult OnPostStopAgent()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(_agentProcessName))
                {
                    p.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return RedirectToPage();
        }

        private bool IsAgentRunning()
        {
            return Process.GetProcessesByName(_agentProcessName).Any();
        }
    }
}
