﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Analogy.Common.DataTypes;
using Analogy.Common.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Analogy.UnitTests
{
    [TestClass]
    public class FileProcessorTests
    {
        private CancellationTokenSource cancellationTokenSource;
        private MessageHandlerForTesting handler = new MessageHandlerForTesting();
        [TestMethod]
        public async Task TestWriteAndRead()
        {
            string fileName = "test_recursive.zip";
            cancellationTokenSource = new CancellationTokenSource();
            var settings = new DefaultUserSettingsManager
            {
                EnableCompressedArchives = true
            };
            FileProcessor fp = new FileProcessor(settings, handler, new EmptyAnalogyLogger());
            OfflineDataProviderForTesting offlineDataProvider = new OfflineDataProviderForTesting();
            var result = await fp.Process(offlineDataProvider, fileName, cancellationTokenSource.Token, false);
            Assert.IsTrue(result.Count() == 40);
        }
    }
}
