﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HotLyric.Win32.Models
{
    public class LyricThemeView
    {
        private static Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);

        public LyricThemeView(Brush? borderBrush, Brush? backgroundBrush, Brush? lyricBrush, Brush? karaokeBrush, Brush? lyricStrokeBrush, Brush? karaokeStrokeBrush)
            : this("customize", borderBrush, backgroundBrush, lyricBrush, karaokeBrush, lyricStrokeBrush, karaokeStrokeBrush)
        {
            KaraokeStrokeBrush = karaokeStrokeBrush;
        }

        internal LyricThemeView(string name, Brush? borderBrush, Brush? backgroundBrush, Brush? lyricBrush, Brush? karaokeBrush, Brush? lyricStrokeBrush, Brush? karaokeStrokeBrush)
        {
            Name = name;

            BorderBrush = NormalizeBrush(borderBrush);
            BackgroundBrush = NormalizeBrush(backgroundBrush);
            LyricBrush = NormalizeBrush(lyricBrush);
            KaraokeBrush = NormalizeBrush(karaokeBrush);
            LyricStrokeBrush = NormalizeBrush(lyricStrokeBrush);
            KaraokeStrokeBrush = NormalizeBrush(karaokeStrokeBrush);

            if (KaraokeStrokeBrush is SolidColorBrush _brush
                && _brush.Opacity * _brush.Color.A == 0)
            {
                KaraokeStrokeBrush = LyricStrokeBrush;
            }

            var color = Colors.Black;

            if (BackgroundBrush is SolidColorBrush scb)
            {
                color = scb.Color;
            }

            var l = (color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f) / 255f;

            ForegroundBrush = l >= 0.65 ? new SolidColorBrush(Color.FromArgb(230, 0, 0, 0)) : new SolidColorBrush(Color.FromArgb(204, 255, 255, 255));
        }

        public string Name { get; }

        public Brush? BorderBrush { get; }

        public Brush? BackgroundBrush { get; }

        public Brush? LyricBrush { get; }

        public Brush? KaraokeBrush { get; }

        public Brush? LyricStrokeBrush { get; }

        public Brush? KaraokeStrokeBrush { get; }

        public Brush? ForegroundBrush { get; }

        private static Brush NormalizeBrush(Brush? brush)
        {
            if (brush == null) return TransparentBrush;
            else if (brush is SolidColorBrush scb)
            {
                if (scb.Opacity == 1) return brush;
                else if (scb.Opacity == 0) return TransparentBrush;
                else return new SolidColorBrush(
                    Color.FromArgb(
                        (byte)Math.Min(Math.Max((int)(scb.Opacity * scb.Color.A), 0), 255),
                        scb.Color.R,
                        scb.Color.G,
                        scb.Color.B));
            }
            else return TransparentBrush;
        }
    }
}
