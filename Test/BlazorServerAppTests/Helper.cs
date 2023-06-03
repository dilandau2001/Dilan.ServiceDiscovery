namespace BlazorServerAppTests
{
    internal static class Helper
    {
        /// <summary>
        /// Configures and existing mocked logger to redirect output to an existing output helper.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger">The logger.</param>
        /// <param name="outputHelper">The output helper.</param>
        public static void ConfigureLogger<T>(Mock<ILogger<T>> logger, ITestOutputHelper outputHelper)
        {
            logger.Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<T>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<T, Exception?, string>>()))
                .Callback(
                    (LogLevel a, EventId b, T c, Exception d, Func<T, Exception?, string> e) =>
                    {
                        var res = e.Invoke(c, d);
                        outputHelper.WriteLine($"{DateTimeOffset.Now:HH:mm:ss.fff} - [{a}]= Message={res}");
                    });
            
            logger
                .Setup(n => n.BeginScope(It.IsAny<T>()))
                .Callback((T a) =>
                {
                    outputHelper.WriteLine("Scope Created " + a);
                });
        }

        /// <summary>
        /// Formats the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static string Format(string message, params object[] args)
        {
            var tokens = FindTokens(message);

            if (!tokens.Any())
                return message;
            
            var result = message;

            if (tokens.Count == args.Length)
            {
                for (var i = 0; i < tokens.Count; i++)
                {
                    var arg = args[i].ToString();
                    result = result.Replace(tokens[i], arg);
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the tokens.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private static List<string> FindTokens(string message)
        {
            var tokens = new List<string>();
            var state = 0;
            var token = string.Empty;

            foreach (var c in message)
            {
                switch (state)
                {
                    case 0:

                        if (c == '{')
                        {
                            state = 1;
                            token += c;
                        }

                        break;

                    case 1:

                        token += c;

                        if (c == '}')
                        {
                            if (tokens.All(n => n != token))
                            {
                                tokens.Add(token);
                                token = string.Empty;
                            }

                            state = 0;
                        }

                        break;
                }
            }
            
            return tokens;
        }
    }
}
