using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Logger that filters editor messages by configured verbosity.
    /// </summary>
    public class Logger
    {
        #region Fields
        private readonly DataContainer m_DataContainer;
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new logger for the provided data container.
        /// </summary>
        /// <param name="dataContainer">The data container that supplies settings.</param>
        public Logger(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
        }

        /// <summary>
        /// Logs a message at the specified log level.
        /// </summary>
        /// <param name="invoker">The object issuing the log.</param>
        /// <param name="logLevel">The log level for the message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(object invoker, LogLevelID logLevel, string message)
        {
            if (m_DataContainer.Settings == null)
            {
                Debug.LogError($"Log failed! Settings = null!");

                return;
            }


            if (logLevel == LogLevelID.OnlyErrors)
            {
                Debug.LogError($"{invoker.GetType().Name}: {message}");

                return;
            }


            if (logLevel <= m_DataContainer.Settings.LogLevel)
            {
                Debug.Log($"{invoker.GetType().Name}: {message}");
            }
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="invoker">The object issuing the log.</param>
        /// <param name="message">The message to log.</param>
        public void LogError(object invoker, string message)
        {
            Log(invoker, LogLevelID.OnlyErrors, message);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="invoker">The object issuing the log.</param>
        /// <param name="message">The message to log.</param>
        public void LogInfo(object invoker, string message)
        {
            Log(invoker, LogLevelID.Info, message);
        }

        /// <summary>
        /// Logs a developer-level diagnostic message.
        /// </summary>
        /// <param name="invoker">The object issuing the log.</param>
        /// <param name="message">The message to log.</param>
        public void LogDev(object invoker, string message)
        {
            Log(invoker, LogLevelID.Developer, message);
        }
        #endregion
    }
}
