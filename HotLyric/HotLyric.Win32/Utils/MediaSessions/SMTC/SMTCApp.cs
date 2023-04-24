﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Media;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    public class SMTCApp : MediaSessionApp
    {
        public SMTCApp(
            string appId,
            string packageFamilyNamePrefix,
            bool hasStoreUri,
            SMTCAppPositionMode positionMode,
            string? customName = null,
            ImageSource? customAppIcon = null,
            bool supportLaunch = true,
            string? defaultLrcProvider = null,
            bool convertToSimpleChinese = false,
            Version? minSupportedVersion = null)
            : base(appId, customName, customAppIcon, defaultLrcProvider, convertToSimpleChinese, minSupportedVersion)
        {
            PackageFamilyNamePrefix = packageFamilyNamePrefix;
            PositionMode = positionMode;
            SupportLaunch = supportLaunch;

            if (hasStoreUri)
            {
                StoreUri = new Uri($"ms-windows-store://pdp/?productid={AppId}&mode=mini");
            }
        }

        public Uri? StoreUri { get; }

        public string PackageFamilyNamePrefix { get; }

        public SMTCAppPositionMode PositionMode { get; }

        public bool SupportLaunch { get; }
    }

    public enum SMTCAppPositionMode
    {
        FromApp,
        FromAppAndUseTimer,
        OnlyUseTimer
    }
}
