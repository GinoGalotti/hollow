using Godot;

public partial class FontCache : Node
{
    public static Font? CinzelBold    { get; private set; }
    public static Font? CinzelRegular { get; private set; }
    public static Font? IMFell        { get; private set; }
    public static Font? IMFellItalic  { get; private set; }

    public override void _Ready()
    {
        CinzelBold    = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Bold.ttf");
        CinzelRegular = GD.Load<Font>("res://godot/assets/fonts/Cinzel-Regular.ttf");
        IMFell        = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Regular.ttf");
        IMFellItalic  = GD.Load<Font>("res://godot/assets/fonts/IMFellEnglish-Italic.ttf");
    }
}
