namespace ChSrt;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Ude;

/// <summary>
/// SubRip file
/// </summary>
public sealed record SrtFile {

    /// <summary>
    /// Creates a new SubRip file.
    /// </summary>
    public SrtFile() { }

    private SrtFile(List<SrtEntry> entries) {
        BackingEntries = entries;
    }


    private List<SrtEntry> BackingEntries = [];

    /// <summary>
    /// Gets the entries in the SubRip file.
    /// </summary>
    public IReadOnlyList<SrtEntry> Entries => BackingEntries.AsReadOnly();


    /// <summary>
    /// Fixes all issues in the SubRip file.
    /// </summary>
    public void FixAll() {
        FixTimeOrder();
        FixTimeOverlaps();
        FixIndices();
    }

    /// <summary>
    /// Updates entries with corrected indices.
    /// </summary>
    public void FixIndices() {
        var newEntries = new List<SrtEntry>();
        var index = 1;
        foreach (var entry in BackingEntries) {
            newEntries.Add(entry with { Index = index++ });
        }
        BackingEntries = newEntries;
    }

    /// <summary>
    /// Updates entries to be sorted by time.
    /// </summary>
    public void FixTimeOrder() {
        BackingEntries.Sort((a, b) => {
            var cmp = a.StartTime.CompareTo(b.StartTime);
            if (cmp == 0) { cmp = a.Index.CompareTo(b.Index); }
            return cmp;
        });
    }

    /// <summary>
    /// Fixes overlapping times.
    /// </summary>
    public void FixTimeOverlaps() {
        var newEntries = new List<SrtEntry>();
        for (var i = 0; i < BackingEntries.Count; i++) {
            var entry = BackingEntries[i];
            var currEnd = BackingEntries[i].EndTime;
            var nextStart = i + 1 < BackingEntries.Count ? BackingEntries[i + 1].StartTime : TimeSpan.MaxValue;
            if (currEnd > nextStart) {
                newEntries.Add(entry with { EndTime = nextStart });
            } else {
                newEntries.Add(entry);

            }
        }
        BackingEntries = newEntries;
    }

    /// <summary>
    /// Adjusts time of all entries.
    /// Entries that would have negative time are set to zero.
    /// </summary>
    /// <param name="timeAdjustment">Time adjustment</param>/
    public void AdjustTime(TimeSpan timeAdjustment) {
        var newEntries = new List<SrtEntry>();
        foreach (var entry in BackingEntries) {
            var newStart = entry.StartTime + timeAdjustment;
            if (newStart < TimeSpan.Zero) { newStart = TimeSpan.Zero; }
            var newEnd = entry.EndTime + timeAdjustment;
            if (newEnd < TimeSpan.Zero) { newEnd = TimeSpan.Zero; }
            newEntries.Add(entry with { StartTime = newStart, EndTime = newEnd });
        }
        BackingEntries = newEntries;
    }


    #region File

    /// <summary>
    /// Saves SubRip file to a stream.
    /// </summary>
    /// <param name="stream">Stream</param>
    public void Save(Stream stream) {
        Save(stream, "\r\n");
    }

