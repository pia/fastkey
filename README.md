# FastKey - Windows 托盘快捷贴靠工具（C#）

这是一个 Windows 全局热键小工具，运行后常驻系统托盘（右下角），用于把当前活动窗口快速贴靠到左右半屏。

## 功能

- `Alt + A`：当前窗口贴靠到屏幕左半边
- `Alt + D`：当前窗口贴靠到屏幕右半边
- 右键托盘菜单新增 `拉回跑丢窗口到主屏`
- 首次运行会自动注册当前用户开机自启（写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`）
- 启动后默认隐藏到系统托盘（不弹命令行窗口）
- 托盘使用月亮图标，便于识别
- 右键托盘菜单可启用/禁用开机自启
- 双击托盘图标：显示/隐藏状态窗口
- 点击窗口右上角关闭：不会退出程序，只会隐藏到托盘
- 真正退出：右键托盘图标 -> `退出程序`
- 已补充高 DPI / 高分屏坐标修正，左右贴靠时会按窗口可视边框进行补偿，尽量消除中间缝隙

> 实现方式为 Win32 API 直接调整窗口位置，不依赖发送 `Win + ←/→`。

---

## 当前目录结构

```text
fastkey/
├─ README.md
└─ tools/
   └─ window-snap-hotkeys-cs/
      ├─ Program.cs
      ├─ build.ps1
      └─ dist/
         └─ WindowSnapHotkeys.exe
```

---

## 直接运行

可执行文件：

`tools/window-snap-hotkeys-cs/dist/WindowSnapHotkeys.exe`

双击运行后，程序会常驻托盘；如果没看到图标，请点任务栏右下角“^”展开隐藏图标。

---

## 重新编译

```powershell
cd tools/window-snap-hotkeys-cs
./build.ps1
```

输出文件：

`tools/window-snap-hotkeys-cs/dist/WindowSnapHotkeys.exe`

说明：当前机器未安装 .NET SDK，脚本使用系统自带 `csc.exe` 编译。

---

## 迁移到新位置（你后面换路径时）

### 只搬运行文件（最简）

只复制这个文件即可：

`WindowSnapHotkeys.exe`

### 搬完整项目（可继续改代码/重编译）

把 `window-snap-hotkeys-cs` 整个目录一起搬走，保留内部结构，然后在新位置运行 `build.ps1`。

---

## 开机自启

程序启动时会自动把当前可执行文件路径写入注册表 `Run` 项，实现当前用户开机自启。

如果自动注册失败，可手动设置：

1. 按 `Win + R`，输入 `shell:startup`
2. 把 `WindowSnapHotkeys.exe` 的快捷方式放进去

### 禁用开机自启

推荐方式：右键托盘图标 -> `禁用开机自启`。

也可手动删除注册表项：

1. 按 `Win + R`，输入 `regedit`
2. 打开 `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
3. 删除 `WindowSnapHotkeys`

---

## 常见问题

### 1) 热键对某些程序无效

如果目标程序是“管理员权限”运行，本工具也需要“管理员权限”运行。

### 2) 看不到托盘图标

先点击任务栏右下角 `^` 展开隐藏图标；可把它拖到常驻显示区域。

### 3) 误关窗口后热键还在吗

在。窗口关闭只是隐藏到托盘，程序仍在运行。

### 4) 窗口跑丢到屏幕外怎么办

右击托盘图标，选择 `拉回跑丢窗口到主屏`，程序会把当前不在任何显示器上的普通窗口拉回主屏工作区中央。

### 5) 高分屏左右贴靠中间有缝怎么办

新版已增加高 DPI 感知和窗口可视边框补偿，通常会明显改善高分辨率或高缩放比例下左右贴靠的中缝问题。
