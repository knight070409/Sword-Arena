# Gape Labs - Multiplayer Arena Shooter Prototype

A lightweight multiplayer arena shooter prototype built with Unity 6 and Photon PUN2, featuring melee combat, round-based gameplay, and optimized mobile performance.

## 🎮 Project Overview

This project is a technical assignment demonstrating fundamentals in Unity multiplayer networking, Addressables, Asset Bundles, async workflows, and mobile optimization. The game features a 3-round sword combat arena where players battle to win rounds, with the winner being the first to eliminate their opponent.

---

## 📥 Download

Get the latest APK build from [Releases](https://github.com/knight070409/Sword-Arena/releases) 

**Latest Version**: v1.0.0  
**Download**: [Sword Arena.apk](https://github.com/knight070409/Sword-Arena/releases/download/v1.0.0/Sword.Arena.apk)

---

## 📋 Technical Specifications

### Unity & Framework
- **Unity Version**: Unity 6 (6000.0.59f2)
- **Networking Library**: Photon PUN2 (Photon Unity Networking 2)
- **Target Platform**: Android (APK build)
- **Build Target**: Android API Level 21+

### Key Features Implemented
✅ Quick Match multiplayer with automatic room joining  
✅ Player movement with virtual joystick  
✅ Melee combat system with sword attacks  
✅ Health system with respawn mechanics  
✅ Round-based gameplay (3 rounds, best of 3)  
✅ Addressables asset loading (async)  
✅ Asset Bundle loading from cloud (runtime)  
✅ Mobile touch controls (joystick + attack button)  
✅ Network optimization (reduced sync frequency)  
✅ Error handling with retry mechanisms  

---

## 🎯 Core Gameplay

### Combat System
- **Attack Type**: Melee sword combat with collision detection
- **Health**: 100 HP per player
- **Damage**: 20 HP per sword hit
- **Attack Cooldown**: 1 second between attacks
- **Death**: Player is eliminated when health reaches 0

### Round System
- **Total Rounds**: 3 rounds per match
- **Win Condition**: First player to die loses the round
- **Match Winner**: Player who wins the most rounds (2/3)
- **Round Timer**: 3-second countdown before each round starts
- **Respawn**: Automatic respawn between rounds

### Multiplayer Features
- **Room System**: Automatic matchmaking or room creation
- **Max Players**: Up to 4 players per room (minimum 2 to start)
- **Player ID**: Auto-generated PlayerPrefs-based ID (Player01-04)
- **Synchronization**: Position, rotation, health, and combat state synced

---

## 📦 Addressables & Asset Bundles

### Addressables Implementation
**Asset**: Snowman Prop  
**Key**: `Snowman`  
**Loading Method**: `Addressables.LoadAssetAsync<GameObject>()`  
**Purpose**: Demonstrates async asset loading from Addressables catalog  
**Location**: Spawned in game scene after async load completes  

### Asset Bundle Implementation
**Asset**: Barrel Prop  
**Bundle Name**: `barrelprop`  
**Hosting**: Google Drive (public CDN)  
**Download URL**: 
```
https://drive.google.com/uc?export=download&id=1rJQLkK1kQvAHC7uwQlu1DAoN2AW-IWJY
```
**Loading Method**: `UnityWebRequest` → `AssetBundle.LoadFromMemory()`  
**Purpose**: Runtime cloud-hosted asset bundle download and instantiation  
**Asset Name in Bundle**: `BarrelProp`  

---

## ⚡ Optimization Techniques

### 1. Network Optimization
- **Send Rate**: 20 updates/second (reduced from 30)
- **Serialization Rate**: 10 updates/second (reduced from 20)
- **Position Updates**: Only synced when player moves (delta threshold)
- **Remote Player Scripts**: Update-heavy scripts disabled on remote players
- **Bandwidth Reduction**: ~33% less network traffic compared to default settings

### 2. Texture Optimization
**Model**: Snowman model texture  
**Compression**: ASTC 6*6 block compression format  
**Purpose**: Reduces texture memory footprint for mobile devices  
**Result**: Smaller build size and faster GPU texture loading  

### 3. Script Optimization
- **Animator Parameter Caching**: Hashed parameter names for faster lookups
- **Conditional Updates**: Movement/animation only updates when state changes
- **RPC Optimization**: Minimal RPC calls, batched where possible
- **Camera Culling**: Third-person camera with collision detection optimization

### ⚠️ Object Pooling
**Status**: Not implemented in this prototype  
**Reason**: Projectile-based combat was replaced with melee sword combat, eliminating the need for frequent instantiate/destroy cycles that object pooling addresses.

---

## 🎨 Folder Structure

```
Assets/
├── Scripts/
│   ├── Networking/
│   │   ├── NetworkManager.cs          # Photon connection & room management
│   │   └── WaitingRoomManager.cs      # Pre-game lobby system
│   ├── Gameplay/
│   │   ├── PlayerController.cs        # Player movement, combat, health
│   │   ├── GameManager.cs             # Game state & player spawning
│   │   ├── RoundManager.cs            # 3-round match system & scoring
│   │   ├── SwordCollider.cs           # Melee combat collision detection
│   │   └── ThirdPersonCamera.cs       # Camera follow system
│   ├── UI/
│   │   └── MobileInputManager.cs      # Virtual joystick & touch controls
│   ├── Addressables/
│   │   └── AddressablesLoader.cs      # Async addressables loading
│   └── AssetBundles/
│       └── AssetBundleLoader.cs       # Cloud asset bundle downloader
├── Resources/
│   └── Player.prefab                  # Networked player prefab
├── Scenes/
│   ├── LauncherScene                  # Main menu with Play button
│   ├── WaitingRoomScene               # Multiplayer lobby
│   └── GameScene                      # Main arena gameplay
└── AddressableAssetsData/             # Addressables catalog
```

---

## 🔧 Error Handling

### Network Errors
- **Connection Failed**: Retry button with reconnection logic
- **Room Creation Failed**: Automatic fallback to join existing room
- **Disconnect During Game**: Remaining player auto-wins remaining rounds
- **Master Client Migration**: Photon handles automatically

### Asset Loading Errors
- **Addressables Load Fail**: Error panel with retry button
- **Asset Bundle Download Fail**: Network error handling with retry mechanism
- **Missing Assets**: Graceful fallback with error logging

### UI Feedback
- Status text updates for all operations
- Error panels with descriptive messages
- Retry buttons for recoverable errors
- Debug console logging for developers

---

## 🚀 Setup Instructions

### How to Run 2 Clients

#### Option 1: Unity Editor + Build (Recommended)
1. **Build the APK**:
   - File → Build Settings → Android → Build
   - Install APK on Android device/emulator

2. **Run in Unity Editor**:
   - Open `LauncherScene`
   - Press Play in Unity Editor
   - Click "Play" button to join

3. **Join from Device**:
   - Launch app on Android device
   - Tap "Play" button
   - Both clients will automatically join the same room

#### Option 2: Two Build Instances
1. Build APK and install on two Android devices
2. Launch on both devices
3. Both tap "Play" - automatic matchmaking will pair them

#### Option 3: Unity Editor Duplication (Testing Only)
1. Build a standalone Windows/Mac build
2. Run the build executable
3. Run Unity Editor play mode
4. Both instances will connect to the same Photon room

### First-Time Setup
1. **Import Photon PUN2**:
   - Window → Asset Store → Search "Photon PUN 2"
   - Import into project
   - Enter Photon App ID in Photon Server Settings

2. **Configure Addressables**:
   - Window → Asset Management → Addressables → Groups
   - Verify `Snowman` asset is marked as Addressable
   - Build Addressables: Build → New Build → Default Build Script

3. **Build Asset Bundles**:
   - Already built and hosted on Google Drive
   - URL configured in `AssetBundleLoader.cs`
   - No additional setup required

---

## 🤖 AI Assistance Summary

AI (ChatGPT/Claude) was used for the following tasks:

1. **Multiplayer Networking Implementation**:
   - Photon PUN2 setup and configuration
   - Room creation/joining logic with automatic matchmaking
   - Network synchronization for player movement and health
   - RPC implementation for combat damage and animations

2. **Round-Based Game Logic**:
   - 3-round match system with scoring
   - Player death/respawn mechanics synchronized across network
   - Countdown timer and match end conditions
   - Handling player disconnection during matches

3. **Script Architecture & Optimization**:
   - Clean folder structure organization
   - Network optimization strategies (send rates, conditional updates)
   - Animator parameter caching for performance
   - Error handling patterns with retry mechanisms

---

## 📱 Mobile Controls

### Virtual Joystick
- **Location**: Bottom-left corner
- **Function**: 360° player movement
- **Implementation**: Touch-based with visual feedback

### Attack Button
- **Location**: Bottom-right corner
- **Function**: Sword attack with cooldown
- **Visual**: Red circular button with sword icon

### UI Elements
- **Health Bar**: Top-center (green = local, red = remote)
- **Player Name**: Above health bar
- **Round Info**: Center-top during round transitions
- **Scoreboard**: Right side showing player scores



**Built with Unity 6 | Powered by Photon PUN2 | Optimized for Mobile**
