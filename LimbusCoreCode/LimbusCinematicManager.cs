using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Relics;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Debug;

namespace LimbusCore.LimbusCoreCode
{
    
    public static class LimbusCinematicManager
    {
        private static float _charStartGlobalX;
        private static bool _isBackgroundParallaxActive = false;
        private static Vector2 _bgStartGlobalPos;
        private static Vector2 _vfxStartGlobalPos;

        private static CanvasLayer _cinematicLayer;
        private static Control _topBox;
        private static Control _bottomBox;
        private static ColorRect _topInk;
        private static ColorRect _bottomInk;

        public static bool IsUiHidden { get; private set; } = false;
        public static bool IsUiPendingShow { get; private set; } = false;

        private const float BoxHeight = 180f;
        private const float AnimDuration = 0.25f;
        private static float _activityLevel = 0f;
        private static float _uTime = 0f;
        private static float _lastReboundPos = 0f;
        private static float _windupTimer = 0f;
        private static float _activityDelayTimer = 0f;

        private static readonly FieldInfo _isDebugHiddenField = typeof(NCombatUi).GetField("_isDebugHidden", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo _isDebugHidingHandField = typeof(NCombatUi).GetField("_isDebugHidingHand", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly PropertyInfo _isDebugHidingPlayContainerProp = typeof(NCombatUi).GetProperty("IsDebugHidingPlayContainer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly PropertyInfo _isDebugHidingIntentProp = typeof(NCombatUi).GetProperty("IsDebugHidingIntent", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly PropertyInfo _isDebugHidingHpBarProp = typeof(NCombatUi).GetProperty("IsDebugHidingHpBar", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo _debugToggleIntentField = typeof(NCombatUi).GetField("DebugToggleIntent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _debugToggleHpBarField = typeof(NCombatUi).GetField("DebugToggleHpBar", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _topBarDebugHiddenField = typeof(NTopBar).GetField("_isDebugHidden", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _relicsDebugHiddenField = typeof(NRelicInventory).GetField("_isDebugHidden", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _cardPlayQueueListField = typeof(NCardPlayQueue).GetField("_playQueue", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _releaseInfoField = typeof(NDebugInfoLabelManager).GetField("_releaseInfo", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _moddedWarningField = typeof(NDebugInfoLabelManager).GetField("_moddedWarning", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _seedField = typeof(NDebugInfoLabelManager).GetField("_seed", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Node _cachedDebugManager;
        
        private static readonly HashSet<ulong> ProcessedNodes = new HashSet<ulong>();
        private static readonly Dictionary<ulong, Vector2> OriginalPositions = new Dictionary<ulong, Vector2>();
        private static string _lastRoomName = "";

        private static readonly string[] ExcludePatterns = {
            "a_foliage_", "b_foliage_", "c_foliage_",
            "little_light_", "medium_light_", "light",
            "water_reflection", "stars", "fog", 
            "cutter_b_", "beads_", "fire", "torch", "ash"
        };

        private static bool IsIncluded(string name)
        {
            return name == "A" || name == "B" || name == "C" || name == "Water" || name == "Foreground";
        }

        private static bool IsExcluded(Node node)
        {
            if (node == null) return false;
            string name = node.Name.ToString().ToLower();
            string type = node.GetType().Name;

            if (node is CpuParticles2D || node is GpuParticles2D || type.Contains("Particles") || type.Contains("Spine")) return true;

            foreach (var pattern in ExcludePatterns)
            {
                if (name.Contains(pattern)) return true;
            }
            return false;
        }
        
        public static void PreloadBackground()
        {
            // CONFIG CHECK: Extended Backgrounds
            if (!LimbusCoreConfig.EnableMapExtension) return;

            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            var bg = combatRoom.GetNodeOrNull<Control>("%BgContainer");
            var vfx = combatRoom.GetNodeOrNull<Node2D>("%BackCombatVfxContainer");

            if (bg != null)
            {
                if (bg.GetChildCount() == 0)
                {
                    return; 
                }

                string currentRoomName = bg.GetParent()?.Name.ToString() ?? "Unknown";
        
                if (_lastRoomName == currentRoomName && ProcessedNodes.Count > 5) 
                {
                    return;
                }
                InitializeBackground(bg, vfx);

                _bgStartGlobalPos = bg.GlobalPosition;
                if (vfx != null) _vfxStartGlobalPos = vfx.GlobalPosition;
            }
        }

        public static void InitializeBackground(Control bgContainer, Node2D vfxContainer)
        {
            if (bgContainer == null) return;

            string currentRoomName = bgContainer.GetParent().Name.ToString();
            if (_lastRoomName != currentRoomName)
            {
                ProcessedNodes.Clear();
                OriginalPositions.Clear();
                _lastRoomName = currentRoomName;
            }

            const float mirrorScale = 3.0f;
            var shaderPath = "res://LimbusCore/scenes/MirrorBG.gdshader";

            SnapshotGlobalPositions(bgContainer);
            if (vfxContainer != null) SnapshotGlobalPositions(vfxContainer);

            ApplyMirrorToAllChildren(bgContainer, mirrorScale, shaderPath, false);
            if (vfxContainer != null) ApplyMirrorToAllChildren(vfxContainer, mirrorScale, shaderPath, false);
        }

        private static void SnapshotGlobalPositions(Node root)
        {
            if (root == null) return;
            
            if (IsExcluded(root))
            {
                if (root is Node2D n2d) OriginalPositions[root.GetInstanceId()] = n2d.GlobalPosition;
                else if (root is Control ctrl) OriginalPositions[root.GetInstanceId()] = ctrl.GlobalPosition;
            }

            foreach (Node child in root.GetChildren())
            {
                SnapshotGlobalPositions(child);
            }
        }
        
        private static int GetTargetZIndex(Node node)
        {
            string name = node.Name.ToString().ToLower();
    
            //SPECIFIC EXCEPTION:
            if (name.Contains("water_light")) 
            {
                return 0;
            }

            if (name.Contains("fire") || name.Contains("stars") || name.Contains("light") || name.Contains("water_reflection")) 
            {
                return 1;
            }

            return 0; 
        }

        private static void ApplyMirrorToAllChildren(Node root, float scale, string shaderPath, bool parentWasTargeted)
        {
            if (root == null) return;
            ulong id = root.GetInstanceId();
            if (ProcessedNodes.Contains(id)) return;

            bool isProp = IsExcluded(root);
            bool isTargetArt = IsIncluded(root.Name.ToString());

            //1. NEUTRALIZATION
            if (parentWasTargeted && isProp)
            {
                float invScale = 1.0f / scale;
                if (OriginalPositions.TryGetValue(id, out Vector2 oldPos))
                {
                    int targetZ = GetTargetZIndex(root);

                    if (root is Node2D n2d)
                    {
                        n2d.Scale = new Vector2(invScale, 1.0f);
                        n2d.GlobalPosition = oldPos;
                        n2d.ZIndex = targetZ; 
                    }
                    else if (root is Control ctrl)
                    {
                        ctrl.Scale = new Vector2(invScale, 1.0f);
                        ctrl.GlobalPosition = oldPos;
                        if (ctrl is CanvasItem ci) ci.ZIndex = targetZ;
                    }
                }
                ProcessedNodes.Add(id);
                return; 
            }

            //2. SCALING
            bool thisNodeIsNowTarget = parentWasTargeted;

            if (isTargetArt && !parentWasTargeted)
            {
                if (root is CanvasItem ci)
                {
                    if (root is Control ctrl)
                    {
                        ctrl.PivotOffset = ctrl.Size / 2.0f;
                        ctrl.Scale = new Vector2(scale, 1.0f);
                    }
                    else if (root is Node2D n2d)
                    {
                        n2d.Scale = new Vector2(scale, 1.0f);
                    }

                    ApplyMirrorSettings(ci, scale, shaderPath);
                    
                    foreach (Node child in root.GetChildren())
                    {
                        if (child is TextureRect tr && !IsExcluded(child))
                        {
                            ApplyMirrorSettings(tr, scale, shaderPath);
                        }
                    }
                    thisNodeIsNowTarget = true;
                }
            }

            ProcessedNodes.Add(id);
            foreach (Node child in root.GetChildren())
            {
                ApplyMirrorToAllChildren(child, scale, shaderPath, thisNodeIsNowTarget);
            }
        }

        private static void ApplyMirrorSettings(CanvasItem node, float scale, string shaderPath)
        {
            var shader = GD.Load<Shader>(shaderPath);
            if (shader != null)
            {
                var mat = new ShaderMaterial { Shader = shader };
                mat.SetShaderParameter("mirror_count", scale);
                node.Material = mat;
            }
        }

        public static void StartBackgroundParallax(float charGlobalX)
        {
            if (_isBackgroundParallaxActive) return;

            // CONFIG CHECK: Parallax Movement
            if (!LimbusCoreConfig.EnableParallax) return;

            var bg = NCombatRoom.Instance?.GetNodeOrNull<Control>("%BgContainer");
            var vfx = NCombatRoom.Instance?.GetNodeOrNull<Node2D>("%BackCombatVfxContainer");

            if (bg != null) 
            {
              InitializeBackground(bg, vfx);
              _bgStartGlobalPos = bg.GlobalPosition;
            }

            if (vfx != null) _vfxStartGlobalPos = vfx.GlobalPosition;

            _charStartGlobalX = charGlobalX;
            _isBackgroundParallaxActive = true;
        }

        public static void UpdateCinematic(float charGlobalX)
        {
            if (!_isBackgroundParallaxActive || NCombatRoom.Instance == null) return;
            ApplyBackgroundPosition(0f); 
        }

        private static void ApplyBackgroundPosition(float offset)
        {
            var bg = NCombatRoom.Instance?.GetNodeOrNull<Control>("%BgContainer");
            var vfx = NCombatRoom.Instance?.GetNodeOrNull<Node2D>("%BackCombatVfxContainer");
            if (bg != null) bg.GlobalPosition = _bgStartGlobalPos;
            if (vfx != null) vfx.GlobalPosition = _vfxStartGlobalPos;
        }

        public static void EndBackgroundParallax()
        {
            if (!_isBackgroundParallaxActive) return;
            _isBackgroundParallaxActive = false;
            ApplyBackgroundPosition(0f);
        }
        
        public static bool IsQueueClear()
        {
            var playQueue = NCardPlayQueue.Instance;
            if (playQueue != null)
            {
                var list = _cardPlayQueueListField?.GetValue(playQueue) as IList;
                if (list != null && list.Count > 0) return false;
            }
            return RunManager.Instance?.ActionQueueSet?.IsEmpty ?? true;
        }

        public static void UpdateShaderIntensity(bool isAttacking, double delta)
        {
            if (_topInk == null || !GodotObject.IsInstanceValid(_topInk)) return;

            float fDelta = (float)delta;

            float rampSpeed = isAttacking ? 5.0f : 3.0f; 
            _activityLevel = Mathf.MoveToward(_activityLevel, isAttacking ? 1.0f : 0.0f, fDelta * rampSpeed);

            if (isAttacking) {
                _windupTimer += fDelta;
                _activityDelayTimer = 0.2f;
            } else {
                _windupTimer = 0f;
                _activityDelayTimer = Mathf.Max(0, _activityDelayTimer - fDelta);
            }

            float windupThreshold = 0.3f; 
            float windupFactor = Mathf.Clamp(_windupTimer / windupThreshold, 0.0f, 1.0f);

            float reboundActivity = (_activityDelayTimer > 0) ? 1.0f : _activityLevel;
            float targetReboundPos = reboundActivity * 1.0f;

            float smoothedReboundPos = Mathf.Lerp(_lastReboundPos, targetReboundPos, fDelta * 2.0f);

            float reboundVelocity = (smoothedReboundPos - _lastReboundPos) / fDelta;
            _lastReboundPos = smoothedReboundPos;
            
            float baseScrollSpeed = Mathf.Lerp(1.0f, 3.0f, _activityLevel * windupFactor);
            
            _uTime += (baseScrollSpeed + reboundVelocity) * fDelta;

            UpdateInkMaterial(_topInk, _activityLevel, _uTime);
            UpdateInkMaterial(_bottomInk, _activityLevel, _uTime);
        }

        private static void UpdateInkMaterial(ColorRect rect, float activity, float time)
        {
            if (rect?.Material is ShaderMaterial mat)
            {
                mat.SetShaderParameter("activity", activity);
                mat.SetShaderParameter("u_time", time);
            }
        }
        
        public static void UpdateBorders(float currentDebounceTimer, float maxDebounceTime, bool isNearNeutral)
        {
            if (!IsUiPendingShow || !IsUiHidden) return;
            
            if (currentDebounceTimer <= 0.25f && isNearNeutral)
            {
                ToggleCinematicBorders(false);
            }
        }

        public static void ConfirmUiShow()
        {
            if (!IsUiPendingShow) return;
            IsUiPendingShow = false;
            
            ToggleCinematicBorders(false);
            SetUiVisibility(true);
        }

        public static void StartUiHide() { if (!IsUiHidden) SetUiVisibility(false); IsUiPendingShow = false; }
        public static void EndUiSequence() { IsUiPendingShow = true; }
        public static void PrepareUiShow() { if (IsUiHidden) ToggleCinematicBorders(false); }

        private static void SetUiVisibility(bool visible)
        {
            // CONFIG CHECK: UI Suppression
            // Skip hiding if disabled, but ALWAYS allow showing
            if (!visible && !LimbusCoreConfig.EnableUiSuppression) return;

            var combatUi = NCombatRoom.Instance?.Ui;
            var globalUi = NRun.Instance?.GlobalUi;
            if (combatUi == null || globalUi == null) return;

            bool hide = !visible;
            Color targetColor = visible ? Colors.White : Colors.Transparent;
            
            _isDebugHiddenField?.SetValue(null, hide);
            _isDebugHidingHandField?.SetValue(null, hide);
            _isDebugHidingPlayContainerProp?.SetValue(null, hide);
            _isDebugHidingIntentProp?.SetValue(null, hide);
            _isDebugHidingHpBarProp?.SetValue(null, hide);
            (_debugToggleIntentField?.GetValue(combatUi) as Action)?.Invoke();
            (_debugToggleHpBarField?.GetValue(combatUi) as Action)?.Invoke();

            foreach (var child in combatUi.GetChildren().OfType<Control>())
                child.Modulate = targetColor;

            if (globalUi.TopBar != null)
            {
                _topBarDebugHiddenField?.SetValue(globalUi.TopBar, hide);
                globalUi.TopBar.Modulate = targetColor;
            }

            if (globalUi.RelicInventory != null)
            {
                _relicsDebugHiddenField?.SetValue(globalUi.RelicInventory, hide);
                globalUi.RelicInventory.Modulate = targetColor;
            }

            if (_cachedDebugManager == null || !GodotObject.IsInstanceValid(_cachedDebugManager))
                _cachedDebugManager = FindNodeByScript<NDebugInfoLabelManager>(NGame.Instance);

            if (_cachedDebugManager != null)
            {
                (_releaseInfoField?.GetValue(_cachedDebugManager) as Control)?.SetIndexed("modulate", targetColor);
                (_moddedWarningField?.GetValue(_cachedDebugManager) as Control)?.SetIndexed("modulate", targetColor);
                (_seedField?.GetValue(_cachedDebugManager) as Control)?.SetIndexed("modulate", targetColor);
            }
            
            IsUiHidden = hide;
            ToggleCinematicBorders(!visible);
        }

        private static void InitializeBorders()
        {
            if (_cinematicLayer != null && GodotObject.IsInstanceValid(_cinematicLayer) && _cinematicLayer.IsInsideTree()) return;
            _cinematicLayer = new CanvasLayer { Layer = 100 };
            NCombatRoom.Instance.AddChild(_cinematicLayer);
            _topBox = CreateCinematicBox(true);
            _bottomBox = CreateCinematicBox(false);
            _cinematicLayer.AddChild(_topBox);
            _cinematicLayer.AddChild(_bottomBox);
        }

        private static Control CreateCinematicBox(bool isTop)
        {
            var container = new Control();
            container.SetAnchorsPreset(isTop ? Control.LayoutPreset.TopWide : Control.LayoutPreset.BottomWide);
            container.CustomMinimumSize = new Vector2(0, BoxHeight);
            container.MouseFilter = Control.MouseFilterEnum.Ignore;
        
            var proceduralRect = new ColorRect(); 
            proceduralRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        
            var mat = new ShaderMaterial { Shader = GD.Load<Shader>("res://LimbusCore/scenes/ScrollingInk.gdshader") };
            mat.SetShaderParameter("is_top", isTop);
            mat.SetShaderParameter("ink_color", new Color(0.996f, 0.933f, 0.682f, 1.0f));    
            proceduralRect.Material = mat;
        
            container.AddChild(proceduralRect);
            if (isTop) _topInk = proceduralRect; else _bottomInk = proceduralRect;

            float viewportHeight = NCombatRoom.Instance.GetViewportRect().Size.Y;
            container.Position = new Vector2(0, isTop ? -BoxHeight : viewportHeight);
        
            return container;
        }

        private static void ToggleCinematicBorders(bool show)
        {
            // CONFIG CHECK: Ink Borders
            // We skip showing if disabled, but ALWAYS allow hiding (to clean up)
            if (show && !LimbusCoreConfig.EnableInkBorders) return;

            InitializeBorders();
            var viewportHeight = _cinematicLayer.GetViewport().GetVisibleRect().Size.Y;
            var tween = _cinematicLayer.CreateTween().SetParallel(true);
            tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(_topBox, "position:y", show ? 0 : -BoxHeight, AnimDuration);
            tween.TweenProperty(_bottomBox, "position:y", show ? viewportHeight - BoxHeight : viewportHeight, AnimDuration);
        }

        private static Node FindNodeByScript<T>(Node root) where T : Node
        {
            if (root == null) return null;
            if (root is T) return root;
            foreach (var child in root.GetChildren())
            {
                var res = FindNodeByScript<T>(child);
                if (res != null) return res;
            }
            return null;
        }
    }
}
