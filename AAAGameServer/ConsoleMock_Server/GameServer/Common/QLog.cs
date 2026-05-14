/*
* ┌──────────────────────────────────┐
* │  描    述: 服务端轻量日志封装，Release 构建自动抹除 Info/Warning
* │  类    名: QLog.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Diagnostics;

namespace GameServer.Common
{
    public static class QLog
    {
        [Conditional("DEBUG")]
        public static void Info(object msg) => Console.WriteLine($"[INFO] {msg}");

        [Conditional("DEBUG")]
        public static void Warning(object msg) => Console.WriteLine($"[WARN] {msg}");

        public static void Error(object msg) => Console.Error.WriteLine($"[ERROR] {msg}");
    }
}
