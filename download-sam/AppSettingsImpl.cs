using System;
using AppSettings;
using System.Text.RegularExpressions;
using System.IO;

namespace download_sam
{
    /// <summary>
    /// Extends the AppSettingsBase class with settings needed by the utility
    /// </summary>

    class AppSettingsImpl : AppSettingsBase
    {
        /// <summary>
        /// Specifies the Job ID
        /// </summary>
        public static StringSetting JobID { get { return (StringSetting)SettingsDict["JobID"]; } }

        /// <summary>
        /// Specifies the year and month of the file to download
        /// </summary>
        public static StringSetting YYYYMM { get { return (StringSetting)SettingsDict["yyyymm"]; } }

        /// <summary>
        /// The registry key that will be used to store settings, if the registry is used for settings persistence
        /// </summary>
        public new static string RegKey { get { return "SCSInc\\SamDownload\\v1"; } }

        /// <summary>
        /// Logging target (e.g. file, database, console)
        /// </summary>
        public static StringSetting Log { get { return (StringSetting)SettingsDict["Log"]; } }

        /// <summary>
        /// Specifies which type of events are logged
        /// </summary>
        public static StringSetting LogLevel { get { return (StringSetting)SettingsDict["LogLevel"]; } }

        /// <summary>
        /// Specifies the AWS Access Key to provide S3 bucket access
        /// </summary>
        public static StringSetting AccessKey { get { return (StringSetting)SettingsDict["AccessKey"]; } }

        /// <summary>
        /// Specifies the AWS Secret Access Key to provide S3 bucket access
        /// </summary>
        public static StringSetting SecretKey { get { return (StringSetting)SettingsDict["SecretKey"]; } }

        /// <summary>
        /// Specifies the S3 bucket name, excluding any prefixes. E.g. "mybucket"
        /// </summary>
        public static StringSetting Bucket { get { return (StringSetting)SettingsDict["Bucket"]; } }

        /// <summary>
        /// Specifies the AWS Bucket prefix to prefix the uploaded file with
        /// </summary>
        public static StringSetting Prefix { get { return (StringSetting)SettingsDict["Prefix"]; } }

        /// <summary>
        /// Allows you to override the uploaded filename. By default the uploaded filename in S3 is the same
        /// as the downloaded filename.
        /// </summary>
        public static StringSetting KeyName { get { return (StringSetting)SettingsDict["KeyName"]; } }

        /// <summary>
        /// Specifies the fully-qualified pathname of the staging directory to place the downloaded file into
        /// for ingest. E.g. "d:\x\y\z"
        /// </summary>
        public static StringSetting Ingest { get { return (StringSetting)SettingsDict["Ingest"]; } }

        /// <summary>
        /// True if the key and secret key are encrypted. False if they are plain text
        /// </summary>
        public static BoolSetting Encrypted { get { return (BoolSetting)SettingsDict["Encrypted"]; } }

        /// <summary>
        /// True to encrypt the key and secret key, display the encrypted values to to console and then exit
        /// </summary>
        public static BoolSetting Encrypt { get { return (BoolSetting)SettingsDict["Encrypt"]; } }

        /// <summary>
        /// True to decrypt the key and secret key, display the decrypted values to to console and then exit
        /// </summary>
        public static BoolSetting Decrypt { get { return (BoolSetting)SettingsDict["Decrypt"]; } }

        /// <summary>
        /// True to force the download even if the file has already been downloaded for the month
        /// </summary>
        public static BoolSetting Force { get { return (BoolSetting)SettingsDict["Force"]; } }

        /// <summary>
        /// If True, only download the file and stop - don't copy to S3
        /// </summary>
        public static BoolSetting OnlyDownload { get { return (BoolSetting)SettingsDict["OnlyDownload"]; } }

        /// <summary>
        /// Initializes the instance with an array of settings that the application supports, as well as usage instructions
        /// </summary>

