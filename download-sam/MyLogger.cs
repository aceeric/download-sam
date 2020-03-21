using System;
using Logging;

namespace download_sam
{
    /// <summary>
    /// Examines the settings and if the application is running as a console application, sends messages to the console,
    /// otherwise sends messages to the logging store indicated by the settings. Extends the base class Logger
    /// </summary>

    class MyLogger : Logger
    {
        /// <summary>
        /// Initialize logging level based on settings. If the optional logging level is not specified then the default
        /// is Informational, which logs everything
        /// </summary>

        public void InitLoggingSettings()
        {
            if (AppSettingsImpl.LogLevel.Initialized)
            {
                switch (AppSettingsImpl.LogLevel.Value.ToString().ToLower())
                {
                    case "err":
                        Level = Logging.LogLevel.Error;
                        break;
                    case "warn":
                        Level = Logging.LogLevel.Warning;
                        break;
                    case "info":
                        Level = Logging.LogLevel.Information;
                        break;
                }
            }
            if (AppSettingsImpl.Log.Initialized)
            {
                switch (AppSettingsImpl.Log.Value.ToString().ToLower())
                {
                    // since Console is default, only take action if that is changed by the user
                    case "file":
                        Output = LogOutput.ToFile;
                        break;
                    case "db":
                        Output = LogOutput.ToDatabase;
                        break;
                    default:
                        Output = LogOutput.ToConsole;
                        break;
                }
            }
            if (AppSettingsImpl.JobID.Initialized)
            {
                // The settings parser ensures that the GUID is valid so this is safe
                GUID = Guid.Parse(AppSettingsImpl.JobID.Value);
            }
            JobName = "sam-monthly";
        }

        /// <summary>
        /// Invokes the base class constructor
        /// </summary>

        public MyLogger() : base() { }
    }
}
