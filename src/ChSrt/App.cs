namespace ChSrt;

using System;
using System.CommandLine;
using System.IO;

internal static class App {
    internal static int Main(string[] args) {
        var fileArgument = new Argument<FileInfo[]>("file") {
            Description = "File(s) to use",
            Arity = ArgumentArity.OneOrMore
        };
        fileArgument.Validators.Add(result => {
            var files = result.GetValueOrDefault<FileInfo[]>();
            foreach (var file in files!) {
                if (!file.Exists) {
                    result.AddError($"File \"{file.FullName}\" doesn't exist");
                }
            }
        });

        var backupOption = new Option<bool>("--backup", "-b") {
            Description = "Creates a backup of the original file before editing",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };

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

        var verbosityLevel = 0;
        var verboseOption = new Option<bool>("--verbose", "-v") {
            Description = "Enable verbose output",
            Arity = ArgumentArity.Zero,
            DefaultValueFactory = _ => false,
        };
        verboseOption.Validators.Add(result => {
            verbosityLevel = result.IdentifierTokenCount;
        });

        // Default command
        var rootCommand = new RootCommand("SRT manipulation tool") {
            fileArgument,
            backupOption,
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
            verboseOption,
        };
        rootCommand.SetAction(result => {
            Exec(
                 result.GetValue(fileArgument)!,  // handled by parser
                 result.GetValue(backupOption)!,
                 result.GetValue(cleanAllOption)!,
                 result.GetValue(cleanAssOption)!,
                 result.GetValue(cleanHtmlOption)!,
                 result.GetValue(cleanHtmlAllOption)!,
                 result.GetValue(fixAllOption)!,
                 result.GetValue(fixIndices)!,
                 result.GetValue(fixOrder)!,
                 result.GetValue(fixOverlap)!,
                 result.GetValue(inPlaceOption)!,
                 result.GetValue(timeAdjustOption)!,
                 verbosityLevel
            );

        });

        return rootCommand.Parse(args).Invoke();
    }

    private static void Exec(FileInfo[] files,
                             bool backup,
                             bool cleanAll, bool cleanAss, bool cleanHtml, bool cleanHtmlAll,
                             bool fixAll, bool fixIndices, bool fixOrder, bool fixOverlap,
                             bool inPlace, decimal timeAdjust,
                             int verbosityLevel) {
        foreach (var file in files) {
            Output.VerbosityLevel = verbosityLevel;
            Output.Verbose1($"{file.FullName}");

            if (backup) {
                var backupFileName = file.FullName + ".bak";
                Output.Verbose2($"Backing up to \"{backupFileName}\"");
                if (File.Exists(backupFileName)) {
                    Output.Warning($"Backup file \"{backupFileName}\" already exists, skipping backup.");
                } else {
                    File.Copy(file.FullName, backupFileName, overwrite: true);
                }
            }

            SrtFile srt;
            using (var fs = file.OpenRead()) {
                Output.Verbose2($"Loading file");
                srt = SrtFile.Load(fs);
            }

            if (cleanAll) {
                Output.Verbose2($"Cleaning all tags");
                srt.CleanAll();
                if (cleanHtmlAll) {
                    Output.Verbose2($"Cleaning bold and italic tags");
                    srt.CleanHtmlTags(cleanBoldAndItalic: true);
                }
            } else {
                if (cleanAss) {
                    Output.Verbose2($"Cleaning ASS tags");
                    srt.CleanAssTags();
                }
                if (cleanHtml) {
                    Output.Verbose2($"Cleaning HTML tags");
                    srt.CleanHtmlTags();
                }
                if (cleanHtmlAll) {
                    Output.Verbose2($"Cleaning bold and italic tags");
                    srt.CleanHtmlTags(cleanBoldAndItalic: true);
                }
            }

            if (fixAll) {
                Output.Verbose2($"Fixing all issues");
                srt.FixAll();
            } else {
                if (fixOrder) {
                    Output.Verbose2($"Fixing time order");
                    srt.FixTimeOrder();
                }
                if (fixOverlap) {
                    Output.Verbose2($"Fixing time overlaps");
                    srt.FixTimeOverlaps();
                }
                if (fixIndices) {
                    Output.Verbose2($"Fixing indices");
                    srt.FixIndices();
                }
            }

            if (timeAdjust != 0) {
                var ts = TimeSpan.FromMilliseconds((long)(timeAdjust * 1000));
                Output.Verbose2($"Adjusting time by {ts}");
                srt.AdjustTime(ts);
            }

            if (inPlace) {
                Output.Verbose2($"Saving file");
                using var fs = file.OpenWrite();
                srt.Save(fs);
            } else {
                srt.Save(Console.OpenStandardOutput(), Environment.NewLine);
            }
        }
    }

}
