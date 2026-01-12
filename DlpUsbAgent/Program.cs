using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DlpUsbAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DLP-USB Agent - Prototype");
            Console.WriteLine("Surveillance des supports amovibles en cours...");
            Console.WriteLine("Appuyez sur Entrée pour quitter.\n");

            // Lancer le watcher sur tous les lecteurs amovibles existants
            UsbWatcher.StartWatchingRemovableDrives();

            // Boucle simple pour garder le programme vivant
            Console.ReadLine();
        }
    }

    static class UsbWatcher
    {
        public static void StartWatchingRemovableDrives()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (var drive in allDrives)
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    Console.WriteLine($"[INFO] Lecteur amovible détecté : {drive.Name}");
                    UsbFileMonitor monitor = new UsbFileMonitor(drive.RootDirectory.FullName);
                    monitor.Start();
                }
            }

            // ⚠️ Pour un vrai produit, il faudrait aussi surveiller l’arrivée
            // de nouvelles clés branchées après le lancement (via WMI).
            // Ici, pour le POC, on gère uniquement celles présentes au démarrage.
        }
    }

    class UsbFileMonitor
    {
        private readonly string _rootPath;
        private readonly FileSystemWatcher _watcher;

        public UsbFileMonitor(string rootPath)
        {
            _rootPath = rootPath;
            _watcher = new FileSystemWatcher(_rootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            _watcher.Created += OnChangedOrCreated;
            _watcher.Changed += OnChangedOrCreated;
        }

        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
            Console.WriteLine($"[INFO] Surveillance active sur : {_rootPath}");
        }

        private void OnChangedOrCreated(object sender, FileSystemEventArgs e)
        {
            // On ignore les dossiers
            if (Directory.Exists(e.FullPath)) return;

            // On ignore les fichiers déjà chiffrés
            if (e.FullPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                Console.WriteLine($"[EVENT] Fichier détecté : {e.FullPath}");

                // Petite tempo pour laisser le temps au système de finir la copie
                System.Threading.Thread.Sleep(500);

                if (File.Exists(e.FullPath))
                {
                    FileEncryptor.EncryptFile(e.FullPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERREUR] Sur le fichier {e.FullPath} : {ex.Message}");
            }
        }
    }

    static class FileEncryptor
    {
        // ⚠️ Pour un POC : clé/IV codés en dur.
        // Pour un vrai système : à stocker autrement (coffre-fort, config chiffrée, etc.)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"); // 32 bytes = 256 bits
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("ABCDEF0123456789"); // 16 bytes = 128 bits

        public static void EncryptFile(string filePath)
        {
            string encPath = filePath + ".enc";

            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (FileStream outputFileStream = new FileStream(encPath, FileMode.Create, FileAccess.Write))
                    using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write))
                    {
                        inputFileStream.CopyTo(cryptoStream);
                    }
                }

                // Suppression du fichier en clair après chiffrement
                File.Delete(filePath);

                Logger.Log($"[OK] Fichier chiffré : {filePath} -> {encPath}");
                Console.WriteLine($"[OK] Fichier chiffré : {encPath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERREUR] Chiffrement de {filePath} : {ex.Message}");
            }
        }
    }

    static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");

        public static void Log(string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            try
            {
                File.AppendAllLines(LogFilePath, new[] { line });
            }
            catch
            {
                // Éviter de faire planter l'appli à cause des logs
            }
        }
    }
}
