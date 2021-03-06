﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YoutubeExplode.Internal;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Services;

#if NETSTANDARD2_0 || NET45 || NETCOREAPP1_0
using System.IO;
using System.Text;
using System.Threading;
#endif

namespace YoutubeExplode
{
    public partial class YoutubeClient
    {
        /// <summary>
        /// Gets the actual closed caption track represented by given metadata
        /// </summary>
        public async Task<ClosedCaptionTrack> GetClosedCaptionTrackAsync(ClosedCaptionTrackInfo info)
        {
            info.GuardNotNull(nameof(info));

            // Get manifest
            var response = await _httpService.GetStringAsync(info.Url).ConfigureAwait(false);
            var trackXml = XElement.Parse(response).StripNamespaces();

            // Parse captions
            var captions = new List<ClosedCaption>();
            foreach (var captionXml in trackXml.Descendants("text"))
            {
                var text = captionXml.Value;
                var offset = TimeSpan.FromSeconds((double) captionXml.AttributeStrict("start"));
                var duration = TimeSpan.FromSeconds((double) captionXml.AttributeStrict("dur"));

                var caption = new ClosedCaption(text, offset, duration);
                captions.Add(caption);
            }

            return new ClosedCaptionTrack(info, captions);
        }

#if NETSTANDARD2_0 || NET45 || NETCOREAPP1_0

        /// <summary>
        /// Downloads a closed caption track to file
        /// </summary>
        public async Task DownloadClosedCaptionTrackAsync(ClosedCaptionTrackInfo info, string filePath,
            IProgress<double> progress, CancellationToken cancellationToken)
        {
            info.GuardNotNull(nameof(info));
            filePath.GuardNotNull(nameof(filePath));

            // Get the track
            var track = await GetClosedCaptionTrackAsync(info).ConfigureAwait(false);

            // Save to file as SRT
            using (var writer = File.CreateText(filePath))
            {
                for (var i = 0; i < track.Captions.Count; i++)
                {
                    // Make sure cancellation was not requested
                    cancellationToken.ThrowIfCancellationRequested();

                    var caption = track.Captions[i];
                    var buffer = new StringBuilder();

                    // Line number
                    buffer.AppendLine((i + 1).ToString());

                    // Time start --> time end
                    buffer.Append(caption.Offset.ToString(@"hh\:mm\:ss\,fff"));
                    buffer.Append(" --> ");
                    buffer.Append((caption.Offset + caption.Duration).ToString(@"hh\:mm\:ss\,fff"));
                    buffer.AppendLine();

                    // Actual text
                    buffer.AppendLine(caption.Text);

                    // Write to stream
                    await writer.WriteLineAsync(buffer.ToString()).ConfigureAwait(false);

                    // Report progress
                    progress?.Report((i + 1.0) / track.Captions.Count);
                }
            }
        }

        /// <summary>
        /// Downloads a closed caption track to file
        /// </summary>
        public Task DownloadClosedCaptionTrackAsync(ClosedCaptionTrackInfo info, string filePath,
            IProgress<double> progress)
            => DownloadClosedCaptionTrackAsync(info, filePath, progress, CancellationToken.None);

        /// <summary>
        /// Downloads a closed caption track to file
        /// </summary>
        public Task DownloadClosedCaptionTrackAsync(ClosedCaptionTrackInfo info, string filePath)
            => DownloadClosedCaptionTrackAsync(info, filePath, null);

#endif
    }
}