        static AppSettingsImpl()
        {
            SettingList = new Setting[] {
                new StringSetting("JobID", "guid", null,  Setting.ArgTyp.Optional, true, false,
                    "Defines a job ID for the logging subsystem. A GUID value is supplied in the canonical 8-4-4-4-12 form. If provided, then " +
                    "the logging subsystem is initialized with the provided GUID. The default behavior is for the logging subsystem to generate its own GUID."),
                new StringSetting("YYYYMM", "n", null,  Setting.ArgTyp.Mandatory, true, false,
                    "Specifies the year and month of the SAM file to download. Specify the literal 'default' (no quotes) to download " +
                    "a file for the year and month corresponding to the current system time. Otherwise specify the exact year and month " +
                    "to download. E.g. \"201707\" for July 2017."),
                new StringSetting("Log", "file|db|con", "con",  Setting.ArgTyp.Optional, true, false,
                    "Determines how the application communicates errors, status, etc. If not supplied, then all output goes to the console. " +
                    "If 'file' is specified, then the application logs to a log file in the application directory called download-sam.log. " +
                    "If 'db' is specified, then logging occurs to the database. If 'con' is specified, then output goes to the console " +
                    "(same as if the arg were omitted.) If logging to file or db is specified then the application runs silently " +
                    "with no console output."),
                new StringSetting("LogLevel", "err|warn|info", "info",  Setting.ArgTyp.Optional, true, false,
                    "Defines the logging level. 'err' specifies that only errors will be reported. 'warn' means errors and warnings, " +
                    "and 'info' means all messages. The default is 'info'."),
                new StringSetting("AccessKey", "keyval", null,  Setting.ArgTyp.Optional, true, false,
                    "Your AWS Access Key."),
                new StringSetting("SecretKey", "keyval", null,  Setting.ArgTyp.Optional, true, false,
                    "Your AWS Secret Access Key."),
                new StringSetting("Bucket", "s3bucket", null,  Setting.ArgTyp.Optional, true, false,
                    "The name of the S3 bucket that the SAM download data should be uploaded into.  Do not prefix with any S3 URL prefixes such " +
                    "as S3: or S3://. Provide only the bucket name."),
                new StringSetting("Prefix", "prefix", null,  Setting.ArgTyp.Optional, true, false,
                    "The name of the S3 bucket prefix that will be prefixed to the file name when the file is uploaded to S3. " +
                    "For example, if your bucket is named 'mybucket' and the file to download is SAM_PUBLIC_MONTHLY_20170702.ZIP, and you specify " +
                    "\"sam\" as the bucket prefix then the file will be copied to the following S3 url: \"s3://mybucket/sam/SAM_PUBLIC_MONTHLY_20170702.ZIP\". " +
                    "If no prefix is specified then the file will be uploaded into the \"root\" of the bucket. **NOTE** S3 keys and prefixes are case sensitive."),
                new StringSetting("Ingest", "fqpn", null,  Setting.ArgTyp.Optional, true, false,
                    "The fully-qualified path name of the ingest folder that the file should be placed in. The utility will download the file from " +
                    "the sam.gov website, place it in this location for ingest, and copy an archive copy to S3. The specified directory must exist, and it " +
                    "must be a directory (not a file) otherwise the utility will abort."),
                new BoolSetting("Encrypted", false,  Setting.ArgTyp.Optional, true, false,
                    "If provided, then the two key values are taken to be encrypted values. The utlity will decrypt them when accessing the " +
                    "AWS bucket. If not provided, then the two key values are interpreted as plain text values."),
                new BoolSetting("Encrypt", false,  Setting.ArgTyp.Optional, false, false,
                    "Encrypt the provided keys. Display the encrypted values to the console, and immediately exit with no further processing."),
                new BoolSetting("Decrypt", false,  Setting.ArgTyp.Optional, false, false,
                    "Decrypt the provided keys. Display the decrypted values to the console, and immediately exit with no further processing."),
                new BoolSetting("Force", false,  Setting.ArgTyp.Optional, false, false,
                    "Specify this argument to force the utility to download the monthly file even if the S3 bucket already has a file for " +
                    "the current month. The default behavior, if this arg is omitted, is for the utility to check the S3 bucket on " +
                    "startup and if a file for the year and month specified by the -yyyymm arg is present there, the utility exits without doing anything."),
                new StringSetting("KeyName", "name", null,  Setting.ArgTyp.Optional, true, false,
                    "The key name to assign to the file when it is uploaded to S3. By default, the downloaded filename is used as the key name " +
                    "when the object is copied to S3. This option enables you to override that and explicitly specify a key name. Note - if you " +
                    "specify this arg, as well as the -prefix arg, both are used."),
                new BoolSetting("OnlyDownload", false,  Setting.ArgTyp.Optional, false, false,
                    "If provided, the the utility stops after downloading the file, and does not copy the file to S3. If specified, then the " +
                    "AWS credentials are optional and ignored."),
            };

            Usage =
                "Downloads a monthly SAM ZIP file from sam.gov. Accesses the https://www.sam.gov website and looks for a downloadable file named " +
                "according to the historical filename pattern for SAM downloads (SAM_PUBLIC_MONTHLY_YYYYMMnn.ZIP) where 'YYYY' is the year, 'MM' " +
                "is the month, and 'nn' is the day between 1 and 10 that the file is generated by the website. The SAM monthly file is historically posted " +
                "in the first week of the month. This utility therefore incrementally scans the website starting on day 1, and progressing to day 10, for a file " +
                "matching the naming convention. Once a match is found, it is downloaded. and copied to the specified ingest folder, as well as to S3. Before beginning " +
                "any processing, the utility examines the passed S3 bucket to see if the file has already been downloaded and - if it has - then it exits without " +
                "doing anything. (Unless the -force argument is supplied.)";
        }

