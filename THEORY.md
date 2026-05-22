# 🛡️ Educational WinLocker in C# (Windows Forms)

> **Important:** This project was created strictly for educational purposes to study the inner workings of screen lockers and defenses against them. The code **is not malicious** (it does not replicate itself, modify the registry, or include autorun persistence), but it demonstrates the real-world techniques used by adversaries.  
> **Only run this application within an isolated virtual environment (VirtualBox, VMware) containing no critical user data.**

---

## 📖 Table of Contents

1. [What is a WinLocker?](#what-is-a-winlocker)
2. [General Code Overview](#general-code-overview)
3. [Project Structure](#project-structure)
4. [Deep Dive into Key Techniques](#deep-dive-into-key-techniques)
   - [Semi-Transparent Tinted Background](#1-semi-transparent-tinted-background)
   - [Blocking the Windows Key via Low-Level Hook](#2-blocking-the-windows-key-via-low-level-hook)
   - [Preventing Form Closure (Alt+F4, Close Button)](#3-preventing-form-closure-altf4-close-button)
   - [Custom-Styled MessageBox](#4-custom-styled-messagebox)
5. [How to Fight Such Applications](#how-to-fight-such-applications)
   - [Rapid Control Recovery](#rapid-control-recovery)
   - [Removing a Locked Application (If Persistent)](#removing-a-locked-application-if-persistent)
6. [Legal Disclaimer](#legal-disclaimer)

---

## What is a WinLocker?

A **WinLocker** is a type of malicious software that completely freezes the Windows user interface, displaying a full-screen window demanding a ransom (usually via SMS or cryptocurrency transfer). The program overlays the desktop, covers the taskbar, and often blocks critical system key combinations (`Alt+F4`, `Ctrl+Esc`, `Win`, etc.) to prevent the victim from closing the window.

Our educational prototype is safe, but it demonstrates three fundamental techniques:
- Obstructing the screen with a semi-transparent, top-most window overlay.
- Blocking the `Win` key (which opens the Start Menu) using a low-level keyboard hook.
- Defending the window against standard application termination methods.

---

## General Code Overview

The codebase is written in C# utilizing Windows Forms and minimal Win32 API interoperability. The underlying logic is cleanly separated into three classes, each responsible for its own part of the user interface:

| Class | Purpose |
| :--- | :--- |
| `LockForm` | The main background form. It is semi-transparent, spans the entire screen display, activates the `Win` key hook, and prevents self-closure. |
| `ControlForm` | An opaque user interface layer containing the PIN input field and submission button. It centers itself directly on top of `LockForm`. |
| `CustomMessageBox` | A styled dialog box (replacing the standard system `MessageBox`) configured to remain layered above all other windows. |

The default unlock code is `1234`. Upon verifying the correct password, the application gracefully exits, restoring full administrative control to the operating system.

---

## Project Structure

The project follows a standard layout for Windows Forms (.NET 6/7/8), consisting of the following files:

- `Form1.cs` – Contains the entire source code architecture (all three classes). While real-world applications separate classes into independent files, they are aggregated here into a single workspace for straightforward analysis.
- `Program.cs` – The application entry point generated automatically by the IDE template. It executes the initialization process by running `Application.Run(new LockForm())`.

> **Note:** If you generated this project inside Rider or Visual Studio, a `Form1.Designer.cs` file might be created automatically. You should **delete** this designer file from the project tree, as we bypass the visual drag-and-drop designer and instantiate all interface elements programmatically.

---

## Deep Dive into Key Techniques

### 1. Semi-Transparent Tinted Background

```csharp
this.WindowState = FormWindowState.Maximized;
this.FormBorderStyle = FormBorderStyle.None;
this.TopMost = true;
this.BackColor = Color.FromArgb(20, 20, 22);
this.Opacity = 0.65;
```

* `Maximized` + `FormBorderStyle.None` – Spans the window across the entire screen layout, removing the standard window borders and caption bar.
* `TopMost = true` – Forces the window to stay permanently layered above all other running applications.
* `BackColor` – Defines the dark theme background color.
* `Opacity = 0.65` – Implements the semi-transparent "dimming" visual effect across the monitor. The background layer darkens the screen, while the input window (`ControlForm`) maintains solid visibility with an `Opacity` value of `1.0`.

### 2. Blocking the Windows Key via Low-Level Hook

A **Hook** is an native Windows operating system mechanism that intercepts global system events (such as keystrokes or mouse movements) before they ever reach the target active application.

```csharp
[DllImport("user32.dll")]
private static extern IntPtr SetWindowsHookEx(...);

[DllImport("user32.dll")]
private static extern bool UnhookWindowsHookEx(...);
```
### 🔑 Under the Hood: Low-Level Input Interception

By deploying a low-level global keyboard hook (`WH_KEYBOARD_LL = 13`), the application injects itself directly into the Windows OS input processing pipeline system-wide.

Inside the `HookCallback` routine, the virtual key code (`vkCode`) of every registered hardware keypress is actively inspected:

* **Targeted Interception:** If the hook detects a Left Windows (`0x5B`) or Right Windows (`0x5C`) key code, the callback function immediately returns `(IntPtr)1`. This sends a signal back to the Windows kernel indicating that the input event has already been completely handled and consumed. As a result, the OS drops the message, and the Start Menu never opens.
* **Standard Pass-Through:** For all other non-targeted key codes, the execution chain invokes `CallNextHookEx`. This safely forwards the message down the OS line, allowing standard typing and shortcuts to process normally.

> ⚠️ **Resource Management Warning:** This unmanaged global hook relies entirely on the lifecycle of our active process thread. If the application is closed gracefully via successful PIN verification or terminated abruptly by an administrator, the unmanaged hook structures are released (`UnhookWindowsHookEx`), safely restoring native input behaviors back to the operating system.

### 3. Preventing Form Closure (Alt+F4, Close Button)

```csharp
private void LockForm_FormClosing(object sender, FormClosingEventArgs e)
{
    if (e.CloseReason == CloseReason.UserClosing)
        e.Cancel = true;
}
```
* **High-Level Interception**: The `FormClosing` event is triggered whenever a user attempts to terminate the active window (e.g., via the system close button, taskbar commands, or the standard `Alt + F4` hotkey shortcut).
* **Event Cancellation**: The condition evaluates the `CloseReason`. If it yields `CloseReason.UserClosing`, the operation is forcefully rejected at runtime by setting `e.Cancel = true`.
* **Low-Level Message Dropping**: As an additional layer of protection, the underlying message processing loop (`WndProc`) is overridden to catch and silently ignore the native `WM_CLOSE` system signal.

>💡 **Design Architecture Note**: We explicitly do not apply these closure restrictions to the interactive `ControlForm`. When the operator types the correct PIN, the program safely bypasses form-level constraints by invoking a global environment shutdown via `Application.Exit()`.

### 4. Custom-Styled MessageBox
The native `MessageBox` component provided by the standard Windows Forms library is highly restrictive and does not support modern UI modifications such as custom background colors, non-standard fonts, or border adjustments.

To preserve a seamless, immersive dark-themed visual experience throughout the application, this project introduces a standalone `CustomMessageBox` class extending the base `Form` component:

* **Strict Layout Constraints**: It is instantiated with a fixed geometry boundary, a borderless configuration (`FormBorderStyle.None`), and a dark color palette.
* **Context-Aware Visual States**: It dynamically accepts input text parameters along with an `isError` boolean flag. This flag evaluates at runtime to switch the border and header accent colors (e.g., turning red during invalid PIN attempts).
* **Modal Context Layering**: It is invoked exclusively via the `.ShowDialog()` routine, forcing it to remain layered directly over the owner window and capturing user attention until dismissed.

## 🧪 Incident Response: How to Counter and Neutralize WinLockers

Analyzing recovery techniques and cleanup strategies provides systemic insight into how an administrator or digital forensics analyst can regain control of an operating system compromised by an interface-containment application.

### 1. Rapid Control Recovery (Immediate Mitigation)

When an untrusted execution thread captures exclusive focus and filters standard keyboard sequences, the following triage methodologies are applied sequentially:

* **Task Manager Interruption:** The most straightforward mitigation vector is triggering the `Ctrl + Shift + Esc` sequence. If the locker aggressively captures focus, execute `Win + D` first to minimize the desktop layer, then immediately invoke the Task Manager. Locate the target binary (e.g., `Test_win.exe`), right-click, and select **End Task**.
* **Command-Line Termination:** If the graphical Task Manager interface is obstructed or fails to initialize, invoke the Run dialog via `Win + R`, type `cmd`, and execute the forceful process termination command:
  ```cmd
  taskkill /f /im Test_win.exe
  ```
(Replace `Test_win.exe` with the explicit runtime name of the targeted binary).

* **Safe Mode Bootstrapping**: Restart the host operating system and enter Safe Mode (via the `F8` boot menu on legacy environments or via Windows Recovery Environments / WinRE on modern systems). Safe Mode bypasses the execution of third-party user-space startup structures, allowing the operator to safely locate and delete the physical payload.
* **External Live USB/Bootable Media**: If the operating system shell is completely unresponsive, boot the machine via an isolated external Live USB environment (such as a Linux-based forensic distribution or a Windows Preinstallation Environment - WinPE). Mount the local storage drive and manually delete the binary from the file system.

### 2. Post-Exploitation Persistence Cleanup

While our educational prototype remains volatile and does not implement persistence mechanisms, real-world WinLockers aggressively anchor themselves within the operating system architecture. Remediation requires auditing the following vectors:

> 📂 **Registry Modification Auditing**

Malicious actors typically hijack the initialization layers of the OS. Inspect and remediate the following keys using a remote registry editor or a bootable environment:

* **Standard User Run Keys:**
`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
* **System-Wide Run Keys:**
`HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
* **OS Shell Substitution (Critical)**: Check the `Shell` string value located inside:
`HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon`

In a secure, standard operating system environment, this value must exclusively read `explorer.exe`. If it targets the path of a temporary binary or script, restore it to the default system shell handler.

>🕒 **Task Scheduler Inspection**

Advanced malware evades standard startup directories by registering persistence tasks. Open the Task Scheduler snap-in (`taskschd.msc`) and analyze tasks triggered on Logon or System Startup for unrecognized binaries.

> 📁 **Direct Startup Directory Auditing**

Manually inspect the filesystem directory where Windows looks for user-defined startup shortcuts:
`C:\Users\<Username>\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup`

### 3. Preventative Engineering Measures

To mitigate risks associated with interface-containment attacks, standard defensive architectures demand:

* **Behavioral Analysis Engines**: Deploying modern EDR/AV solutions capable of intercepting unexpected global Windows API hooks (`SetWindowsHookEx`) and unusual full-screen `TopMost` behaviors.
* **System Backups**: Maintaining decentralized, offline backup topologies (3-2-1 backup strategy) to eliminate extortion leverage.
* **Privilege Isolation**: Restricting daily operations to low-privileged user accounts, preventing unmanaged binaries from executing elevated or system-wide hooks.

---

## ⚖️ Legal & Ethical Disclaimer

**CRITICAL NOTICE:** This repository and the accompanying documentation are created strictly for **educational, academic, and research purposes** within the scopes of digital forensics, malware analysis, and defensive systems engineering. 

* **No Malicious Intent:** The source code and concepts demonstrated herein are designed to analyze user-interface isolation and native Win32 input monitoring patterns. This project does not contain propagation routines, automated registry persistence mechanisms, or payload encryption capabilities.
* **Prohibited Use:** The author explicitly condemns the use of this software, code fragments, or theoretical methodologies for unauthorized testing, malicious disruption, digital extortion, or any actions that violate local, national, or international computer crime legislation (such as the US Computer Fraud and Abuse Act or equivalent global cyber-laws).
* **Limitation of Liability:** The materials are provided "AS IS" without warranty of any kind. The author assumes **zero liability** for any direct or indirect damages, data loss, system instability, or legal consequences resulting from the misuse, execution, or modification of the provided code or documentation. All testing should be restricted to isolated, non-production virtual environments.
