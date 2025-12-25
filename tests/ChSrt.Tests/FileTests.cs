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
    [DynamicData(nameof(BasicAssets))]
    public void File_Basic(string fileIn, string fileOut) {
        TestFile(fileIn, fileOut, srt => { });
    }

    [DataTestMethod]
    [DynamicData(nameof(CleanupAssets))]
    public void File_Clean(string fileIn, string fileOut) {
        TestFile(fileIn, fileOut, srt => { srt.CleanAll();});
    }

    [DataTestMethod]
    [DynamicData(nameof(FixupAssets))]
    public void File_Fixup(string fileIn, string fileOut) {
        TestFile(fileIn, fileOut, srt => { srt.FixAll(); });
    }

    private void TestFile(string fileIn, string fileOut, Action<SrtFile> action) {
        var streamIn = GetStream(fileIn)!;
        var streamOut = GetStream(fileOut);

        var srtIn = SrtFile.Load(streamIn);
        action(srtIn);

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


    private static IEnumerable<(string, string)> BasicAssets {
        get { return GetFileAssets("Basic"); }
    }

    private static IEnumerable<(string, string)> CleanupAssets{
        get { return GetFileAssets("Cleanup"); }
    }

    private static IEnumerable<(string, string)> FixupAssets {
        get { return GetFileAssets("Fixup"); }
    }

    private static IEnumerable<(string, string)> GetFileAssets(string directory) {
        var foundAny = false;

        var resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames()!;
        foreach (var resName in resNames) {
            if (!resName.EndsWith(".srt", StringComparison.Ordinal)) { continue; }

            var parts = resName.Split('.');
            if (parts.Length != 6) { continue; }
            if (!parts[2].Equals(directory, StringComparison.Ordinal)) { continue; }
            if (!parts[3].Equals("In", StringComparison.Ordinal)) { continue; }

            foundAny = true;
            var outFile = string.Join('.', parts[0], parts[1], parts[2], "Out", parts[4], parts[5]);
            yield return (resName, outFile);
        }

        if (!foundAny) { throw new InvalidOperationException($"No documents found"); }
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
