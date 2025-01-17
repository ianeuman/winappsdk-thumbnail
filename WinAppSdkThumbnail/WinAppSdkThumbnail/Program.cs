using System;
using System.Collections.Generic;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;

if (args.Length != 2)
{
    Console.WriteLine("Usage: WinAppSdkThumbnail <videoFilePath> <outputDirectoryPath>");
    return;
}

if (!File.Exists(args[0]))
{
    Console.WriteLine("Video file not found.");
    return;
}

if (!Directory.Exists(args[1]))
{
    Console.WriteLine("Output directory not found.");
    return;
}

var videoFilePath = args[0];
var outputDirectoryPath = args[1];

var videoFile = await StorageFile.GetFileFromPathAsync(videoFilePath);
var clip = await MediaClip.CreateFromFileAsync(videoFile);
MediaComposition composition = new();
composition.Clips.Add(clip);
List<TimeSpan> positions = [];
var currentPosition = TimeSpan.Zero;
while (currentPosition < composition.Duration)
{
    positions.Add(currentPosition);
    currentPosition += TimeSpan.FromMilliseconds(100);
}

var properties = clip.GetVideoEncodingProperties();
var thumbnails = await composition.GetThumbnailsAsync(positions, (int)properties.Width, (int)properties.Height, VideoFramePrecision.NearestFrame);

// workaround for the issue is to call GetThumbnailAsync for each position, but it's ~4x slower.
//List<ImageStream> thumbnails = new();
//foreach (var position in positions)
//{
//    thumbnails.Add(await composition.GetThumbnailAsync(position, (int)properties.Width, (int)properties.Height, VideoFramePrecision.NearestFrame));
//}

var frameId = 0;
var directory = await StorageFolder.GetFolderFromPathAsync(outputDirectoryPath);

foreach (var frame in thumbnails)
{
    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(frame);
    using SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

    var file = await directory.CreateFileAsync($"Frame_{frameId++}.jpg", CreationCollisionOption.OpenIfExists);
    using var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);
    encoder.SetSoftwareBitmap(softwareBitmap);
    await encoder.FlushAsync();
}

Console.WriteLine($"Finished!");