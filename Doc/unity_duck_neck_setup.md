# 🦆 Unity Splines + Tube Mesh 前置作業完整指南

> 分兩個階段：**灰盒驗證版**（快速確認技術可行）→ **正式美術版**（交給 3D 美術製作）

---

## 目錄
1. [Unity 環境設定](#1-unity-環境設定)
2. [套件安裝](#2-套件安裝)
3. [場景結構建立](#3-場景結構建立)
4. [階段一：灰盒驗證美術](#4-階段一灰盒驗證美術)
5. [階段二：正式美術資源規格](#5-階段二正式美術資源規格)
6. [Material 設定](#6-material-設定)
7. [驗證 Checklist](#7-驗證-checklist)

---

## 1. Unity 環境設定

### 推薦版本
```
Unity 2022.3 LTS 或以上（Splines package 穩定版需要此版本）
Render Pipeline：URP（Universal Render Pipeline）推薦
```

> ⚠️ **注意**：Built-in RP 也可以，但 URP 的 Shader Graph 對後續正式美術比較友善。

### 建立專案
```
1. Unity Hub → New Project
2. 選擇 "3D (URP)" 模板
3. 專案名稱：DuckNeckProto
```

---

## 2. 套件安裝

### 必裝套件

打開 `Window → Package Manager`：

| 套件名稱 | 安裝方式 | 用途 |
|----------|----------|------|
| **Unity Splines** | Unity Registry 搜尋 "Splines" | 脖子曲線骨架 |
| **ProBuilder** | Unity Registry 搜尋 "ProBuilder" | 灰盒美術製作 |
| **Polybrush** | Unity Registry 搜尋 "Polybrush" | （選用）灰盒細節調整 |

### XR 相關（如果做 VR）

| 套件名稱 | 安裝方式 |
|----------|----------|
| **XR Interaction Toolkit** | Unity Registry |
| **XR Hands** | Unity Registry |
| **OpenXR Plugin** | Unity Registry |

> 💡 **不做 VR 的話**：用滑鼠模擬頭部位置即可，先跳過 XR 套件。

### 安裝後確認
```
Package Manager 裡確認以下都顯示 ✓ Installed：
  - com.unity.splines  (2.x.x 以上)
  - com.unity.probuilder (5.x.x 以上)
```

---

## 3. 場景結構建立

### Hierarchy 結構
```
Scene
├── [Lighting]
│    ├── Directional Light
│    └── Sky Volume (URP)
│
├── [Environment]
│    └── Ground (ProBuilder Plane)
│
└── Duck (Empty GameObject)
     ├── DuckHead          ← 頭部，跟著手/Controller 走
     │    └── HeadMesh     ← 美術 Mesh 放這裡
     │
     ├── DuckBody          ← 身體，有 Rigidbody
     │    └── BodyMesh     ← 美術 Mesh 放這裡
     │
     └── Neck              ← 脖子系統
          ├── SplineContainer (Component)
          ├── NeckSplineController (Script)
          └── TubeMeshRenderer (Script)
               └── MeshFilter + MeshRenderer (自動產生)
```

### 各 GameObject 的 Component 設定

**DuckHead**
```
Component: Transform
  Position: (0, 1.5, 0)  ← 初始高度
  ← 不加 Collider（頭部用 Trigger 就好）
```

**DuckBody**
```
Component: Rigidbody
  Mass: 1
  Drag: 2            ← 加阻力讓身體不會亂飛
  Angular Drag: 5
  Constraints: Freeze Rotation X, Z  ← 防止身體翻滾

Component: CapsuleCollider
  Height: 0.4
  Radius: 0.15
```

**Neck**
```
Component: SplineContainer  ← 從 Add Component 搜尋
  （不需要額外設定，Scripts 會控制它）
```

---

## 4. 階段一：灰盒驗證美術

> 目標：用最快速的方式確認**脖子伸縮感覺對了**，不需要漂亮。

### 4-1. 鴨頭（灰盒）

用 **Unity 內建 Primitive** 即可：

```
DuckHead → 右鍵 → 3D Object → Sphere
  Scale: (0.25, 0.2, 0.3)   ← 略扁的蛋形
  Material: 黃色 (見下方 Material 設定)

加一個嘴巴：
  再加一個 Cube 子物件
  Scale: (0.15, 0.06, 0.2)
  Position: (0, -0.05, 0.2)  ← 往前突出
  Material: 橘色
```

### 4-2. 鴨身體（灰盒）

```
DuckBody → 右鍵 → 3D Object → Capsule
  Scale: (0.3, 0.25, 0.4)   ← 橢圓形身體
  Rotation: (90, 0, 0)       ← 讓膠囊橫躺
  Material: 黃色
```

### 4-3. 眼睛（灰盒）

```
在 DuckHead 下加兩個 Sphere：
  Left Eye:
    Scale: (0.06, 0.06, 0.06)
    Position: (-0.09, 0.05, 0.2)
    Material: 黑色

  Right Eye:
    Scale: (0.06, 0.06, 0.06)
    Position: (0.09, 0.05, 0.2)
    Material: 黑色
```

### 4-4. 脖子（灰盒）

脖子由程式碼自動生成 Tube Mesh，灰盒階段只需要：

```
Neck 物件的 TubeMeshRenderer 設定：
  Radius: 0.04
  Length Segments: 12
  Radial Segments: 6     ← 灰盒用 6 段就好，省效能

Material: 黃色（與身體同色）
```

### 4-5. 驗證用的「假手」控制器（非 VR 版）

建立一個簡單的滑鼠控制腳本，讓你可以在 Editor 裡測試：

```csharp
// HeadMouseController.cs
// 掛在 DuckHead 上，用滑鼠位置控制頭部
using UnityEngine;

public class HeadMouseController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float maxHeight = 3f;
    public float minHeight = 0.2f;

    void Update()
    {
        // WASD 控制 XZ 平面移動
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Q/E 控制高度（模擬手的上下）
        float y = 0f;
        if (Input.GetKey(KeyCode.E)) y =  1f;
        if (Input.GetKey(KeyCode.Q)) y = -1f;

        Vector3 move = new Vector3(h, y, v) * moveSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + move;

        // 限制高度範圍
        newPos.y = Mathf.Clamp(newPos.y, minHeight, maxHeight);
        transform.position = newPos;
    }
}
```

### 4-6. 灰盒階段完成標準

在進入正式美術之前，確認以下都 OK：

- [ ] 頭部移動時脖子曲線跟著變形
- [ ] 拉到最大長度時身體被帶著走
- [ ] 鬆弛時身體自然停止（不抖動）
- [ ] 脖子 Mesh 沒有破面或閃爍
- [ ] FPS 維持 60fps 以上（在目標平台測試）

---

## 5. 階段二：正式美術資源規格

> 灰盒驗證通過後，將以下規格交給 3D 美術製作。

### 5-1. 鴨頭 Mesh 規格

```
軟體：Blender / Maya / ZBrush
格式：.fbx（匯入 Unity）

多邊形數量：
  LOD0（近距離）：800 ~ 1,200 tri
  LOD1（遠距離）：300 ~ 500 tri

UV：
  展 UV 時將頭部、嘴巴分開 UV Island
  嘴巴保留足夠解析度（玩家視覺焦點）

Pivot Point：
  設在頭部中心偏下（脖子接合處）
  ← 這樣 Rotation 動畫看起來自然

骨架（Rig）：
  灰盒驗證版：不需要骨架
  正式版（如果要嘴巴開合動畫）：
    需要 2 根骨頭：UpperBeak, LowerBeak

匯出設定（Blender → FBX）：
  Scale: 0.01（Blender 預設單位 → Unity 公尺）
  Apply Transform: ✓
  Mesh: Triangulate ✓
```

### 5-2. 鴨身體 Mesh 規格

```
多邊形數量：
  LOD0：600 ~ 1,000 tri
  LOD1：200 ~ 400 tri

外型重點：
  身體底部要平坦（方便站在地面）
  尾巴翹起來（鴨子特徵）
  翅膀可以是身體的一部分（不需要獨立骨架）

Pivot Point：
  設在身體中心（與 Collider 中心對齊）
```

### 5-3. 脖子 Texture（重要！）

脖子是程式生成的 Tube Mesh，**不需要美術做 Mesh**，但需要提供 Texture：

```
解析度：256 × 512（寬 × 高）
  ← 高度要長，因為脖子會伸縮，UV 會拉伸

格式：PNG（匯入 Unity 後轉 Texture2D）

UV 方向：
  U 軸 = 繞圓周方向
  V 軸 = 沿脖子長度方向（會被拉伸）

內容建議：
  羽毛紋路沿 V 軸方向排列（拉伸時比較自然）
  避免明顯的橫向條紋（拉伸時會很醜）

Tiling 設定（在 Unity Material 裡）：
  X: 1
  Y: 2   ← 讓紋路重複，減少拉伸感
```

**脖子拉伸問題的解決方案（進階）：**

如果不希望貼圖跟著拉伸，在 `TubeMeshRenderer.cs` 的 UV 計算改成依**世界長度**而非 t 值：

```csharp
// 原本（會拉伸）：
uvs[idx] = new Vector2(j / (float)radialSegments, t);

// 改成依實際長度（不拉伸）：
float arcLength = spline.GetLength() * t;
uvs[idx] = new Vector2(j / (float)radialSegments, arcLength * uvTilingPerMeter);
```

### 5-4. Texture Map 清單

| Map | 解析度 | 說明 |
|-----|--------|------|
| **Albedo / Base Color** | 512×512（頭身）/ 256×512（脖子） | 主色，黃色羽毛 |
| **Normal Map** | 同上 | 羽毛凹凸感，可以很低頻 |
| **Roughness** | 可與 Normal 合併（GA channel） | 羽毛偏霧面 0.7 左右 |
| **Emission**（選用） | 128×128 | 眼睛發光效果 |

> 💡 **卡通風格的話**：可以只用 Albedo，配合 Toon Shader，省掉 Normal/Roughness。

### 5-5. 美術風格參考建議

```
參考方向：
  - 低多邊形（Low Poly）+ 平坦色：製作快、效能好、遠看清楚
  - 卡通渲染（Cel Shading）：需要自訂 Shader，但視覺特色強

鴨子顏色建議：
  身體/頭部：#F5C842（暖黃色）
  嘴巴/腳：  #E8892A（橘色）
  眼睛：     #1A1A1A（深黑）
  脖子：     比身體稍深一點 #D4A830（讓接縫自然）
```

---

## 6. Material 設定

### 灰盒用（Unity 內建）

```
在 Project 視窗右鍵 → Create → Material

DuckYellow_Gray:
  Shader: Universal Render Pipeline/Lit
  Base Color: #F5C842
  Smoothness: 0.3

DuckOrange_Gray:
  Base Color: #E8892A
  Smoothness: 0.3

DuckBlack_Gray:
  Base Color: #1A1A1A
```

### 正式版（URP Lit）

```
DuckBody_Mat:
  Shader: Universal Render Pipeline/Lit
  Base Map: duck_body_albedo.png
  Normal Map: duck_body_normal.png（Strength: 0.5）
  Smoothness: 0.25

DuckNeck_Mat:
  Shader: Universal Render Pipeline/Lit
  Base Map: duck_neck_albedo.png
  Tiling: (1, 2)   ← 避免拉伸
  Smoothness: 0.3
```

### 卡通 Shader（選用，需 Shader Graph）

```
Shader Graph 節點結構：

BaseColor
    ↓
[Cel Shading Node]
  受光 → 亮色 #F5C842
  背光 → 暗色 #C4A030
  Threshold: 0.4
    ↓
[Rim Light Node]
  Color: White
  Power: 3.0
    ↓
Output
```

---

## 7. 驗證 Checklist

### 技術驗證（灰盒階段）
- [ ] Spline 兩端 Knot 跟著 Head/Body Transform 更新
- [ ] Tube Mesh 每幀正確重建，無破面
- [ ] 脖子長度超過 `maxNeckLength` 時身體正確被拉動
- [ ] Rigidbody Drag 設定讓身體停止時不抖動
- [ ] `HeadMouseController` 可以用 WASD+QE 控制頭部

### 美術驗證（正式美術前）
- [ ] 灰盒比例看起來像鴨子（頭身比、脖子粗細）
- [ ] 脖子半徑 `radius` 值與頭部/身體接合處大小一致（沒有突兀的接縫）
- [ ] 脖子 Texture UV 方向正確（羽毛紋路沿長度方向）
- [ ] 拉伸到最長時貼圖不會過度扭曲

### 效能驗證
- [ ] PC：穩定 60fps（`lengthSegments=20, radialSegments=8`）
- [ ] Quest 2（VR）：穩定 72fps（`lengthSegments=12, radialSegments=6`）
- [ ] Profiler 確認 `RebuildMesh()` 耗時 < 0.5ms

---

## 快速開始流程

```
Day 1：環境設定 + 套件安裝 + Hierarchy 建立
Day 2：貼上 NeckSplineController + TubeMeshRenderer 程式碼，跑通灰盒
Day 3：調整物理參數，確認脖子手感
Day 4：通過灰盒 Checklist → 開美術規格 Brief 給 3D 美術
Day 5+：等美術的同時，繼續做遊戲機制
```