        /// <summary>
        /// Performs custom arg validation for the utility, after invoking the base class parser.
        /// </summary>
        /// <param name="Settings">A settings instance to parse</param>
        /// <param name="CmdLineArgs">Command-line args array</param>
        /// <returns>True if args are valid, else False</returns>

        public new static bool Parse(SettingsSource Settings, string[] CmdLineArgs = null)
        {
            if (AppSettingsBase.Parse(Settings, CmdLineArgs))
            {
                bool YYYYMMParses = false;
                if (YYYYMM.Value.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    int Year = DateTime.Now.Year;
                    int Month = DateTime.Now.Month;
                    Globals.FileYYYYMM = string.Format("{0:0000}{1:00}", Year, Month);
                    YYYYMMParses = true;
                }
                else
                {
                    int CmdLineMonth = 0;
                    if (!Regex.IsMatch(YYYYMM.Value, "^\\d{6}$"))
                    {
                        YYYYMMParses = false;
                    }
                    else if (!int.TryParse(YYYYMM.Value.Substring(4), out CmdLineMonth))
                    {
                        YYYYMMParses = false;
                    }
                    else if (!(1 <= CmdLineMonth && CmdLineMonth <= 12))
                    {
                        YYYYMMParses = false;
                    }
                    else
                    {
                        Globals.FileYYYYMM = YYYYMM.Value;
                        YYYYMMParses = true;
                    }
                }
                if (!YYYYMMParses)
                {
                    ParseErrorMessage = string.Format("yyyymm argument value {0} must be in the form YYYYMM", YYYYMM.Value);
                    return false;
                }
                if (!OnlyDownload && !Directory.Exists(Ingest.Value))
                {
                    ParseErrorMessage = string.Format("Ingest directory {0} does not exist", Ingest.Value);
                    return false;
                }
                if (!OnlyDownload)
                {
                    if (!AccessKey.Initialized || !SecretKey.Initialized || !Bucket.Initialized || !Ingest.Initialized)
                    {
                        ParseErrorMessage = "S3 credentials/bucket name and ingest folder are required unless the -onlydownload option is specified";
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
