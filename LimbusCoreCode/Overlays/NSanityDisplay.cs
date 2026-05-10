using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace LimbusCore.LimbusCoreCode.Mechanics;

[GlobalClass]
public partial class NSanityDisplay : Control
{
    [Export] public Gradient? SanityGradient;

    private RichTextLabel? _spValueLabel;
    private TextureRect? _icon;
    private Sprite2D? _ripple;

    private Player? _currentPlayer; 
    private bool _isListeningToSanityChanged; 

    private float _currentSP = 0f;
    private double _rippleTimer = 0f;
    private HoverTip? _hoverTip;

    public override void _Ready()
    {
        var marginContainer = GetNodeOrNull<MarginContainer>("MarginContainer");
        _spValueLabel = marginContainer?.GetNodeOrNull<RichTextLabel>("SPValueLabel");
        _icon = GetNodeOrNull<TextureRect>("TextureRect"); 
        _ripple = GetNodeOrNull<Sprite2D>("RippleEffect");

        if (marginContainer == null || _spValueLabel == null)
        {
             return;
        }

        marginContainer.MouseFilter = MouseFilterEnum.Ignore;
        _spValueLabel.MouseFilter = MouseFilterEnum.Ignore;
        if (_icon != null) _icon.MouseFilter = MouseFilterEnum.Ignore;
        
        marginContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _spValueLabel.FitContent = true; 
        _spValueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _spValueLabel.VerticalAlignment = VerticalAlignment.Center;

        _hoverTip = new HoverTip(
            new LocString("static_hover_tips", "LIMBUS_SANITY.title"), 
            new LocString("static_hover_tips", "LIMBUS_SANITY.description")
        );

        MouseFilter = MouseFilterEnum.Pass;
        Connect(SignalName.MouseEntered, Callable.From(OnHovered));
        Connect(SignalName.MouseExited, Callable.From(OnUnhovered));

        UpdateSanityDisplay(0f);
    }

    private void OnHovered()
    {
        NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
        
        Vector2 tooltipOffset = new Vector2(0f, -325f); 
        nHoverTipSet.GlobalPosition = base.GlobalPosition + tooltipOffset;
        
        nHoverTipSet.MouseFilter = MouseFilterEnum.Ignore;
    }

    private void OnUnhovered()
    {
        NHoverTipSet.Remove(this);
    }

    public override void _Process(double delta)
    {
        if (_currentSP <= -45f)
        {
            _rippleTimer += delta;
            if (_rippleTimer >= 0.8)
            {
                _rippleTimer = 0f;
                PlayRippleEffect(_currentSP, 0.6f);
            }
        }
        else if (_currentSP >= 45f)
        {
            _rippleTimer += delta;
            if (_rippleTimer >= 1.2)
            {
                _rippleTimer = 0f;
                PlayRippleEffect(_currentSP, 0.8f);
            }
        }
        else
        {
            _rippleTimer = 0f; 
        }
    }

    public void Initialize(Player? player)
    {
        if (_currentPlayer != null && _isListeningToSanityChanged)
        {
            SanityManager.GetData(_currentPlayer).OnSanityChanged -= OnSanityChanged;
            _isListeningToSanityChanged = false;
        }

        _currentPlayer = player;
        if (_currentPlayer != null)
        {
            SanityManager.GetData(_currentPlayer).OnSanityChanged += OnSanityChanged;
            _isListeningToSanityChanged = true;
            _currentSP = SanityManager.GetSanity(_currentPlayer);
            UpdateSanityDisplay(_currentSP);
        }
    }

    private void OnSanityChanged(float newSP)
    {
        if (!Mathf.IsEqualApprox(_currentSP, newSP))
        {
            _currentSP = newSP;
            UpdateSanityDisplay(newSP);
            
            PlayRippleEffect(newSP, 0.6f);
        }
    }

    private void UpdateSanityDisplay(float newSP)
    {
        Color orbColor = Colors.White;
        Color labelColor = Colors.White;

        if (SanityGradient != null)
        {
            float weight = Mathf.Clamp((newSP + 45f) / 90f, 0f, 1f);
            orbColor = SanityGradient.Sample(weight);

            if (newSP < 0f)
            {
                Color maxPanicRed = Color.FromHsv(0f, 1f, 1f, 1f); 
                float negativeIntensity = Mathf.Clamp(newSP / -45f, 0f, 1f);
                labelColor = Colors.White.Lerp(maxPanicRed, negativeIntensity * 0.8f);
            }
            else
            {
                labelColor = Colors.White.Lerp(orbColor, 0.15f);
            }
        }

        if (_icon != null) _icon.SelfModulate = orbColor;
        if (_spValueLabel != null)
        {
            _spValueLabel.Text = $"[center]{newSP:F0}[/center]";
            _spValueLabel.AddThemeColorOverride("default_color", labelColor);
        }
    }

    private void PlayRippleEffect(float currentSP, float duration)
    {
        if (_ripple == null) return;

        Color rippleColor = (currentSP < 0) ? new Color(2f, 0.2f, 0.2f) : new Color(0.2f, 2f, 2f);

        _ripple.Modulate = rippleColor;
        _ripple.Scale = new Vector2(0.4f, 0.4f);
        _ripple.SelfModulate = new Color(1, 1, 1, 1f);

        var material = _ripple.Material as ShaderMaterial;
        material?.SetShaderParameter("dissolve_value", 0.0f);

        var tween = GetTree().CreateTween();
        tween.SetParallel(true);
    
        tween.TweenProperty(_ripple, "scale", new Vector2(0.85f, 0.85f), duration)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.Out);
     
        tween.TweenProperty(_ripple, "self_modulate:a", 0f, duration)
            .SetTrans(Tween.TransitionType.Linear);

        if (material != null)
        {
            float startDelay = duration * 0.35f;
            float dissolveTime = duration - startDelay;

            var dissolveChain = GetTree().CreateTween();
            dissolveChain.TweenInterval(startDelay);
            dissolveChain.TweenProperty(material, "shader_parameter/dissolve_value", 1.0f, dissolveTime)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
        }
    }

    public override void _ExitTree()
    {
        if (_currentPlayer != null && _isListeningToSanityChanged)
        {
            SanityManager.GetData(_currentPlayer).OnSanityChanged -= OnSanityChanged;
        }
    }
}