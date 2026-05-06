# Link Burst – Unity Setup Guide

## Scene Setup

Create **one scene** (`_Project/Scenes/GameplayScene.unity`) with the following hierarchy:

```
GameplayScene
├── [Managers]                        (empty GameObject, DontDestroyOnLoad)
│   ├── GameManager       → GameManager.cs
│   ├── BoardManager      → BoardManager.cs
│   ├── InputManager      → InputManager.cs
│   ├── ScoreManager      → ScoreManager.cs
│   ├── PowerUpManager    → PowerUpManager.cs
│   └── AudioManager      → AudioManager.cs
│
├── Board                             (empty, place at 0,0,0)
│   └── (nodes spawn here at runtime)
│
├── ChainLine                         (LineRenderer component)
│
├── Main Camera                       (Camera, URP stack)
│
└── [UI Canvas]                       (Canvas → Screen Space Overlay)
    ├── SplashScreen
    │   ├── Logo (Image)
    │   ├── LogoGroup (CanvasGroup)
    │   └── LoadingBar (Slider)
    │
    ├── MainMenuScreen
    │   ├── Title "LINK BURST" (TextMeshPro)
    │   ├── PlayButton → UIManager.OnPlayButton()
    │   ├── LevelSelectButton → UIManager.OnLevelSelectButton()
    │   └── SettingsButton → UIManager.ToggleSettings()
    │
    ├── LevelSelectScreen
    │   └── ButtonContainer (GridLayoutGroup)
    │       └── (level buttons spawned by LevelSelectUI.cs)
    │
    ├── GameplayScreen
    │   ├── HUD
    │   │   ├── ScoreText (TextMeshPro)
    │   │   ├── TargetScoreText (TextMeshPro)
    │   │   ├── EnergyText (TextMeshPro)
    │   │   ├── EnergySlider (Slider)
    │   │   ├── LevelText (TextMeshPro)
    │   │   ├── ComboText (TextMeshPro)  ← floating score pop-up
    │   │   └── ComboMultiText (TextMeshPro)  ← "x2 COMBO!"
    │   │
    │   ├── PowerUpBar
    │   │   ├── BoostButton → UIManager.OnBoostButton()
    │   │   │   └── CountLabel (TextMeshPro)
    │   │   ├── BombButton → UIManager.OnBombButton()
    │   │   │   └── CountLabel (TextMeshPro)
    │   │   └── EnergyButton → UIManager.OnEnergyButton()
    │   │       └── CountLabel (TextMeshPro)
    │   │
    │   └── PauseButton → UIManager.OnPauseButton()
    │
    ├── [Popups]
    │   ├── PauseMenu
    │   │   ├── ResumeButton → UIManager.OnResumeButton()
    │   │   ├── RestartButton → UIManager.OnRestartButton()
    │   │   └── HomeButton → UIManager.OnHomeButton()
    │   │
    │   ├── WinScreen
    │   │   ├── WinScoreText (TextMeshPro)
    │   │   ├── WinHighScoreText (TextMeshPro)
    │   │   ├── Star1 (Image)
    │   │   ├── Star2 (Image)
    │   │   ├── Star3 (Image)
    │   │   ├── NextLevelButton → UIManager.OnNextLevelButton()
    │   │   └── HomeButton → UIManager.OnHomeButton()
    │   │
    │   ├── LoseScreen
    │   │   ├── RetryButton → UIManager.OnRestartButton()
    │   │   └── HomeButton → UIManager.OnHomeButton()
    │   │
    │   ├── InvalidMovePopup   (text: "Need 2+ connected nodes!")
    │   │
    │   ├── BoostPopup
    │   │   ├── Title "BOOST"
    │   │   ├── Description "Clears a 3×3 area!"
    │   │   ├── UseButton → UIManager.OnConfirmBoost()
    │   │   └── CloseButton → UIManager.HideBoostPopup()
    │   │
    │   ├── BombPopup
    │   │   ├── Title "BOMB"
    │   │   ├── Description "Destroys surrounding nodes!"
    │   │   ├── UseButton → UIManager.OnConfirmBomb()
    │   │   └── CloseButton → UIManager.HideBombPopup()
    │   │
    │   └── EnergyPopup (auto-used, no confirm needed)
    │
    └── SettingsPanel
        ├── SoundToggle → UIManager.ToggleSound()
        └── MusicToggle → UIManager.ToggleSound()
```

---

## Component Wiring (Inspector)

### GameManager
| Field | Value |
|---|---|
| Default Target Score | 500 |
| Default Start Energy | 30 |
| Default Grid Width | 6 |
| Default Grid Height | 6 |
| Levels | drag LevelData assets here |

### BoardManager
| Field | Value |
|---|---|
| Grid Width | 6 |
| Grid Height | 6 |
| Cell Size | 1.1 |
| Cell Spacing | 0.05 |
| Node Prefab | drag `NodePrefab` here |
| Node Sprites | 6 sprites (Red,Blue,Green,Yellow,Purple,Orange) |
| Node Glow Colors | 6 colors matching above order |
| Board Parent | drag `Board` GameObject |

### InputManager
| Field | Value |
|---|---|
| Minimum Chain Length | 2 |
| Chain Line | drag `ChainLine` GameObject |
| Valid Color | white (0.95 alpha) |
| Pending Color | yellow (0.85 alpha) |
| Line Width | 0.08 |

### AudioManager – Sounds array (8 entries)
| Name | Loop |
|---|---|
| background | ✓ |
| tap | ✗ |
| explosion | ✗ |
| energy | ✗ |
| ui_click | ✗ |
| win | ✗ |
| lose | ✗ |
| chain | ✗ |

---

## Node Prefab Structure

```
NodePrefab (Sprite Renderer + NodeController)
└── Glow (Sprite Renderer – additive blending, scale ~1.4)
```

- Outer sprite: colored circle / gem
- Glow child: same sprite, additive material, alpha driven by NodeController

---

## LevelData Assets

Create via: **Assets → Create → LinkBurst → LevelData**

Suggested progression:

| Level | Grid | Target | Energy | bonusBase |
|---|---|---|---|---|
| 1 | 6×6 | 300 | 30 | 5 |
| 2 | 6×6 | 450 | 28 | 5 |
| 3 | 6×6 | 600 | 26 | 5 |
| 4 | 7×7 | 800 | 30 | 6 |
| 5 | 7×7 | 1000 | 28 | 6 |

---

## Color Scheme (Neon/Glow)

| NodeColor | Sprite Tint | Glow Color |
|---|---|---|
| Red | `#FF3A3A` | `#FF6060` |
| Blue | `#3A8FFF` | `#60B0FF` |
| Green | `#3AFF7A` | `#60FFB0` |
| Yellow | `#FFE03A` | `#FFEE80` |
| Purple | `#B03AFF` | `#D060FF` |
| Orange | `#FF8C3A` | `#FFB060` |

Background: dark space gradient `#0A0A1A` → `#1A0A2E`
