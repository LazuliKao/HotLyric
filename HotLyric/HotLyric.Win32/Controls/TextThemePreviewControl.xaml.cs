using HotLyric.Win32.Controls.LyricControlDrawingData;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils.LyricFiles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;
using BlueFire.Toolkit.WinUI3.Text;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace HotLyric.Win32.Controls
{
    public sealed partial class TextThemePreviewControl : UserControl
    {
        private LyricDrawingLine? line;
        private LyricDrawingTextColors colors;

        private SolidColorBrush backgroundBrush;
        private SolidColorBrush borderBrush;

        private IReadOnlyList<CanvasFontFamily> canvasFontFamilies;

        public TextThemePreviewControl()
        {
            this.InitializeComponent();
            this.Loaded += TextThemePreviewControl_Loaded;
            this.Unloaded += TextThemePreviewControl_Unloaded;

            backgroundBrush = new SolidColorBrush();
            borderBrush = new SolidColorBrush();

            BackgroundBorder.Background = backgroundBrush;
            BackgroundBorder.BorderBrush = borderBrush;

            colors = new LyricDrawingTextColors();

            canvasFontFamilies = new FontFamilySets("SYSTEM-UI").BuildCanvasFontFamilies();
        }

        private void TextThemePreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            CanvasControl.Draw += CanvasControl_Draw;
            CanvasControl.Invalidate();
        }

        private void TextThemePreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CanvasControl.Draw -= CanvasControl_Draw;
        }

        private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            try
            {
                if (line == null)
                {
                    line = new LyricDrawingLine(
                        args.DrawingSession,
                        sender.Size,
                        new SampleLine("字"),
                        canvasFontFamilies,
                        FontWeight,
                        FontStyle,
                        LyricDrawingLineType.Classic,
                        LyricDrawingLineAlignment.Center,
                        1,
                        LyricDrawingLineTextSizeType.DrawSize);
                }

                line.Draw(args.DrawingSession, new LyricDrawingParameters(0.5, 1, true, LyricControlProgressAnimationMode.Karaoke, colors));
            }
            catch (Exception ex) when (sender.Device.IsDeviceLost(ex.HResult))
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
                sender.Device.RaiseDeviceLost();
            }
        }


        public LyricThemeView Theme
        {
            get { return (LyricThemeView)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(LyricThemeView), typeof(TextThemePreviewControl), new PropertyMetadata(null, (s, a) =>
            {
                if (s is TextThemePreviewControl sender)
                {
                    sender.UpdateTheme();
                }
            }));

        private void UpdateTheme()
        {
            if (Theme == null) return;

            backgroundBrush.Color = Theme.BackgroundColor;
            borderBrush.Color = Theme.BorderColor;

            colors.FillColor1 = Theme.LyricColor;
            colors.FillColor2 = Theme.KaraokeColor;
            colors.StrokeColor1 = Theme.LyricStrokeColor;
            colors.StrokeColor2 = Theme.KaraokeStrokeColor;

            if (IsLoaded)
            {
                CanvasControl.Invalidate();
            }
        }

        private class SampleLine : ILyricLine
        {
            public SampleLine(string text)
            {
                Text = text;
            }

            public TimeSpan StartTime => TimeSpan.FromSeconds(0);

            public TimeSpan EndTime => TimeSpan.FromSeconds(1);

            public bool IsEndLine => false;

            public string Text { get; }

            public IReadOnlyList<ILyricLineSpan> AllSpans => throw new NotImplementedException();

            public ILyricLineSpan? GetCurrentOrNextSpan(TimeSpan time)
            {
                throw new NotImplementedException();
            }

            public ILyricLineSpan? GetCurrentSpan(TimeSpan time)
            {
                throw new NotImplementedException();
            }

            public ILyricLineSpan? GetNextSpan(TimeSpan time)
            {
                throw new NotImplementedException();
            }
        }
    }
}
