﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples
{
    /// <summary>
    /// Basic logger printing to console
    /// </summary>
    internal static class ConsoleLogger
    {
        internal static ILogger Logger => LoggerFactory.CreateLogger<object>();

        internal static ILoggerFactory LoggerFactory => s_loggerFactory.Value;

        private static readonly Lazy<ILoggerFactory> s_loggerFactory = new(LogBuilder);

        private static ILoggerFactory LogBuilder()
        {
            return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);

                builder.AddFilter("Microsoft", LogLevel.Trace);
                builder.AddFilter("Microsoft", LogLevel.Debug);
                builder.AddFilter("Microsoft", LogLevel.Information);
                builder.AddFilter("Microsoft", LogLevel.Warning);
                builder.AddFilter("Microsoft", LogLevel.Error);

                builder.AddFilter("Microsoft", LogLevel.Warning);
                builder.AddFilter("System", LogLevel.Warning);

                builder.AddConsole();
            });
        }
    }
}
