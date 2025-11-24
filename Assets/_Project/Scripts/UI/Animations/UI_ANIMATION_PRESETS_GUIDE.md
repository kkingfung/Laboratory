# UI Animation Presets System

Designer-friendly system for creating and applying consistent UI animations across Project Chimera.

---

## Overview

The UI Animation Presets system allows designers to:
- Create reusable animation configurations
- Apply consistent animations across multiple UI elements
- Quickly test and iterate on animation feel
- Switch between animation styles without touching code

---

## Components

### 1. **UIAnimationPreset** (ScriptableObject)
Stores animation settings that can be applied to animation components.

**Create a Preset:**
1. Right-click in Project window
2. Create → Chimera → UI → Animation Preset
3. Name it descriptively (e.g., "FastPopup", "SmoothSlide")
4. Configure settings in Inspector

**Preset Settings:**
- **Transition Type**: Fade, Slide, Scale, or combinations
- **Durations**: Show and hide animation lengths
- **Easing**: Animation curves (OutBack, InCubic, etc.)
- **Button Animations**: Hover, press, and click effects
- **Colors**: Optional hover color changes

### 2. **UIAnimationPresetApplicator** (Component)
Applies presets to UI animation components.

**Usage:**
1. Add to GameObject with UITransitionAnimator or UIButtonAnimator
2. Assign a preset in Inspector
3. Component auto-applies preset when changed
4. Can apply at runtime with `SetPreset(preset)`

### 3. **Pre-made Presets**
The system includes built-in presets for common patterns:
- **Fast_Popup**: Quick scale animation with back easing (0.2s)
- **Smooth_SlideIn**: Gentle slide from bottom (0.4s)
- **Quick_Fade**: Rapid fade transition (0.25s)
- **Bouncy_Scale**: Playful bounce effect (0.5s)

---

## Quick Start

### Example 1: Apply Preset to Menu Panel

```
1. Select your menu panel GameObject
2. Add Component → UI → UITransitionAnimator
3. Add Component → UI → UIAnimationPresetApplicator
4. Drag "FastPopup" preset into Preset field
5. ✅ Panel now animates with preset settings
```

### Example 2: Apply Preset to Button

```
1. Select button GameObject
2. Add Component → UI → UIButtonAnimator
3. Add Component → UI → UIAnimationPresetApplicator
4. Drag "SmoothSlide" preset into Preset field
5. ✅ Button now has smooth hover/press animations
```

### Example 3: Create Custom Preset

```
1. Create → Chimera → UI → Animation Preset
2. Name: "DialogBox_Preset"
3. Configure:
   - Transition: FadeSlideIn
   - Show Duration: 0.35s
   - Show Ease: OutCubic
   - Slide Direction: Bottom
4. Drag preset onto dialog GameObjects
5. ✅ All dialogs use consistent animation
```

---

## Common Patterns

### Pattern 1: **Menu Panels**
```
Preset Settings:
- Transition: FadeSlideIn
- Show Duration: 0.4s
- Ease: OutCubic
- Slide Direction: Bottom
```
**Use Case:** Main menu, settings panel, inventory screen

### Pattern 2: **Popups & Dialogs**
```
Preset Settings:
- Transition: FadeScale
- Show Duration: 0.3s
- Ease: OutBack
- Start Scale: 0.8
```
**Use Case:** Confirmation dialogs, tooltips, context menus

### Pattern 3: **Tooltips**
```
Preset Settings:
- Transition: Fade
- Show Duration: 0.15s
- Ease: Linear
```
**Use Case:** Hover tooltips, info bubbles

### Pattern 4: **Action Buttons**
```
Preset Settings:
- Hover Scale: 1.15
- Hover Ease: OutBack
- Press Scale: 0.92
- Click Punch: Enabled
```
**Use Case:** Primary action buttons, call-to-action buttons

### Pattern 5: **Icon Buttons**
```
Preset Settings:
- Hover Scale: 1.1
- Hover Duration: 0.15s
- Press Scale: 0.95
- Click Punch: Disabled
```
**Use Case:** Small icon buttons, toolbar buttons

