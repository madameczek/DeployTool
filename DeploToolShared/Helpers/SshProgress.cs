﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DeployTool.Helpers
{
    internal class SshProgress
    {
        private long _lastFilePartial;
        private Action<SshProgress> _onTransfer;
        private DateTime _lastUpdate;
        private TimeSpan _minRefresh = TimeSpan.FromMilliseconds(750);
        private object _lock = new object();

        public SshProgress(long totalTransferSize, long numberOfFiles, Action<SshProgress> onTransfer)
        {
            Reset(totalTransferSize, numberOfFiles);
            _onTransfer = onTransfer;
        }

        public long Percent { get; private set; }
        public long TotalNumberOfFiles { get; private set; }
        public long TotalTransferSize { get; private set; }
        public long AlreadyTransferredSize { get; private set; }
        public string TotalTransferSizeWithSuffix { get; private set; }
        public string CurrentFilename { get; private set; }
        public long CurrentFileIndex { get; private set; }
        public string FormattedString { get; private set; }

        public void Reset(long totalTransferSize, long numberOfFiles)
        {
            TotalTransferSize = totalTransferSize;
            TotalNumberOfFiles = numberOfFiles;

            _lastFilePartial = 0;
            Percent = 0;
            AlreadyTransferredSize = 0;
            TotalTransferSizeWithSuffix = string.Empty;
            CurrentFilename = string.Empty;
            CurrentFileIndex = 0;
            FormattedString = string.Empty;
            _lastUpdate = DateTime.Now;
        }

        public void UpdateProgress(string filename, long size, long partial)
        {
            lock (_lock)
            {
                if (CurrentFilename != filename)
                {
                    _lastFilePartial = 0;

                    //_relativesize += _lastFileSize;
                    //_lastFileSize = size;
                    CurrentFilename = filename;
                    CurrentFileIndex++;
                }

                var delta = partial - _lastFilePartial;
                _lastFilePartial = partial;
                AlreadyTransferredSize += delta;

                var percent = AlreadyTransferredSize * 100 / TotalTransferSize;
                var msg = $"{percent}% {CurrentFileIndex}/{TotalNumberOfFiles} {filename} ";
                var filler = new string(' ', Console.WindowWidth - msg.Length - 1);
                FormattedString = msg + filler;
                //ConsoleManager.WriteAt(0, _cursorTop, FormattedString);
                if (DateTime.Now - _lastUpdate > _minRefresh)
                {
                    _lastUpdate = DateTime.Now;
                    _onTransfer(this);
                }
            }
        }

        public void UpdateProgressFinal(long? currentCount = null)
        {
            lock (_lock)
            {
                if (TotalTransferSize != 0)
                {
                    long total;
                    if (currentCount.HasValue)
                        total = currentCount.Value;
                    else
                        total = TotalNumberOfFiles;

                    //var percent = AlreadyTransferredSize * 100 / TotalTransferSize;
                    var percent = total * 100 / TotalNumberOfFiles;
                    var msg = $"{percent}% {TotalNumberOfFiles} File(s), {FormatSize(TotalTransferSize)}";
                    var filler = new string(' ', Console.WindowWidth - msg.Length - 1);
                    FormattedString = msg + filler;
                    //ConsoleManager.WriteAt(0, _cursorTop, FormattedString);
                    _onTransfer(this);
                }
            }
        }

        public static string FormatSize(long totalsize)
        {
            string[] prefixes = { "bytes", "Kb", "Mb", "Gb", "Tb" };
            int index = 0;
            long size = totalsize;
            while (index < prefixes.Length - 1 && size >= 1024)
            {
                size = size / 1024;
                index++;
            }

            return $"{size.ToString("0.#")} {prefixes[index]}";
        }
    }
}
