#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using AutoDarkModeLib.Configs;
using Microsoft.Win32;

namespace AutoDarkModeLib;

public static class WindowsUiSettings
{
    private const uint SPI_GETANIMATION = 0x0048;
    private const uint SPI_SETANIMATION = 0x0049;
    private const uint SPI_GETMENUANIMATION = 0x1002;
    private const uint SPI_SETMENUANIMATION = 0x1003;
    private const uint SPI_GETMENUFADE = 0x1012;
    private const uint SPI_SETMENUFADE = 0x1013;
    private const uint SPIF_UPDATEINIFILE = 0x0001;
    private const uint SPIF_SENDCHANGE = 0x0002;
    private const int HWND_BROADCAST = 0xffff;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const string ExplorerAdvancedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string TaskbarGlomLevelValue = "TaskbarGlomLevel";

    public static bool GetMinimizeMaximizeAnimationEnabled()
    {
        ANIMATIONINFO info = new() { cbSize = Marshal.SizeOf<ANIMATIONINFO>() };
        if (!SystemParametersInfo(SPI_GETANIMATION, info.cbSize, ref info, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        return info.iMinAnimate != 0;
    }

    public static void SetMinimizeMaximizeAnimationEnabled(bool enabled)
    {
        ANIMATIONINFO info = new()
        {
            cbSize = Marshal.SizeOf<ANIMATIONINFO>(),
            iMinAnimate = enabled ? 1 : 0
        };
        if (!SystemParametersInfo(SPI_SETANIMATION, info.cbSize, ref info, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static bool GetMenuFadeOrSlideEnabled()
    {
        bool menuAnimation = GetSystemBool(SPI_GETMENUANIMATION);
        bool menuFade = GetSystemBool(SPI_GETMENUFADE);
        return menuAnimation && menuFade;
    }

    public static void SetMenuFadeOrSlideEnabled(bool enabled)
    {
        SetSystemBool(SPI_SETMENUANIMATION, enabled);
        SetSystemBool(SPI_SETMENUFADE, enabled);
    }

    public static TaskbarGroupingMode GetTaskbarGrouping()
    {
        object value = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedPath)?.GetValue(TaskbarGlomLevelValue);
        return value is int intValue && Enum.IsDefined(typeof(TaskbarGroupingMode), intValue)
            ? (TaskbarGroupingMode)intValue
            : TaskbarGroupingMode.Always;
    }

    public static void SetTaskbarGrouping(TaskbarGroupingMode mode)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedPath, true);
        key.SetValue(TaskbarGlomLevelValue, (int)mode, RegistryValueKind.DWord);
        BroadcastSettingChange(ExplorerAdvancedPath);
    }

    private static bool GetSystemBool(uint action)
    {
        bool value = false;
        if (!SystemParametersInfo(action, 0, ref value, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        return value;
    }

    private static void SetSystemBool(uint action, bool value)
    {
        if (!SystemParametersInfo(action, 0, ref value, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static void BroadcastSettingChange(string area)
    {
        SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, area, SMTO_ABORTIFHUNG, 2000, out _);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ANIMATIONINFO
    {
        public int cbSize;
        public int iMinAnimate;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, int uiParam, ref ANIMATIONINFO pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, int uiParam, ref bool pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, string lParam, uint flags, uint timeout, out IntPtr result);
}
