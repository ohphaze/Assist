using System;

namespace Assist.Core.Models;

public sealed class ModuleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; init; }

    public string? Description { get; init; }

    public ModuleCategory Category { get; init; } = ModuleCategory.Utility;

    public bool IsEnabled { get; set; }
}

public enum ModuleCategory
{
    Utility,
    Social,
    Gameplay,
    Integrations
}
