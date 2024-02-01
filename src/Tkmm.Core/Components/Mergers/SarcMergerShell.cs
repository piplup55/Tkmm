﻿using Tkmm.Core.Generics;
using Tkmm.Core.Helpers;
using Tkmm.Core.Services;

namespace Tkmm.Core.Components.Mergers;

public class SarcMergerShell : IMerger
{
    public Task Merge(IModItem[] mods)
    {
        return ToolHelper.Call(Tool.SarcTool, [
            "merge",
            "--base", ProfileManager.ModsFolder,
            "--mods", .. mods.Select(x => Path.GetRelativePath(ProfileManager.ModsFolder, x.SourceFolder)),
            "--process", "All",
            "--output", Path.Combine(Config.Shared.MergeOutput, "romfs")
        ]).WaitForExitAsync();
    }
}
