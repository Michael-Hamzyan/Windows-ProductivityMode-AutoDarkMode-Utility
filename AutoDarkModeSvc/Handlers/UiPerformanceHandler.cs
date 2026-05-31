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
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;

namespace AutoDarkModeSvc.Handlers;

public static class UiPerformanceHandler
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private static readonly AdmConfigBuilder Builder = AdmConfigBuilder.Instance();

    public static void ActivateFastUiMode(DateTime? activeUntil)
    {
        UiPerformanceProfile profile = Builder.Config.UiPerformance.GetFastUiProfile();
        ActivateProfile(profile, activeUntil);
    }

    public static void ActivateProfile(UiPerformanceProfile profile, DateTime? activeUntil)
    {
        UiPerformanceSnapshot snapshot = new();

        if (profile.SetMinimizeMaximizeAnimation)
        {
            snapshot.MinimizeMaximizeAnimationEnabled = WindowsUiSettings.GetMinimizeMaximizeAnimationEnabled();
            snapshot.AppliedMinimizeMaximizeAnimationEnabled = profile.MinimizeMaximizeAnimationEnabled;
            WindowsUiSettings.SetMinimizeMaximizeAnimationEnabled(profile.MinimizeMaximizeAnimationEnabled);
        }

        if (profile.SetTaskbarGrouping)
        {
            snapshot.TaskbarGrouping = WindowsUiSettings.GetTaskbarGrouping();
            snapshot.AppliedTaskbarGrouping = profile.TaskbarGrouping;
            WindowsUiSettings.SetTaskbarGrouping(profile.TaskbarGrouping);
        }

        Builder.Config.UiPerformance.FastUiModeActive = true;
        Builder.Config.UiPerformance.FastUiModeActiveUntil = activeUntil;
        Builder.Config.UiPerformance.ActiveProfileName = profile.Name;
        Builder.Config.UiPerformance.ActiveSnapshot = snapshot;
        Builder.Save();

        Logger.Info($"activated UI performance profile '{profile.Name}' until {(activeUntil?.ToString("O") ?? "manual disable")}");
    }

    public static void DisableActiveProfile()
    {
        UiPerformance config = Builder.Config.UiPerformance;
        if (!config.FastUiModeActive)
        {
            return;
        }

        UiPerformanceSnapshot snapshot = config.ActiveSnapshot;
        if (snapshot != null)
        {
            RestoreMinimizeMaximizeAnimation(snapshot);
            RestoreTaskbarGrouping(snapshot);
        }

        config.FastUiModeActive = false;
        config.FastUiModeActiveUntil = null;
        config.ActiveProfileName = null;
        config.ActiveSnapshot = null;
        Builder.Save();

        Logger.Info("disabled active UI performance profile");
    }

    public static void DisableIfExpired()
    {
        UiPerformance config = Builder.Config.UiPerformance;
        if (config.FastUiModeActive && config.FastUiModeActiveUntil.HasValue && DateTime.Now >= config.FastUiModeActiveUntil.Value)
        {
            DisableActiveProfile();
        }
    }

    public static string GetStatus()
    {
        UiPerformance config = Builder.Config.UiPerformance;
        if (!config.FastUiModeActive)
        {
            return "Inactive";
        }

        return config.FastUiModeActiveUntil.HasValue
            ? $"Active until {config.FastUiModeActiveUntil.Value:O}"
            : "Active";
    }

    private static void RestoreMinimizeMaximizeAnimation(UiPerformanceSnapshot snapshot)
    {
        if (!snapshot.MinimizeMaximizeAnimationEnabled.HasValue || !snapshot.AppliedMinimizeMaximizeAnimationEnabled.HasValue)
        {
            return;
        }

        bool current = WindowsUiSettings.GetMinimizeMaximizeAnimationEnabled();
        if (current == snapshot.AppliedMinimizeMaximizeAnimationEnabled.Value)
        {
            WindowsUiSettings.SetMinimizeMaximizeAnimationEnabled(snapshot.MinimizeMaximizeAnimationEnabled.Value);
        }
    }

    private static void RestoreTaskbarGrouping(UiPerformanceSnapshot snapshot)
    {
        if (!snapshot.TaskbarGrouping.HasValue || !snapshot.AppliedTaskbarGrouping.HasValue)
        {
            return;
        }

        TaskbarGroupingMode current = WindowsUiSettings.GetTaskbarGrouping();
        if (current == snapshot.AppliedTaskbarGrouping.Value)
        {
            WindowsUiSettings.SetTaskbarGrouping(snapshot.TaskbarGrouping.Value);
        }
    }
}
