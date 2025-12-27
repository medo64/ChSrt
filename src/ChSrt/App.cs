namespace ChSrt;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

internal static class App {
    internal static int Main(string[] args) {
        var fileArgument = new Argument<FileInfo>("file") {
            Description = "File to use",
            Arity = ArgumentArity.ExactlyOne
        };
        fileArgument.Validators.Add(result => {
            var value = result.GetValueOrDefault<FileInfo>();
            if (!value.Exists) {
                result.AddError($"File \"{value.FullName}\" doesn't exist");
            }
        });

        var cleanAllOption = new Option<bool>("--clean-all", "-c") {
            Description = "Executes all available cleanup operations",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var fixAllOption = new Option<bool>("--fix-all", "-f") {
            Description = "Executes all available fixup operations",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var inPlaceOption = new Option<bool>("--in-place", "-i") {
            Description = "Edit file in place",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var timeAdjustOption = new Option<decimal>("--time-adjust", "-t") {
            Description = "Time adjustment in seconds (can be negative)",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => 0,
        };

        // Default command
        var rootCommand = new RootCommand("SRT manipulation tool") {
            fileArgument,
            cleanAllOption,
            fixAllOption,
            inPlaceOption,
            timeAdjustOption,
        };
        rootCommand.SetAction(result => {
            Exec(
                 result.GetValue(fileArgument)!,  // handled by parser
                 result.GetValue(cleanAllOption)!,
                 result.GetValue(fixAllOption)!,
                 result.GetValue(inPlaceOption)!,
                 result.GetValue(timeAdjustOption)!
            );

        });

        return rootCommand.Parse(args).Invoke();
    }

    private static void Exec(FileInfo file, bool cleanAll, bool fixAll, bool inPlace, decimal timeAdjust) {
        SrtFile srt;
        using (var fs = file.OpenRead()) {
            srt = SrtFile.Load(fs);
        }

        if (cleanAll) { srt.CleanAll(); }
        if (fixAll) { srt.FixAll(); }
        if (timeAdjust != 0) { srt.AdjustTime(TimeSpan.FromMilliseconds((long)(timeAdjust * 1000))); }

        if (inPlace) {
            using var fs = file.OpenWrite();
            srt.Save(fs);
        } else {
            srt.Save(Console.OpenStandardOutput(), Environment.NewLine);
        }
    }

}