---

## Runtime Usage

### Change Preset at Runtime
```csharp
using Laboratory.UI.Animations;

public class MenuController : MonoBehaviour
{
    [SerializeField] private UIAnimationPresetApplicator panelApplicator;
    [SerializeField] private UIAnimationPreset fastPreset;
    [SerializeField] private UIAnimationPreset smoothPreset;

    public void SetFastMode()
    {
        panelApplicator.SetPreset(fastPreset);
    }

    public void SetSmoothMode()
    {
        panelApplicator.SetPreset(smoothPreset);
    }
}
```

### Create Runtime Preset
```csharp
using Laboratory.UI.Animations;

var dynamicPreset = UIAnimationPreset.CreateRuntimePreset(
    "DynamicFade",
    TransitionType.Fade,
    0.25f
);

applicator.SetPreset(dynamicPreset);
```

---

## Advanced Features

### Auto-Apply on Load
UIAnimationPresetApplicator can automatically apply presets when the scene loads:
- Enable "Auto Apply On Load" in Inspector
- Preset applies in Start() during gameplay

### Multi-Component Application
One preset can apply to multiple components:
- Check "Apply To Transition Animator" for panel animations
- Check "Apply To Button Animator" for button animations
- Single preset configures both

### Context Menu Utilities
Right-click UIAnimationPreset in Inspector:
- **Create Common Presets**: Generates Fast_Popup, Smooth_SlideIn, Quick_Fade, Bouncy_Scale
- **Apply Preset** (on applicator): Manually trigger preset application

---

## Best Practices

1. **Naming Convention**: Use descriptive names
   - Good: "MenuPanel_SlideIn", "Button_HoverBounce"
   - Avoid: "Preset1", "NewPreset"

2. **Organize by Category**: Create folders
   - `Presets/Panels/`
   - `Presets/Buttons/`
   - `Presets/Dialogs/`

3. **Duration Guidelines**:
   - **Fast**: 0.15-0.2s (tooltips, small elements)
   - **Medium**: 0.3-0.4s (panels, dialogs)
   - **Slow**: 0.5-0.6s (dramatic reveals)

4. **Easing Recommendations**:
   - **Enter**: OutBack, OutCubic, OutQuad
   - **Exit**: InCubic, InQuad, Linear
   - **Bounce**: OutBounce (sparingly!)

5. **Consistency**: Use same preset for similar elements
   - All menu panels: Same slide direction
   - All confirm dialogs: Same scale animation
   - All tooltips: Same fade timing

---

## Troubleshooting

### Preset Not Applying
- ✅ Check component references are detected
- ✅ Use "Detect Components" context menu
- ✅ Verify preset asset is assigned

### Animation Feels Wrong
- ✅ Adjust duration (faster/slower)
- ✅ Try different easing curves
- ✅ Test on actual device (not just editor)

### Components Not Found
- ✅ Ensure UITransitionAnimator or UIButtonAnimator is on GameObject
- ✅ Click "Detect Components" in Inspector
- ✅ Check "Apply To" checkboxes are enabled

---

## Performance Notes

- Presets use reflection to set private fields (editor only)
- Runtime performance is identical to manually configured components
- No overhead during gameplay (presets applied once)
- DOTween handles all animation execution

---

## Examples in Project

Look for these examples in the project:
- **MainMenuPanel**: Smooth_SlideIn preset
- **ConfirmDialog**: Fast_Popup preset
- **InventoryButton**: Icon button preset
- **TooltipPanel**: Quick_Fade preset

---

## Related Components

- **UITransitionAnimator**: Panel/screen transitions
- **UIButtonAnimator**: Button hover/press animations
- **UILoadingScreen**: Loading screen animations
- **UIParticleEffect**: Celebration particle effects

---

**Preset System Goals:**
- 🎨 Designer-friendly (no code required)
- 🔁 Reusable (one preset, many elements)
- ⚡ Fast iteration (change preset, see results)
- 📏 Consistent (standardized animations)

**Workflow:**
1. Create preset
2. Configure in Inspector
3. Drag onto GameObjects
4. Done! ✅
