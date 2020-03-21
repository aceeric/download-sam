using AppSettings;
using CryptLib;
using System;
using System.IO;
using System.Threading;
using System.IO.Compression;

namespace download_sam
{
    enum ExitCode : int
    {
        /// <summary>
        /// Indicates successful completion
        /// </summary>
        Success = 0,
        /// <summary>
        /// Indicates that invalid settings or command line args were provided
        /// </summary>
        InvalidParameters = 1,
        /// <summary>
        /// Indicates that some other error occurred
        /// </summary>
        OtherError = 99
    }

    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] {
              "-yyyymm", "default"
             ,"-ingest", "./"
             ,"-onlydownload"
            };
#endif

            try
            {
                if (!AppSettingsImpl.Parse(SettingsSource.CommandLine, args))
                {
                    if (AppSettingsImpl.ParseErrorMessage != null)
                    {
                        Console.WriteLine(AppSettingsImpl.ParseErrorMessage);
                    }
                    else
                    {
                        AppSettingsImpl.ShowUsage();
                    }
                    Environment.ExitCode = (int)ExitCode.InvalidParameters;
                    return;
                }

                if (AppSettingsImpl.Encrypt.Value || AppSettingsImpl.Decrypt.Value) // only one can be true. Both are false by default
                {
                    if (!AppSettingsImpl.AccessKey.Initialized || !AppSettingsImpl.SecretKey.Initialized)
                    {
                        Globals.Log.ErrorMessage("The encrypt or decrypt option was specified, but either one or both of the keys were not proided.");
                        return;
                    }
                    // regardless of the output setting (to log or console or db) write this to the console so it
                    // is not persisted if the Decrypt function is specified

                    Console.WriteLine("Access Key:");
                    Console.WriteLine(EncryptOrDecryptKey(AppSettingsImpl.AccessKey.Value, AppSettingsImpl.Encrypt.Value));
                    Console.WriteLine("Secret Access Key:");
                    Console.WriteLine(EncryptOrDecryptKey(AppSettingsImpl.SecretKey.Value, AppSettingsImpl.Encrypt.Value));
                    return;
                }

                Globals.Log.InitLoggingSettings();

                Globals.Log.InformationMessage("Started");

                if (!AppSettingsImpl.OnlyDownload)
                {
                    string MonthlyFile = S3.FileExists();
                    if (MonthlyFile != string.Empty)
                    {
                        if (AppSettingsImpl.Force.Value)
                        {
                            Globals.Log.InformationMessage("Monthly File {0} has already been downloaded for this month, however the -Force argument was supplied, so the file will be re-downloaded", MonthlyFile);

                        }
                        else
                        {
                            Globals.Log.InformationMessage("Monthly File {0} has already been downloaded for this month. Nothing to do. Normal completion", MonthlyFile);
                            return;
                        }
                    }
                }

                DoWork();

                Environment.ExitCode = (int)ExitCode.Success;

                Globals.Log.InformationMessage("Normal completion");
            }
            catch (Exception Ex)
            {
                if (Ex is DownLoadException)
                {
                    Globals.Log.ErrorMessage(Ex.Message);
                }
                else
                {
                    Globals.Log.ErrorMessage("An unhandled exception occurred. The exception was: {0}. Stack trace follows:\n{1}", Ex.Message, Ex.StackTrace);
                }
                Environment.ExitCode = (int)ExitCode.OtherError;
            }
        }

        /// <summary>
        /// Main worker for the program
        /// </summary>

        private static void DoWork()
        {
            string Fqpn = DownloadFile();
            if (Fqpn != string.Empty)
            {
                if (!AppSettingsImpl.OnlyDownload)
                {
                    S3.CopyToS3(Fqpn);
                    UnzipFileToStagingDirectory(Fqpn);
                    // remove the downloaded file since it's been unzipped and also copied to S3
                    Globals.Log.InformationMessage("Deleting downloaded file from the work location: {0}", Fqpn);
                    File.Delete(Fqpn);
                }
                else
                {
                    Globals.Log.InformationMessage("File was downloaded: {0}\nThe '-onlydownload' option was specified, so no further processing will occur", Fqpn);
                }
            }
        }

        /// <summary>
        /// Download the monthly file from sam.gov. Tries to get a file named on the 1st through the 10th of the month,
        /// and retries each file three times. Gives up if totally unable to download the file
        /// </summary>
        /// <returns>The name of the downloaded file, or the empty string if nothing was downloaded.</returns>

        private static string DownloadFile()
        {
            const int THREE_MINUTES = 180;
            for (int day = 1; day <= 10; ++day)
            {
                string Url = string.Format("https://www.sam.gov/SAM/extractfiledownload?role=WW&version=SAM&filename=SAM_PUBLIC_MONTHLY_{0}{1:00}.ZIP",
                    Globals.FileYYYYMM, day);
                string FileName = Path.Combine(Directory.GetCurrentDirectory(), string.Format("SAM_PUBLIC_MONTHLY_{0}{1:00}.ZIP", Globals.FileYYYYMM, day));

                Globals.Log.InformationMessage("Built this URL: {0}", Url);
                Globals.Log.InformationMessage("Attempting to download file: {0}", FileName);

                for (int attempt = 0; attempt < 3; ++attempt)
                {
                    if (!DownloadAsync.DoDownload(Url, FileName, THREE_MINUTES))
                    {
                        Thread.Sleep(2000); // sleep two seconds and retry
                    }
                    else
                    {
                        Globals.Log.InformationMessage("Successfully downloaded {0}", FileName);
                        return FileName;
                    }
                }
            }
            Globals.Log.InformationMessage("No file found to download. Normal completion.");
            return string.Empty;
        }

        /// <summary>
        /// DEPRECATED
        /// Move the source file into the ingest target folder specified via the configuration settings
        /// </summary>
        /// <param name="Source">Fully-qualified path name of the file to move</param>

        private static void MoveFileToStagingDirectory(string Source)
        {
            Globals.Log.InformationMessage("Moving file to ingest folder");

            string Target = Path.Combine(AppSettingsImpl.Ingest.Value, Path.GetFileName(Source));
            if (File.Exists(Target))
            {
                Globals.Log.InformationMessage("Deleting existing file in target folder first");
                File.Delete(Target);
            }
            File.Move(Source, Target);

            Globals.Log.InformationMessage("Moved file for ingest: {0}", Target);
        }

        /// <summary>
        /// Extracts the SAM DAT file from the downloaded ZIP and places it into the ingest target folder
        /// specified via the configuration settings
        /// </summary>
        /// <param name="Source">Fully-qualified path name of the SAM ZIP file</param>

        private static void UnzipFileToStagingDirectory(string Source)
        {
            Globals.Log.InformationMessage("Unzipping downloaded archive to the ingest folder");

            using (ZipArchive archive = ZipFile.OpenRead(Source))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string Target = Path.Combine(AppSettingsImpl.Ingest.Value, entry.FullName);
                    if (File.Exists(Target))
                    {
                        Globals.Log.InformationMessage("Deleting existing file in target folder first: {0}", Target);
                        File.Delete(Target);
                    }
                    Globals.Log.InformationMessage("Extracting file: {0}", Target);
                    entry.ExtractToFile(Target); // there should only be one file in the archive...
                }
            }
            Globals.Log.InformationMessage("Unzip complete");
        }

        /// <summary>
        /// If Encrypt == true, then encrypt and return the key. The Key arg is treated as plain text. Else
        /// decrypt and return the key. The Key arg is treated as encrypted.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Encrypt"></param>
        /// <returns></returns>

        private static string EncryptOrDecryptKey(string Key, bool Encrypt)
        {
            string s = "";
            if (Encrypt)
            {
                s = Crypto.Protect(Key);
            }
            else
            {
                s = Crypto.Unprotect(Key);
            }
            return s;
        }
    }
}