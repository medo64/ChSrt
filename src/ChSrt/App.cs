namespace ChSrt;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

internal static class App {
    internal static int Main(string[] args) {
        var inPlaceOption = new Option<bool>("--in-place", "-i") {
            Description = "Edit file in place",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var timeOption = new Option<decimal>("--adjust-time", "-t") {
            Description = "Time adjustment in seconds (can be negative)",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => 0,
        };

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

        // Default command
        var rootCommand = new RootCommand("SRT manipulation tool") {
            fileArgument,
            inPlaceOption,
            timeOption,
        };
        rootCommand.SetAction(result => {
            Exec(
                 result.GetValue(fileArgument)!,  // handled by parser
                 result.GetValue(inPlaceOption)!,
                 result.GetValue(timeOption)!
            );

        });

        return rootCommand.Parse(args).Invoke();
    }

    private static void Exec(FileInfo file, bool inPlace, decimal timeAdjustment) {
        SrtFile srt;
        using (var fs = file.OpenRead()) {
            srt = SrtFile.Load(fs);
        }

        srt.FixAll();
        srt.AdjustTime(TimeSpan.FromMilliseconds((long)(timeAdjustment * 1000)));

        if (inPlace) {
            using (var fs = file.OpenWrite()) {
                srt.Save(fs);
            }
        } else {
            srt.Save(Console.OpenStandardOutput(), Environment.NewLine);
        }
    }

}