    /// <summary>
    /// Saves SubRip file to a stream.
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="newLine">New line sequence.</param>
    public void Save(Stream stream, string newLine) {
        if (newLine is not "\r\n" and not "\n" and not "\r") { throw new ArgumentOutOfRangeException(nameof(newLine), "Unsupported EOL sequence"); }
        var newLineBytes = Utf8.GetBytes(newLine);
        foreach (var entry in BackingEntries) {
            var bytes = new List<byte>();
            bytes.AddRange(Utf8.GetBytes(entry.Index.ToString(CultureInfo.InvariantCulture)));
            bytes.AddRange(newLineBytes);
            bytes.AddRange(Utf8.GetBytes(entry.StartTime.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture) + " --> " + entry.EndTime.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture)));
            bytes.AddRange(newLineBytes);
            bytes.AddRange(Utf8.GetBytes(string.Join(newLine, entry.Lines)));
            bytes.AddRange(newLineBytes);
            bytes.AddRange(newLineBytes);
            stream.Write([.. bytes], 0, bytes.Count);
        }
    }

    /// <summary>
    /// Saves SubRip file from a reader.
    /// </summary>
    /// <param name="reader">Text reader.</param>
    public void Save(TextReader reader) {
        Save(reader, "\r\n");
    }

    /// <summary>
    /// Saves SubRip file from a reader.
    /// </summary>
    /// <param name="reader">Text reader.</param>
    /// <param name="newLine">New line sequence.</param>
    public void Save(TextReader reader, string newLine) {
        Save(reader, newLine);
    }

    /// <summary>
    /// Saves SubRip file from a file.
    /// </summary>
    /// <param name="filePath">Path to the UTF-8 encoded file.</param>
    public void Save(string filePath) {
        Save(filePath, "\r\n");
    }

    /// <summary>
    /// Saves SubRip file from a file.
    /// </summary>
    /// <param name="filePath">Path to the UTF-8 encoded file.</param>
    /// <param name="newLine">New line sequence.</param>
    public void Save(string filePath, string newLine) {
        using var stream = File.OpenWrite(filePath);
        stream.SetLength(0);
        Save(stream, newLine);
    }


    /// <summary>
    /// Loads SubRip file from a stream.
    /// </summary>
    /// <param name="stream">Stream</param>
    public static SrtFile Load(Stream stream) {
        return Load(stream, null);
    }

    /// <summary>
    /// Loads SubRip file from a stream.
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="encoding">Stream encoding.</param>
    public static SrtFile Load(Stream stream, Encoding? encoding) {
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        if (encoding == null) {  // try to auto-detect
            var detector = new CharsetDetector();
            detector.Feed(bytes, 0, bytes.Length);
            detector.DataEnd();

            if (detector.Charset != null) {  // auto-detected
                Debug.WriteLine($"Detected {detector.Charset} (confidence: {detector.Confidence})");
                encoding = Encoding.GetEncoding(detector.Charset);
                return Load(bytes, encoding);
            } else if (IsValidUtf8(bytes)) {  // valid UTF-8
                Debug.WriteLine("Detected UTF-8 (valid byte sequence)");
                return Load(bytes, Utf8);
            } else {  // default to Windows-1252
                Debug.WriteLine("Windows-1252 (defailt)");
                return Load(bytes, Encoding.GetEncoding(1252));
            }
        } else {  // use specified encoding
            return Load(bytes, encoding);
        }
    }

    /// <summary>
    /// Loads SubRip file from a reader.
    /// </summary>
    /// <param name="reader">Text reader.</param>
    public static SrtFile Load(TextReader reader) {
        return Load(reader);
    }

    /// <summary>
    /// Loads SubRip file from a reader.
    /// </summary>
    /// <param name="reader">Text reader.</param>
    /// <param name="encoding">Text reader encoding.</param>
    public static SrtFile Load(TextReader reader, Encoding? encoding) {
        return Load(reader, encoding);
    }

    /// <summary>
    /// Loads SubRip file from a file.
    /// </summary>
    /// <param name="filePath">Path to the UTF-8 encoded file.</param>
    public static SrtFile Load(string filePath) {
        return Load(filePath, null);
    }

    /// <summary>
    /// Loads SubRip file from a file.
    /// </summary>
    /// <param name="filePath">Path to the UTF-8 encoded file.</param>
    /// <param name="encoding">File encoding.</param>
    public static SrtFile Load(string filePath, Encoding? encoding) {
        using var stream = File.OpenRead(filePath);
        return Load(stream, encoding);
    }

    private static SrtFile Load(byte[] bytes, Encoding encoding) {
        var entries = new List<SrtEntry>();
        var lineChars = new List<char>();
        var lines = new List<string>();

        var chars = encoding.GetChars(bytes);
        var lastChar = '\0';
        foreach (var ch in chars) {
            var isNewLine = false;
            var isText = false;
            if (ch is '\r') {
                isNewLine = true;
            } else if (ch is '\n') {
                if (lastChar is not '\r') {
                    isNewLine = true;
                }
            } else {
                isText = true;
            }

            if (isNewLine) {
                if (lineChars.Count > 0) {
                    var line = new string([.. lineChars]);
                    lines.Add(line);
                    lineChars.Clear();
                } else {  // empty line
                    if (lines.Count >= 2) {
                        ParseLines(entries, lines);
                    }
                    lines.Clear();
                }
            } else if (isText) {
                lineChars.Add(ch);
            }

            lastChar = ch;
        }

        if (lines.Count >= 2) {
            ParseLines(entries, lines);
        }

        return new SrtFile(entries);
    }

    private static void ParseLines(List<SrtEntry> entries, List<string> lines) {
        if (!int.TryParse(lines[0], out var index)) { index = 0; }

        var timeParts = lines[1].Split("-->", StringSplitOptions.TrimEntries);
        if (timeParts.Length == 2) {
            if (TimeSpan.TryParseExact(timeParts[0], "hh\\:mm\\:ss\\,fff", null, out var timeStart)
                && TimeSpan.TryParseExact(timeParts[1], "hh\\:mm\\:ss\\,fff", null, out var timeEnd)) {

                var textLines = lines[2..];

                var entry = new SrtEntry(index, timeStart, timeEnd, textLines);
                entries.Add(entry);
            }
        }
    }

    private static bool IsValidUtf8(byte[] bytes) {
        try {
            var utf8 = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true
            );
            utf8.GetString(bytes);
            return true;
        } catch (DecoderFallbackException) {
            return false;
        }
    }

    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    #endregion File

}
