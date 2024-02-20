﻿using Tkmm.Core.Generics;
using Tkmm.Core.Helpers;
using Tkmm.Core.Services;

namespace Tkmm.Core.Components.Mergers;

public class MalsMergerShell : IMerger
{
    public Task Merge(IModItem[] mods, string output)
    {
        return ToolHelper.Call(Tool.MalsMerger,
            "merge", string.Join('|', mods.Select(x => Path.Combine(x.SourceFolder, "romfs"))),
            Path.Combine(output, "romfs"),
            "--target", Config.Shared.GameLanguage
        ).WaitForExitAsync();
    }
}