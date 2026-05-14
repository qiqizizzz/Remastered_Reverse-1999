/*
* ┌──────────────────────────────────┐
* │  描    述: 服务端轻量日志封装
* │  类    名: QLog.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;

namespace GameServer.Common
{
    public static class QLog
    {
        public static void Info(object msg) => Console.WriteLine(msg);

        public static void Warning(object msg) => Console.WriteLine($"[WARN] {msg}");

        public static void Error(object msg) => Console.Error.WriteLine($"[ERROR] {msg}");
    }
}
