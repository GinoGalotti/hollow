using Godot;
using HollowWardens.Core.Models;

/// <summary>
/// Shows all 6 element counters with threshold markers at 4, 7, 11.
/// One row per element.
/// </summary>
public partial class ElementTrackerController : VBoxContainer
{
    private readonly Label[] _labels = new Label[6];

    public override void _Ready()
    {
        AddChild(new Label { Text = "── Elements ──", Modulate = Colors.Yellow });

        for (int i = 0; i < 6; i++)
        {
            var e = (Element)i;
            var row = new HBoxContainer();
            var nameLabel = new Label
            {
                Text = e.ToString()[..3],
                CustomMinimumSize = new Vector2(36, 0)
            };
            _labels[i] = new Label { Text = "0  [----:----:----]" };
            row.AddChild(nameLabel);
            row.AddChild(_labels[i]);
            AddChild(row);
        }

        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        bridge.ElementChanged     += OnElementChanged;
        bridge.ThresholdTriggered += OnThresholdTriggered;
        bridge.ElementsDecayed    += Refresh;
        bridge.TurnStarted        += Refresh;

        Refresh();
    }

    private void OnElementChanged(int element, int value) => UpdateRow((Element)element);
    private void OnThresholdTriggered(int element, int tier) => UpdateRow((Element)element);
    private void Refresh() { for (int i = 0; i < 6; i++) UpdateRow((Element)i); }

    private void UpdateRow(Element element)
    {
        var bridge = GameBridge.Instance;
        if (bridge == null) return;

        int val = bridge.State.Elements?.Get(element) ?? 0;
        int idx = (int)element;

        // Build bar: markers at 4, 7, 11 over a 13-char bar
        char[] bar = new char[13];
        for (int b = 0; b < 13; b++)
            bar[b] = b < val ? '#' : '-';

        // Threshold separators at positions 4, 7, 11 → indices 4, 7, 11
        if (bar.Length > 4)  bar[4]  = bar[4]  == '#' ? '|' : ':';
        if (bar.Length > 7)  bar[7]  = bar[7]  == '#' ? '|' : ':';
        if (bar.Length > 11) bar[11] = bar[11] == '#' ? '|' : ':';

        _labels[idx].Text = $"{val,2}  [{new string(bar)}]";

        // Highlight at or above tier 1
        _labels[idx].Modulate = val >= 4 ? Colors.LightGreen : Colors.White;
    }
}
