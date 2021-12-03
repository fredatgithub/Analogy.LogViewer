﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Analogy.CommonControls.Managers;
using Analogy.CommonControls.UserControls;
using Analogy.Interfaces;
using DevExpress.Mvvm.Native;

namespace Analogy.CommonControls.DataTypes
{
    public class FileProcessor
    {
        public event EventHandler<EventArgs> OnFileReadingFinished;
        public event EventHandler<string> ProcessingMessage;
        public DateTime lastNewestMessage;
        private string FileName { get; set; }
        public Stream DataStream { get; set; }
        private ILogMessageCreatedHandler DataWindow { get; }
        public UCLogs LogWindow { get; }

        public FileProcessor(ILogMessageCreatedHandler dataWindow)
        {
            DataWindow = dataWindow;
            if (dataWindow is UCLogs logs)
            {
                LogWindow = logs;
            }
        }

        public async Task<IEnumerable<AnalogyLogMessage>> Process(IAnalogyOfflineDataProvider fileDataProvider,
            string filename, CancellationToken token, bool enableFileCaching, bool isReload = false)
        {
            //TODO in case of zip recursive call on all extracted files


            FileName = filename;
            if (string.IsNullOrEmpty(FileName))
            {
                return new List<AnalogyLogMessage>();
            }

            if (!isReload && !DataWindow.ForceNoFileCaching &&
                FileProcessingManager.Instance.AlreadyProcessed(FileName) &&
                enableFileCaching) //get it from the cache
            {
                var cachedMessages = FileProcessingManager.Instance.GetMessages(FileName);
                DataWindow.AppendMessages(cachedMessages, Utils.GetFileNameAsDataSource(FileName));
                if (LogWindow != null)
                {
                    Interlocked.Decrement(ref LogWindow.fileLoadingCount);
                }

                return cachedMessages;

            }

            if (FileProcessingManager.Instance.IsFileCurrentlyBeingProcessed(FileName))
            {
                while (FileProcessingManager.Instance.IsFileCurrentlyBeingProcessed(FileName))
                {
                    await Task.Delay(1000);
                }

                var cachedMessages = FileProcessingManager.Instance.GetMessages(FileName);
                DataWindow.AppendMessages(cachedMessages, Utils.GetFileNameAsDataSource(FileName));
                if (LogWindow != null)
                {
                    Interlocked.Decrement(ref LogWindow.fileLoadingCount);
                }

                return cachedMessages;

            }

            //otherwise read file:
            try
            {

                if (fileDataProvider.CanOpenFile(filename)) //if can open natively: add to processing and process
                {
                    FileProcessingManager.Instance.AddProcessingFile(FileName);

                    var messages = (await fileDataProvider.Process(filename, token, DataWindow).ConfigureAwait(false))
                        .ToList();
                    FileProcessingManager.Instance.DoneProcessingFile(messages.ToList(), FileName);
                    if (messages.Any())
                    {
                        lastNewestMessage = messages.Select(m => m.Date).Max();
                    }

                    OnFileReadingFinished?.Invoke(this, EventArgs.Empty);
                    if (LogWindow != null)
                    {
                        Interlocked.Decrement(ref LogWindow.fileLoadingCount);
                    }

                    return messages;
                }
                else //cannot open natively. is it compressed file?
                {
                    if (Utils.IsCompressedArchive(filename))
                    {
                        var compressedMessages = new List<AnalogyLogMessage>();
                        string extractedPath = UnzipFilesIntoTempFolder(filename, fileDataProvider);
                        if (string.IsNullOrEmpty(extractedPath))
                        {
                            string msg = $"File is not supported by {fileDataProvider}. File: {filename}";
                            ProcessingMessage?.Invoke(this, msg);
                            AnalogyLogMessage error = new AnalogyErrorMessage(msg, "Analogy");
                            error.Source = nameof(FileProcessor);
                            error.Module = "Analogy";
                            DataWindow.AppendMessage(error, fileDataProvider.GetType().FullName);
                            return new List<AnalogyLogMessage> { error };
                        }
                        var files = Directory.GetFiles(extractedPath);
                        files.ForEach(async file =>
                        {
                            var messages = await Process(fileDataProvider, file, token, isReload);
                            compressedMessages.AddRange(messages);
                        });
                        return compressedMessages;

                    }
                    else
                    {
                        string msg = $"File is not supported by {fileDataProvider}. File: {filename}";
                        ProcessingMessage?.Invoke(this, msg);
                        AnalogyLogMessage error = new AnalogyErrorMessage(msg, "Analogy");
                        error.Source = nameof(FileProcessor);
                        error.Module = "Analogy";
                        DataWindow.AppendMessage(error, fileDataProvider.GetType().FullName);
                        return new List<AnalogyLogMessage> { error };
                    }
                }
            }
            catch (Exception e)
            {
                ProcessingMessage?.Invoke(this, $"Error parsing file: {e}");
                var error = new AnalogyErrorMessage($"Error reading file {filename}: Error: {e.Message}", "Analogy");
                error.Source = nameof(FileProcessor);
                error.Module = "Analogy";
                DataWindow.AppendMessage(error, fileDataProvider.GetType().FullName);
                return new List<AnalogyLogMessage> { error };
            }
        }


        private string UnzipFilesIntoTempFolder(string zipPath, IAnalogyOfflineDataProvider fileDataProvider)
        {
            string extractPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(extractPath);
            if (zipPath.EndsWith("zip", StringComparison.InvariantCultureIgnoreCase))
            {
                UnzipZipFileIntoTempFolder(zipPath, extractPath, fileDataProvider);
            }
            else if (zipPath.EndsWith("gz", StringComparison.InvariantCultureIgnoreCase))
            {
                UnzipGzFileIntoTempFolder(zipPath, extractPath);
            }
            else
            {
                ProcessingMessage?.Invoke(this, $"Unsupported file: {zipPath}.");
                return string.Empty;
            }

            return extractPath;
        }

        private void UnzipGzFileIntoTempFolder(string zipPath, string extractPath)
        {
            FileInfo fileToDecompress = new FileInfo(zipPath);
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.Name;
                string newFileName = Path.Combine(extractPath, currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length));

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        private void UnzipZipFileIntoTempFolder(string zipPath, string extractPath, IAnalogyOfflineDataProvider fileDataProvider)
        {

            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {

                    //build a list of files to be extracted
                    var entries = archive.Entries.Where(entry => !entry.FullName.EndsWith("/") && fileDataProvider.CanOpenFile(entry.FullName));
                    foreach (ZipArchiveEntry entry in entries)
                    {
                        entry.ExtractToFile(Path.Combine(extractPath, entry.Name));
                    }

                    if (!Directory.GetFiles(extractPath).Any())
                    {
                        ProcessingMessage?.Invoke(this, "Zip file does not contain any supported files");
                    }

                }
            }
        }
    }
}