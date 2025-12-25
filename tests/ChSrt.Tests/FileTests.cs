namespace Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChSrt;

[TestClass]
public sealed class File_Tests {

    [DataTestMethod]
    [DynamicData(nameof(FileAssets))]
    public void File_Basic(string fileIn, string fileOut) {
        var streamIn = GetStream(fileIn)!;
        var streamOut = GetStream(fileOut);

        var srtIn = SrtFile.Load(streamIn);
        srtIn.CleanAll();
        srtIn.FixAll();
        var srtActual = new MemoryStream();
        srtIn.Save(srtActual);
        srtActual.Position = 0;

        var srtBytesExpected = streamOut.ToArray();
        var srtBytesActual = srtActual.ToArray();

        var linesExpected = GetLines(srtBytesExpected);
        var linesActual = GetLines(srtBytesActual);
        for (var i = 0; i < Math.Max(linesExpected.Length, linesActual.Length); i++) {
            if (linesExpected.Length <= i) {
                Assert.Fail($"Files differ at line {i + 1}:\n  Expected: <NO LINE>\n  Actual .: {linesActual[i]}");
            } else if (linesActual.Length <= i) {
                Assert.Fail($"Files differ at line {i + 1}:\n  Expected: {linesExpected[i]}\n  Actual .: <NO LINE>");
            } else if (!linesExpected[i].Equals(linesActual[i], StringComparison.Ordinal)) {
                Assert.Fail($"Files differ at line {i + 1}:\n  Expected: {linesExpected[i]}\n  Actual .: {linesActual[i]}");
            }
        }
    }


    private static IEnumerable<(string, string)> FileAssets {
        get {
            var foundAny = false;

            var resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames()!;
            foreach (var resName in resNames) {
                if (!resName.EndsWith(".srt", StringComparison.Ordinal)) { continue; }

                var parts = resName.Split('.');
                if (parts.Length != 5) { continue; }
                if (!parts[2].Equals("In", StringComparison.Ordinal)) { continue; }

                foundAny = true;
                var outFile = string.Join('.', parts[0], parts[1], "Out", parts[3], parts[4]);
                yield return (resName, outFile);
            }

            if (!foundAny) { throw new InvalidOperationException($"No documents found"); }
        }
    }

    private static MemoryStream GetStream(string streamName) {
        var assembly = Assembly.GetExecutingAssembly();
        var resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        foreach (var resName in resNames) {
            if (resName.Equals(streamName, StringComparison.Ordinal)) {
                var resStream = assembly.GetManifestResourceStream(streamName);
                var buffer = new byte[(int)resStream!.Length];
                resStream.Read(buffer, 0, buffer.Length);
                return new MemoryStream(buffer) { Position = 0 };
            }
        }
        return null;
    }

    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static string[] GetLines(byte[] bytes) {
        return Utf8.GetString(bytes).Split('\n', StringSplitOptions.None);
    }

}
