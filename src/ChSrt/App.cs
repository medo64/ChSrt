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

        var cleanAssOption = new Option<bool>("--clean-ass") {
            Description = "Cleans ASS tags",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var cleanHtmlOption = new Option<bool>("--clean-html") {
            Description = "Cleans HTML tags",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var cleanHtmlAllOption = new Option<bool>("--clean-html-all") {
            Description = "Cleans HTML tags (including bold and italic)",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var fixAllOption = new Option<bool>("--fix-all", "-f") {
            Description = "Executes all available fixup operations",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var fixIndices = new Option<bool>("--fix-indices") {
            Description = "Fixes subtitle indices",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var fixOrder = new Option<bool>("--fix-order") {
            Description = "Sorts all subtitles by time",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

        var fixOverlap = new Option<bool>("--fix-overlap") {
            Description = "Fixes overlapping subtitles",
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
            cleanAssOption,
            cleanHtmlOption,
            cleanHtmlAllOption,
            fixAllOption,
            fixIndices,
            fixOrder,
            fixOverlap,
            inPlaceOption,
            timeAdjustOption,
        };
        rootCommand.SetAction(result => {
            Exec(
                 result.GetValue(fileArgument)!,  // handled by parser
                 result.GetValue(cleanAllOption)!,
                 result.GetValue(cleanAssOption)!,
                 result.GetValue(cleanHtmlOption)!,
                 result.GetValue(cleanHtmlAllOption)!,
                 result.GetValue(fixAllOption)!,
                 result.GetValue(fixIndices)!,
                 result.GetValue(fixOrder)!,
                 result.GetValue(fixOverlap)!,
                 result.GetValue(inPlaceOption)!,
                 result.GetValue(timeAdjustOption)!
            );

        });

        return rootCommand.Parse(args).Invoke();
    }

    private static void Exec(FileInfo file,
                             bool cleanAll, bool cleanAss, bool cleanHtml, bool cleanHtmlAll,
                             bool fixAll, bool fixIndices, bool fixOrder, bool fixOverlap,
                             bool inPlace, decimal timeAdjust) {
        SrtFile srt;
        using (var fs = file.OpenRead()) {
            srt = SrtFile.Load(fs);
        }

        if (cleanAll) {
            srt.CleanAll();
            if (cleanHtmlAll) { srt.CleanHtmlTags(cleanBoldAndItalic: true); }
        } else {
            if (cleanAss) { srt.CleanAssTags(); }
            if (cleanHtml) { srt.CleanHtmlTags(); }
            if (cleanHtmlAll) { srt.CleanHtmlTags(cleanBoldAndItalic: true); }
        }

        if (fixAll) {
            srt.FixAll();
        } else {
            if (fixOrder) { srt.FixTimeOrder(); }
            if (fixOverlap) { srt.FixTimeOverlaps(); }
            if (fixIndices) { srt.FixIndices(); }
        }

        if (timeAdjust != 0) { srt.AdjustTime(TimeSpan.FromMilliseconds((long)(timeAdjust * 1000))); }

        if (inPlace) {
            using var fs = file.OpenWrite();
            srt.Save(fs);
        } else {
            srt.Save(Console.OpenStandardOutput(), Environment.NewLine);
        }
    }

}
