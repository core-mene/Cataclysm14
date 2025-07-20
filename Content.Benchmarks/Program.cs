// SPDX-FileCopyrightText: 2020 Tyler Young
// SPDX-FileCopyrightText: 2022 Paul Ritter
// SPDX-FileCopyrightText: 2023 Visne
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers
// SPDX-FileCopyrightText: 2025 Leon Friedrich
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using Robust.Benchmarks.Configs;

namespace Content.Benchmarks
{
    internal static class Program
    {

        public static void Main(string[] args)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nWARNING: YOU ARE RUNNING A DEBUG BUILD, USE A RELEASE BUILD FOR AN ACCURATE BENCHMARK");
            Console.WriteLine("THE DEBUG BUILD IS ONLY GOOD FOR FIXING A CRASHING BENCHMARK\n");
            var baseConfig = new DebugInProcessConfig();
#else
            var baseConfig = Environment.GetEnvironmentVariable("ROBUST_BENCHMARKS_ENABLE_SQL") != null
                ? DefaultSQLConfig.Instance
                : DefaultConfig.Instance;
#endif
            var config = ManualConfig.Create(baseConfig);
            config.BuildTimeout = TimeSpan.FromMinutes(5);
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
