using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Implementation of an ILogger that prints to console.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConsoleLogger<T> : ILogger<T>
    {
        /// <summary>
        /// Initializes a new instance of the ConsoleLogger class.
        /// </summary>
        public ConsoleLogger()
        {
            _scopeProvider = new LoggerExternalScopeProvider();
        }

        /// <summary>
        /// Scope provider.
        /// </summary>
        private readonly LoggerExternalScopeProvider _scopeProvider;

        #region Implementation of ILogger
        
        /// <summary>
        /// Gets a value indicating whether the logger is enabled.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        /// <summary>
        /// Begin a scope. Adds a context to the scope provider.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

        /// <summary>
        /// Prints message to the console.
        /// </summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var sb = new StringBuilder();
            sb.Append(GetLogLevelString(logLevel))
                .Append(" [")
                .Append(eventId)
                .Append("] ");

            // Append scopes
            _scopeProvider.ForEachScope((scope, s) =>
            {
                s.Append(" => ");
                s.Append(scope);
            }, sb);

            if (exception != null)
            {
                sb.Append(exception);
            }
            
            sb.Append(formatter(state, exception));

            try
            {
                Console.WriteLine(sb.ToString());
            }
            catch
            {
                // Ignore all trace errors.
            }
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return "[VRB]";
                case LogLevel.Debug: return "[DBG]";
                case LogLevel.Information: return "[INF]";
                case LogLevel.Warning: return "[WAR]";
                case LogLevel.Error: return "[ERR]";
                case LogLevel.Critical: return "[CRT]";
            }

            return "[VRB]";
        }

        #endregion
    }
}