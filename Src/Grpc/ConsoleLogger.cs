using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    public class ConsoleLogger<T> : ILogger<T>
    {
        public ConsoleLogger()
        {
            _scopeProvider = new LoggerExternalScopeProvider();
        }

        private readonly LoggerExternalScopeProvider _scopeProvider;

        #region Implementation of ILogger
        
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

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