﻿using Philips.Analogy.Interfaces;
using Philips.Analogy.Interfaces.Interfaces;
using Philips.Analogy.LogLoaders;
using Philips.Analogy.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Philips.Analogy.DataSources
{
    public class AnalogyOfflineFactory : IAnalogyFactories
    {
        public Guid FactoryID { get; } = new Guid("D3047F5D-CFEB-4A69-8F10-AE5F4D3F2D04");
        public string Title { get; } = "Analogy Built-in Logs Features";
        public IAnalogyDataSourceFactory DataSources { get; }
        public IAnalogyCustomActionFactory Actions { get; }
        public IAnalogyUserControlFactory UserControls { get; }
        public IEnumerable<IAnalogyChangeLog> ChangeLog { get; }
        public IEnumerable<string> Contributors { get; } = new List<string> { "Lior Banai" };

        public AnalogyOfflineFactory()
        {
            DataSources = new AnalogyOfflineDataSourceFactory();
            Actions = new AnalogyCustomActionFactory();
            ChangeLog = new List<IAnalogyChangeLog>();
        }
    }

    public class AnalogyOfflineDataSourceFactory : IAnalogyDataSourceFactory
    {
        public string Title { get; } = "Analogy Built-In Data Sources";
        public IEnumerable<IAnalogyDataSource> Items { get; }

        public AnalogyOfflineDataSourceFactory()
        {
            Items = new List<IAnalogyDataSource> { new AnalogyOfflineDataSource() };
        }
    }

    public class AnalogyOfflineDataSource : IAnalogyOfflineDataSource
    {
        public Guid ID { get; } = new Guid("A475EB76-2524-49D0-B931-E800CB358106");

        public bool CanSaveToLogFile { get; } = true;
        public string FileOpenDialogFilters { get; } = "All supported Analogy log file types|*.log;*.json|Plain Analogy XML log file (*.log)|*.log|Analogy JSON file (*.json)|*.json";
        public string FileSaveDialogFilters { get; } = "Plain Analogy XML log file (*.log)|*.log|Analogy JSON file (*.json)|*.json";
        public IEnumerable<string> SupportFormats { get; } = new[] { "*.log", "*.json" };
        public string InitialFolderFullPath { get; } = Environment.CurrentDirectory;
        public Image OptionalOpenFolderImage { get; }
        public Image OptionalOpenFilesImage { get; }
        public void InitDataSource()
        {

        }
        public async Task<IEnumerable<AnalogyLogMessage>> Process(string fileName, CancellationToken token, ILogMessageCreatedHandler messagesHandler)
        {
            if (fileName.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase))
            {
                AnalogyXmlLogFile logFile = new AnalogyXmlLogFile();
                var messages = await logFile.ReadFromFile(fileName, token, messagesHandler);
                return messages;

            }
            if (fileName.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                AnalogyJsonLogFile logFile = new AnalogyJsonLogFile();
                var messages = await logFile.ReadFromFile(fileName, token, messagesHandler);
                return messages;
            }
            else
            {
                AnalogyLogMessage m = new AnalogyLogMessage();
                m.Text = $"Unsupported file: {fileName}. Skipping file";
                m.Level = AnalogyLogLevel.Critical;
                m.Source = "Analogy";
                m.Module = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                m.ProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;
                m.Class = AnalogyLogClass.General;
                m.User = Environment.UserName;
                m.Date = DateTime.Now;
                messagesHandler.AppendMessage(m, Environment.MachineName);
                return new List<AnalogyLogMessage>() { m };
            }
        }

        public IEnumerable<FileInfo> GetSupportedFiles(DirectoryInfo dirInfo, bool recursiveLoad)
        => GetSupportedFilesInternal(dirInfo, recursiveLoad);

        public Task SaveAsync(List<AnalogyLogMessage> messages, string fileName)

            => Task.Factory.StartNew(async () =>
            {

                if (fileName.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase))
                {
                    AnalogyXmlLogFile logFile = new AnalogyXmlLogFile();
                    await logFile.Save(messages, fileName);

                }
                if (fileName.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                {
                    AnalogyJsonLogFile logFile = new AnalogyJsonLogFile();
                    await logFile.Save(messages, fileName);

                }
            });

        public bool CanOpenFile(string fileName)

            => fileName.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase) ||
                fileName.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase);


        public bool CanOpenAllFiles(IEnumerable<string> fileNames) => fileNames.All(CanOpenFile);

        public static List<FileInfo> GetSupportedFilesInternal(DirectoryInfo dirInfo, bool recursive)
        {
            List<FileInfo> files = dirInfo.GetFiles("*.log")
                .Concat(dirInfo.GetFiles("*.json"))
                .ToList();
            if (!recursive)
                return files;
            try
            {
                foreach (DirectoryInfo dir in dirInfo.GetDirectories())
                {
                    files.AddRange(GetSupportedFilesInternal(dir, true));
                }
            }
            catch (Exception)
            {
                return files;
            }

            return files;
        }
    }



    public class AnalogyCustomActionFactory : IAnalogyCustomActionFactory
    {
        public string Title { get; } = "Analogy Built-In tools";
        public IEnumerable<IAnalogyCustomAction> Items { get; }

        public AnalogyCustomActionFactory()
        {
            Items = new List<IAnalogyCustomAction>() { new AnalogyCustomAction() };
        }
    }

    public class AnalogyCustomAction : IAnalogyCustomAction
    {
        public Action Action => () =>
        {
            var p = new ProcessNameAndID();
            p.Show();
        };
        public Guid ID { get; } = new Guid("8D24EC70-60C0-4823-BE9C-F4A59303FFB3");
        public Image Image { get; } = Resources.ChartsShowLegend_32x32;
        public string Title { get; } = "Process Identifier";

    }
}

