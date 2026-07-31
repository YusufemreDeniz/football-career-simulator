using FootballCareerSimulator.Application.TeamPreparation.Queries;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// XI şeridi chip paneli — maç günü ve sonuç köprüsü ortak yüzeyi.
/// </summary>
internal static class LineupStripUi
{
    public static Control BuildPanel(MatchDayLineupStrip strip, string? captionOverride = null)
    {
        ArgumentNullException.ThrowIfNull(strip);

        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", CareerUiTheme.SoftPanel());

        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);

        var caption = new Label
        {
            Text = captionOverride ?? strip.Caption,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        CareerUiTheme.StyleBody(caption, muted: true);
        box.AddChild(caption);

        var xiStrip = new HFlowContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        xiStrip.AddThemeConstantOverride("h_separation", 6);
        xiStrip.AddThemeConstantOverride("v_separation", 6);
        box.AddChild(xiStrip);
        foreach (var chip in strip.StartingXi)
        {
            xiStrip.AddChild(BuildChip(chip));
        }

        if (strip.OutPlayers.Count > 0)
        {
            var outStrip = new HFlowContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            outStrip.AddThemeConstantOverride("h_separation", 6);
            outStrip.AddThemeConstantOverride("v_separation", 6);
            box.AddChild(outStrip);
            foreach (var chip in strip.OutPlayers)
            {
                outStrip.AddChild(BuildChip(chip));
            }
        }

        return panel;
    }

    private static Control BuildChip(MatchDayLineupChip chip)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride(
            "panel",
            CareerUiTheme.LineupChipPanel(chip.IsIn, chip.IsOut));
        var label = new Label { Text = chip.ChipLabel };
        CareerUiTheme.StyleLineupChip(label, chip.IsIn, chip.IsOut);
        panel.AddChild(label);
        return panel;
    }
}
