namespace ChSrt;
using System;
using System.Threading;

internal static class Output {

    private static readonly Lock SyncRoot = new();

    public static void Warning(string message) {
        lock (SyncRoot) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void Error(string message) {
        lock (SyncRoot) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }
    }


    public static int VerbosityLevel { get; set; } = 0;

    public static void Verbose1(string message) {
        if (VerbosityLevel < 1) { return; }
        lock (SyncRoot) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void Verbose2(string message) {
        if (VerbosityLevel < 2) { return; }
        lock (SyncRoot) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }
    }

}
