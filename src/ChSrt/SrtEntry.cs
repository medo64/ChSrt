namespace ChSrt;
using System;
using System.Collections.Generic;

/// <summary>
/// One SubRip subtitle entry.
/// </summary>
public sealed record SrtEntry {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="index">Index.</param>
    /// <param name="startTime">Start time of the subtitle.</param>
    /// <param name="endTime">End time of the subtitle.</param>
    /// <param name="lines">Text lines.</param>
    public SrtEntry(int index, TimeSpan startTime, TimeSpan endTime, IEnumerable<string> lines) {
        Index = index;
        StartTime = startTime;
        EndTime = endTime;
        BackingLines = new List<string>(lines);
    }


    private List<string> BackingLines = [];


    /// <summary>
    /// Entry index.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Start time of the subtitle.
    /// </summary>
    public TimeSpan StartTime { get; init; }

    /// <summary>
    /// End time of the subtitle.
    /// </summary>
    public TimeSpan EndTime { get; init; }


    /// <summary>
    /// Gets the text lines.
    /// </summary>
    public IReadOnlyList<string> Lines => BackingLines.AsReadOnly();

}
