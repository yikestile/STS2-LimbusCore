using Godot;

namespace LimbusCore.LimbusCoreCode.Overlays;

public partial class NShinEffect : Node2D
{
    public override void _Ready()
    {
        Visible = false;
    }

    public void UpdateShinVisibility(bool hasShin)
    {

        if (Visible != hasShin)
        {
            Visible = hasShin;
        }
    }
}