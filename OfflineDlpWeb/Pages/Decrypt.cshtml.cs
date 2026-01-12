using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;

namespace OfflineDlpWeb.Pages
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class DecryptModel : PageModel
    {
        private readonly IWebHostEnvironment _env;

        public DecryptModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        [BindProperty]
        public IFormFile EncFile { get; set; }

        public string? DecryptedFileName { get; set; }
        public string? ErrorMessage { get; set; }
	// même clé ET même IV que l'agent
	private static readonly byte[] KeyBytes =
    		System.Text.Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
	private static readonly byte[] IVBytes =
    		System.Text.Encoding.UTF8.GetBytes("ABCDEF0123456789");

        

        public async Task<IActionResult> OnPost()
        {
            if (EncFile == null || !EncFile.FileName.EndsWith(".enc"))
            {
                ErrorMessage = "Fichier invalide.";
                return Page();
            }

            try
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                var downloads = Path.Combine(_env.WebRootPath, "downloads");

                Directory.CreateDirectory(uploads);
                Directory.CreateDirectory(downloads);

                var tempEncPath = Path.Combine(uploads, EncFile.FileName);

                using (var fs = new FileStream(tempEncPath, FileMode.Create))
                    await EncFile.CopyToAsync(fs);

                // Nom du fichier déchiffré
                DecryptedFileName = EncFile.FileName.Replace(".enc", "");
                var decryptedPath = Path.Combine(downloads, DecryptedFileName);

                DecryptFile(tempEncPath, decryptedPath);

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Erreur : " + ex.Message;
                return Page();
            }
        }

       private void DecryptFile(string inputPath, string outputPath)
       {
          using FileStream input = new FileStream(inputPath, FileMode.Open);
          using FileStream output = new FileStream(outputPath, FileMode.Create);
          using Aes aes = Aes.Create();

          aes.Key = KeyBytes;
          aes.IV = IVBytes;
          aes.Mode = CipherMode.CBC;
          aes.Padding = PaddingMode.PKCS7;

          using CryptoStream cryptoStream =
            new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);

    	  cryptoStream.CopyTo(output);
	}

    }
}
