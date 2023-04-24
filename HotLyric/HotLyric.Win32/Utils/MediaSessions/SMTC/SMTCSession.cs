﻿using HotLyric.Win32.Utils.MediaSessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Media.Control;
using Microsoft.UI.Xaml.Media;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    public partial class SMTCSession : ISMTCSession
    {
        private bool disposedValue;
        private GlobalSystemMediaTransportControlsSession session;
        private string appUserModelId;
        private TaskCompletionSource<MediaSessionMediaProperties?>? mediaPropertiesSource;
        private GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo;
        private GlobalSystemMediaTransportControlsSessionTimelineProperties? timelineProperties;
        private TaskCompletionSource<Package?>? packageSource;

        private SMTCCommand playCommand;
        private SMTCCommand pauseCommand;
        private SMTCCommand skipPreviousCommand;
        private SMTCCommand skipNextCommand;

        private TimeSpan lastPosition = TimeSpan.Zero;
        private DateTime lastUpdatePositionTime = default;
        private Timer? internalPositionTimer;

        public SMTCSession(GlobalSystemMediaTransportControlsSession session, SMTCApp app)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            PositionMode = app.PositionMode;
            App = app;
            appUserModelId = session.SourceAppUserModelId;

            session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
            session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
            session.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;

            playbackInfo = session.GetPlaybackInfo();
            timelineProperties = session.GetTimelineProperties();

            playCommand = new SMTCCommand(async () => await session.TryPlayAsync());
            pauseCommand = new SMTCCommand(async () => await session.TryPauseAsync());
            skipPreviousCommand = new SMTCCommand(async () => await session.TrySkipPreviousAsync());
            skipNextCommand = new SMTCCommand(async () => await session.TrySkipNextAsync());

            UpdateTimelineProperties();
            UpdatePlaybackInfo();

            if (PositionMode == SMTCAppPositionMode.FromAppAndUseTimer || PositionMode == SMTCAppPositionMode.OnlyUseTimer)
            {
                internalPositionTimer = new Timer(300);

                internalPositionTimer.Elapsed += InternalPositionTimer_Elapsed;

                if (PositionMode == SMTCAppPositionMode.OnlyUseTimer)
                {
                    lastUpdatePositionTime = DateTime.Now;
                    UpdateInternalTimerState();
                }
            }
        }

        private void InternalPositionTimer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            var pos = TimeSpan.FromSeconds((DateTime.Now - lastUpdatePositionTime).TotalSeconds * PlaybackRate + lastPosition.TotalSeconds);
            if (PositionMode == SMTCAppPositionMode.OnlyUseTimer
                || pos >= StartTime && pos <= EndTime)
            {
                Position = pos;
            }

            UpdateInternalTimerState();

            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateInternalTimerState()
        {
            if (timelineProperties == null || PlaybackStatus != MediaSessionPlaybackStatus.Playing)
            {
                internalPositionTimer?.Stop();
            }
            else
            {
                lastPosition = Position;
                lastUpdatePositionTime = DateTime.Now;
                if (internalPositionTimer?.Enabled == false)
                {
                    internalPositionTimer?.Start();
                }
            }
        }

        private void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            timelineProperties = session.GetTimelineProperties();
            UpdateInternalTimerState();

            if (PositionMode != SMTCAppPositionMode.OnlyUseTimer)
            {
                UpdateTimelineProperties();
                PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            playbackInfo = session.GetPlaybackInfo();
            UpdatePlaybackInfo();
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            if (PositionMode == SMTCAppPositionMode.OnlyUseTimer)
            {
                lastPosition = TimeSpan.Zero;
                lastUpdatePositionTime = DateTime.Now;
                Position = TimeSpan.Zero;
                PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
            }

            mediaPropertiesSource = null;
            MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTimelineProperties()
        {
            if (timelineProperties != null)
            {
                StartTime = timelineProperties.StartTime;
                EndTime = timelineProperties.EndTime;
                Position = timelineProperties.Position;

                if (PositionMode != SMTCAppPositionMode.OnlyUseTimer)
                {
                    lastUpdatePositionTime = DateTime.Now;
                    lastPosition = Position;
                }
            }
            else
            {
                StartTime = TimeSpan.Zero;
                EndTime = TimeSpan.Zero;
                Position = TimeSpan.Zero;

                if (PositionMode != SMTCAppPositionMode.OnlyUseTimer)
                {
                    lastUpdatePositionTime = default;
                    lastPosition = TimeSpan.Zero;
                    internalPositionTimer?.Stop();
                }
            }
        }

        private void UpdatePlaybackInfo()
        {
            if (playbackInfo != null)
            {
                var rate = playbackInfo.PlaybackRate ?? 1;
                PlaybackRate = rate > 0.001 ? rate : 1;

                PlaybackStatus = playbackInfo.PlaybackStatus switch
                {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => MediaSessionPlaybackStatus.Closed,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened => MediaSessionPlaybackStatus.Opened,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing => MediaSessionPlaybackStatus.Changing,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaSessionPlaybackStatus.Stopped,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaSessionPlaybackStatus.Playing,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaSessionPlaybackStatus.Paused,
                    _ => MediaSessionPlaybackStatus.Closed
                };
            }
            else
            {
                PlaybackRate = 1;
                PlaybackStatus = MediaSessionPlaybackStatus.Stopped;
            }

            UpdateInternalTimerState();

            UpdateControls();
        }

        private void UpdateControls()
        {
            if (playbackInfo != null)
            {
                playCommand.CanExecute = playbackInfo.Controls.IsPlayEnabled;
                pauseCommand.CanExecute = playbackInfo.Controls.IsPauseEnabled;
                skipPreviousCommand.CanExecute = playbackInfo.Controls.IsPreviousEnabled;
                skipNextCommand.CanExecute = playbackInfo.Controls.IsNextEnabled;
            }
            else
            {
                playCommand.CanExecute = false;
                pauseCommand.CanExecute = false;
                skipPreviousCommand.CanExecute = false;
                skipNextCommand.CanExecute = false;
            }
        }

        public async Task<MediaSessionMediaProperties?> GetMediaPropertiesAsync()
        {
            var taskSource = mediaPropertiesSource;
            if (taskSource == null)
            {
                taskSource = new TaskCompletionSource<MediaSessionMediaProperties?>();
                mediaPropertiesSource = taskSource;
                try
                {
                    var mediaProperties = await session?.TryGetMediaPropertiesAsync();

                    var playbackType = mediaProperties?.PlaybackType;

                    taskSource.SetResult(mediaProperties?.PlaybackType switch
                    {
                        global::Windows.Media.MediaPlaybackType.Video => null,
                        global::Windows.Media.MediaPlaybackType.Image => null,
                        _ => CreateMediaProperties(mediaProperties)
                    });
                }
                catch
                {
                    mediaPropertiesSource = null;
                    return null;
                }
            }
            return await taskSource.Task.ConfigureAwait(false);
        }

        public GlobalSystemMediaTransportControlsSession Session => session;

        public double PlaybackRate { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public TimeSpan Position { get; private set; }

        public MediaSessionPlaybackStatus PlaybackStatus { get; private set; }

        public string AppUserModelId => appUserModelId;


        public ICommand PlayCommand => playCommand;
        public ICommand PauseCommand => pauseCommand;
        public ICommand SkipPreviousCommand => skipPreviousCommand;
        public ICommand SkipNextCommand => skipNextCommand;

        public SMTCAppPositionMode PositionMode { get; }

        public MediaSessionApp App { get; }

        public bool IsDisposed => disposedValue;

        private MediaSessionMediaProperties? CreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaProperties == null) return null;

            int skip = 0;

            var neteaseMusicId = string.Empty;
            var localLrcPath = string.Empty;

            var genres = mediaProperties.Genres?.ToArray();

            if (genres != null)
            {
                if (genres.Length > 0 && genres[0]?.StartsWith("ncm-", StringComparison.OrdinalIgnoreCase) == true)
                {
                    neteaseMusicId = genres[0].Substring(4);
                    skip++;
                }

                if (genres.Length > 1
                    && !string.IsNullOrEmpty(genres[1])
                    && genres[1].Trim() is string path
                    && !Path.IsPathRooted(path))
                {
                    localLrcPath = path;
                    skip++;
                }
            }

            if (skip > 0)
            {
                genres = genres?.Skip(skip).ToArray();
            }

            return new MediaSessionMediaProperties(
                mediaProperties.AlbumArtist,
                mediaProperties.AlbumTitle,
                mediaProperties.AlbumTrackCount,
                mediaProperties.Artist,
                neteaseMusicId,
                localLrcPath,
                genres ?? Array.Empty<string>(),
                mediaProperties.Subtitle,
                mediaProperties.Title,
                mediaProperties.TrackNumber);
        }

        private async Task<Package?> GetAppPackageAsync()
        {
            if (packageSource == null)
            {
                packageSource = new TaskCompletionSource<Package?>();
                var package = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync(appUserModelId);
                packageSource.TrySetResult(package);
            }

            return await packageSource.Task.ConfigureAwait(false);
        }


        public async Task<string?> GetSessionNameAsync()
        {
            if (!string.IsNullOrEmpty(App.CustomName)) return App.CustomName;

            var package = await GetAppPackageAsync();
            return package?.DisplayName;
        }

        public async Task<ImageSource?> GetSessionIconAsync()
        {
            if (App.CustomAppIcon != null) return App.CustomAppIcon;

            var package = await GetAppPackageAsync();
            if (package != null)
            {
                return await ApplicationHelper.GetPackageIconAsync(package);
            }
            return null;
        }

        public event EventHandler? PlaybackInfoChanged;

        public event EventHandler? MediaPropertiesChanged;


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    if (internalPositionTimer != null)
                    {
                        internalPositionTimer.Elapsed -= InternalPositionTimer_Elapsed;
                        internalPositionTimer.Stop();
                        internalPositionTimer = null;
                    }

                    playCommand.CanExecute = false;
                    pauseCommand.CanExecute = false;
                    skipPreviousCommand.CanExecute = false;
                    skipNextCommand.CanExecute = false;

                    session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                    session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                    session.TimelinePropertiesChanged -= Session_TimelinePropertiesChanged;

                    mediaPropertiesSource = null;
                    playbackInfo = null;
                    timelineProperties = null;

                    UpdateTimelineProperties();
                    UpdatePlaybackInfo();

                    session = null!;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SMTCSession()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
