using Godot;

namespace LimbusCore.LimbusCoreCode.Overlays;

public partial class NShinEffect : Node2D
{
    private Sprite2D? _effectSprite;
    private GpuParticles2D? _particles;

    public override void _Ready()
    {
        Visible = false;
        _effectSprite = GetNodeOrNull<Sprite2D>("ShinEffect");
        _particles = GetNodeOrNull<GpuParticles2D>("Sparkle");

        if (_effectSprite != null) _effectSprite.Centered = false;
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            ApplyGroundingOffset();
        }
    }

    private void ApplyGroundingOffset()
    {
        if (_effectSprite == null || _effectSprite.Texture == null) return;

        Vector2 texSize = _effectSprite.Texture.GetSize();
        Vector2 groundingOffset = new Vector2(-texSize.X / 2f, -texSize.Y);

        _effectSprite.Offset = groundingOffset;

        if (_particles != null)
        {
            _particles.Position = groundingOffset + (texSize / 2f);
        }
    }

    public void UpdateShinVisibility(bool hasShin)
    {
        if (Visible != hasShin)
        {
            Visible = hasShin;
        }
    }
}