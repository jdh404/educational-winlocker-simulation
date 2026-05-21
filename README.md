# 🔬 Educational WinLocker Demo & Security Analysis

![C#](https://img.shields.io/badge/Language-C%23-blue?style=for-the-badge&logo=csharp)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/Framework-.NET%20Framework-purple?style=for-the-badge&logo=.net)
![Category](https://img.shields.io/badge/Category-Educational%20%2F%20Research-orange?style=for-the-badge)

A localized, modern dark-themed user interface simulation built with **C#** and **Windows Forms**. This project serves as an engineering case study demonstrating full-screen window containment (Kiosk Mode mechanics), Win32 API interoperability, low-level keyboard input monitoring, and defensive design patterns.

---

## ⚠️ Important Disclaimer & Session Recovery
> **CRITICAL NOTICE:** This software is developed strictly for **educational research, digital forensics analysis, and UI/UX prototyping**. It contains simulated locking behaviors designed to test input containment. It **does not** modify registry startup strings, inject into system processes, or encrypt user data.

* 🔑 **Default Session Unlock PIN:** `1234`
* 🛠 **Emergency Exit:** If the application loses focus during debugging, press `Ctrl + Shift + Esc` to open Task Manager, locate `Test_win.exe`, and select **End Task**.

---

## 📐 Architecture & Dual-Form Design

To bypass the classic Windows Forms limitation where setting an window's `Opacity` uniformly dilutes all child controls, this project implements a specialized **Dual-Form Architecture**:


                        ┌────────────────────────────────────────────────────────┐
                        │                   Windows Desktop Layer                │
                        └───────────────────────────▲────────────────────────────┘
                                                    │ Overlaid by
                        ┌───────────────────────────┴────────────────────────────┐
                        │  LockForm (Main Background)                            │
                        │  • WindowState: Maximized                              │
                        │  • Opacity: 65% (Tinted Glass Effect)                  │
                        │  • Captures low-level WH_KEYBOARD_LL hook              │
                        └───────────────────────────▲────────────────────────────┘
                                                    │ Centers & Hosts
                        ┌───────────────────────────┴────────────────────────────┐
                        │  ControlForm (Interactive UI Layer)                    │
                        │  • WindowState: Normal (500x400)                       │
                        │  • Opacity: 100% (Solid, High-Contrast Text/Inputs)    │
                        │  • Validates local PIN authentication strings          │
                        └────────────────────────────────────────────────────────┘

* **Visual Separation:** The background form stretches across all display boundaries to create a sleek, modern dimming effect over the operating system, while the inner panel remains perfectly sharp, legible, and uncompromised.
* **Dynamic Centering:** Responsive geometry events automatically recalculate coordinates to anchor the interactive interface securely in the absolute center of the monitor during runtime resolution shifts.

---

## 🧠 Technical Deep-Dive: Input Containment Methods

In this version of the educational WinLocker, there is a desktop-covering overlay that cannot be closed via Alt+F4 or close window buttons. It also includes a keyboard hook that blocks the Windows key.

### 1. Intercepting High-Level Window Messages
Standard interactive applications naturally listen to termination requests dispatched from the desktop manager. This project blocks user-driven escapes using two deep layers:

* **Form-Level Cancellation:** 
  The application attaches an event listener to `FormClosing`. If the evaluation yields `CloseReason.UserClosing`, the event is rejected at runtime:
  
```csharp
  if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; }
```
* **Kernel-Level Message Dropping:** The underlying Windows operating system message processing loop (`WndProc`) is overridden. The `WM_CLOSE (0x0010)` signal is targeted and dropped completely before it can reach the base compilation layers or trigger standard application shutdown routines:
  ```csharp
  const int WM_CLOSE = 0x0010;
  if (m.Msg == WM_CLOSE) 
  {
      return; // Silently dropped, ignoring the OS close request
  }
  ```


### 2. Low-Level Keyboard Hook (WH_KEYBOARD_LL)
System-critical infrastructure controls—specifically the Windows Menu Hotkey (Win)—operate globally. Intercepting them requires an unmanaged application hook injected into the Windows operating system input pipeline using native structural bindings (P/Invoke):

```csharp
[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
```
When a low-level keystroke occurs, the callback filters the virtual key codes for Left Windows (`0x5B`) and Right Windows (`0x5C`). By returning an arbitrary integer pointer (`(IntPtr)1`) instead of executing `CallNextHookEx`, the keyboard pipeline evaluates the event as consumed, effectively blinding the operating system from the keystroke.

## 🧪 Security Analysis & Bypass Research (How to Counter It)
Analyzing software constraint mechanics from an offensive and defensive engineering perspective provides deep insight into how administrative tools can control system behavior.

### Method 1: Security Desktop Interruption
Because the application runs as a standard execution thread within the user workspace, it remains bound to user-mode limitations.
* **The Bypass:** Triggering the **Secure Attention Sequence** (`Ctrl + Alt + Del`) forces Windows to switch context to the highly privileged *Winlogon Secure Desktop*. User-space global hooks are temporarily suspended in this privileged layer, allowing an administrator to safely launch Task Manager and force-kill the underlying execution tree.

### Method 2: Dynamic Binary Analysis
Since compiled applications evaluate conditions locally, local structures can be easily monitored and extracted by reverse engineers.
* **The Bypass:** Dropping the compiled assembly (`.exe`) into static analytical frameworks or .NET decompilers (such as **dnSpy** or **ILSpy**) instantly reconstructs the underlying source hierarchy. An analyst can inspect the class structures under `ControlForm.CheckPassword` to extract the authentication token directly from the local string constants:

```csharp
// Revealed instantly under compilation review
private const string UnlockPassword = "1234";
```

## 🛡️ Defensive Engineering: Best Practices

If we were designing a real, secure payment terminal for a bank or an information kiosk for a shopping mall, the current demonstration approach could not be used. Any experienced programmer or hacker could easily bypass this level of protection.

Here is how professionals upgrade this exact concept into legitimate, enterprise-grade security software for business environments:

| Security Domain | Current Educational Implementation | Enterprise Secure Alternative |
| :--- | :--- | :--- |
| **Credential Management** | Plaintext hardcoded verification string 1234. | Asymmetric cryptographic evaluation or secure salted hashing algorithms like Argon2id. |
| **Process Privilege** | User Mode standard Windows Form thread. | Custom Windows Shell substitution via Group Policy Object GPO execution constraints. |
| **Keyboard Restriction** | Unmanaged OS Hooks via user32 dynamic library. | Native Windows Assigned Access configurations restricting system key assignments at the kernel level. |
| **Lifecycle Architecture** | Local manual invocation. | Controlled OS Service deployment managed via dedicated background watchdog processes. |
