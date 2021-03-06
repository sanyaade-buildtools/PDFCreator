﻿using System.Linq;
using SystemInterface.IO;
using SystemWrapper.IO;
using NLog;
using pdfforge.PDFCreator.Core.Settings;
using pdfforge.PDFCreator.Helper;
using pdfforge.PDFCreator.Utilities;

namespace pdfforge.PDFCreator.Startup
{
    internal class AppStartFactory
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ApplicationSettings _appSettings;

        public AppStartFactory() : this(SettingsHelper.Settings.ApplicationSettings)
        {   }

        internal AppStartFactory(ApplicationSettings appSettings)
        {
            _appSettings = appSettings;
        }

        //for testing
        public IAppStart CreateApplicationStart(string[] commandLineArgs)
        {
            return CreateApplicationStart(commandLineArgs, new FileWrap());
        }

        public IAppStart CreateApplicationStart(string[] commandLineArgs, IFile fileWrap)
        {
            if (DragAndDropHelper.IsDragAndDrop(commandLineArgs))
            {
                _logger.Debug("Detected only filenames as parameters: Assuming Drag & Drop");
                var validFiles = DragAndDropHelper.RemoveInvalidFiles(commandLineArgs, fileWrap);
                if(validFiles.Count > 0)
                    return new DragAndDropStart(commandLineArgs);                
            }

            CommandLineParser commandLineParser = new CommandLineParser(commandLineArgs);                                                                                                                                                                                                                                      
            
            if (commandLineParser.HasArgument("PrintFile"))
            {
                var printFile = FindPrintFile(commandLineParser);
                var printerName = FindPrinterName(commandLineParser);
                return new PrintFileStart(printFile, printerName);
            }

            if (ShouldCallInitialize(commandLineParser))
            {
                return new InitializeSettingsStart();
            }

            var appStart = DetermineAppStart(commandLineParser);

            if (commandLineParser.HasArgument("ManagePrintJobs"))
            {
                appStart.StartManagePrintJobs = true;
            }

            return appStart;
        }

        private static bool ShouldCallInitialize(CommandLineParser commandLineParser)
        {
            if (!commandLineParser.HasArgument("InitializeSettings"))
                return false;

            var excludingArguments = new[] {"ManagePrintJobs", "InfoDataFile", "PsFile", "PdfFile"};

            return excludingArguments.All(argument => !commandLineParser.HasArgument(argument));
        }

        private MaybePipedStart DetermineAppStart(CommandLineParser commandLineParser)
        {
            // let's see if we have a new JobInfo passed as command line argument
            var newJob = FindJobInfoFile(commandLineParser);
            if (newJob != null) 
                return new NewPrintJobStart(newJob);
            
            // or a PSFile?
            newJob = FindPSFile(commandLineParser);
            if (newJob != null)
            {
                var printerName = FindPrinterName(commandLineParser); 
                return new NewPsJobStart(newJob, printerName);
            }

            // or a PdfFile?
            newJob = FindPdfFile(commandLineParser);
            if (newJob != null)
            {
                var printerName = FindPrinterName(commandLineParser);
                return new NewPsJobStart(newJob, printerName);
            }

            // ...nope!? We have a MainWindowStart
            return new MainWindowStart();
        }

        private string FindPrintFile(CommandLineParser commandLineParser)
        {
            return commandLineParser.GetArgument("PrintFile");
        }

        private string FindJobInfoFile(CommandLineParser commandLineParser)
        {
            string infFile = null;

            if (!commandLineParser.HasArgument("InfoDataFile"))
                return null;

            _logger.Info("Launched PDFCreator with InfoDataFile parameter.");
            infFile = commandLineParser.GetArgument("InfoDataFile");
            
            _logger.Debug("Recevied \"" + infFile + "\" as command line parameter.");

            return infFile;
        }

        private string FindPSFile(CommandLineParser commandlineParser)
        {
            string psFile = null;

            if (!commandlineParser.HasArgument("PsFile"))
                return null;

            _logger.Info("Launched PDFCreator with PsFile parameter.");
            psFile = commandlineParser.GetArgument("PsFile");
            
            _logger.Debug("Recevied \"" + psFile + "\" as command line parameter.");

            return psFile;
        }

        private string FindPdfFile(CommandLineParser commandlineParser)
        {
            string pdfFile = null;

            if (!commandlineParser.HasArgument("PdfFile"))
                return null;

            _logger.Info("Launched PDFCreator with PdfFile parameter.");
            pdfFile = commandlineParser.GetArgument("PdfFile");

            _logger.Debug("Recevied \"" + pdfFile + "\" as command line parameter.");

            return pdfFile;
        }

        private string FindPrinterName(CommandLineParser commandLineParser)
        {
            if (commandLineParser.HasArgument("PrinterName"))
                return commandLineParser.GetArgument("PrinterName");

            return _appSettings.PrimaryPrinter;
        }
    }
}
