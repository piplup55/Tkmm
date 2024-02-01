﻿using Cocona;
using Tkmm.Core.Services;

namespace Tkmm.Core.Commands;

[HasSubCommands(typeof(ModCommands), "mods", Description = "Mod commands")]
public class GeneralCommands
{
    [Command("merge", Description = "Merge the mods into an output folder")]
    public async Task Merge([Option("output", ['o'])] string? output)
    {
        await MergerService.Merge();
    }
}
