﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using pdfforge.DataStorage.Storage;
using pdfforge.PDFCreator.Core.Settings;
using pdfforge.PDFCreator.Helper;
using pdfforge.PDFCreator.Shared.Helper;
using pdfforge.PDFCreator.Shared.Helper.Logging;
using pdfforge.PDFCreator.Shared.ViewModels;
using pdfforge.PDFCreator.Shared.Views;
using pdfforge.PDFCreator.ViewModels.UserControls;

namespace pdfforge.PDFCreator.Views.UserControls
{
    internal partial class DebugTab : UserControl
    {
        private static readonly TranslationHelper TranslationHelper = TranslationHelper.Instance;

        public DebugTab()
        {
            InitializeComponent();
            InitializeComponent();
            if (TranslationHelper.IsInitialized)
            {
                TranslationHelper.TranslatorInstance.Translate(this);
            }
        }

        public Action<PdfCreatorSettings> UpdateSettings { get; set; }

        public DebugTabViewModel ViewModel
        {
            get { return (DebugTabViewModel) DataContext; }
        }

        private void ShowLogButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists(LoggingHelper.LogFile))
            {
                Process.Start(LoggingHelper.LogFile);
            }
            else
            {
                var caption = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow", "NoLogFile",
                    "No log file");
                var message = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow",
                    "NoLogFileAvailable", "There is currently no log file available.");
                MessageWindow.ShowTopMost(message, caption, MessageWindowButtons.OK, MessageWindowIcon.Warning);
            }
        }

        private void ClearLogButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists(LoggingHelper.LogFile))
            {
                File.WriteAllText(LoggingHelper.LogFile, "");
            }
        }

        private void PdfCreatorTestPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!QuerySaveModifiedSettings())
                return;

            JobInfoQueue.Instance.AddTestPage();
        }

        private void WindowsTestPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!QuerySaveModifiedSettings())
                return;
            var printerHelper = new Shared.Helper.PrinterHelper();
            printerHelper.PrintWindowsTestPage(ViewModel.ApplicationSettings.PrimaryPrinter);
        }

        private bool QuerySaveModifiedSettings()
        {
            if (!AppSettingsAreModified())
                return true; //No changes -> proceed

            var message = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow",
                "AskSaveModifiedSettings",
                "You have modified the application settings. In order to print a test page with the new settings, you have to save them first.\r\nDo you want to save the application settings now?");
            var caption = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow", "AppSettings",
                "Application Settings");

            var response = MessageWindow.ShowTopMost(message, caption, MessageWindowButtons.YesNo,
                MessageWindowIcon.Question);
            if (response == MessageWindowResponse.Yes) //Proceed with saved settings
            {
                SaveAppSettings();
                return true;
            }
            if (response == MessageWindowResponse.No) //Proceed with old settings
            {
                return true;
            }
            return false; //Cancel Testprinting
        }

        private bool AppSettingsAreModified()
        {
            return !ViewModel.ApplicationSettings.Equals(SettingsHelper.Settings.ApplicationSettings);
        }

        private void SaveAppSettings()
        {
            SettingsHelper.Settings.ApplicationSettings = ViewModel.ApplicationSettings.Copy();
            SettingsHelper.ApplySettings(SettingsHelper.Settings);
        }

        private void SaveIniButton_OnClick(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow",
                "IniFileFilter", "INI files (*.ini)|*.ini");
            saveFileDialog.FileName = "PDFCreator.ini";

            if (saveFileDialog.ShowDialog() != true)
                return;

            var ini = new IniStorage(Encoding.UTF8);
            SettingsHelper.Settings.SaveData(ini, saveFileDialog.FileName);
        }

        private void LoadIniButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow",
                "IniFileFilter", "INI files (*.ini)|*.ini");
            openFileDialog.FileName = "PDFCreator.ini";

            if (openFileDialog.ShowDialog() != true)
                return;

            var message = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow",
                "LoadSettingsFromFileWarning",
                "By loading the file all settings and profiles will be overwritten. Do you want to continue?");
            var caption = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow", "OverwriteAllSettings",
                "Overwrite all settings");
            if (MessageWindow.ShowTopMost(message, caption, MessageWindowButtons.YesNo, MessageWindowIcon.Warning) !=
                MessageWindowResponse.Yes)
                return;

            var ini = new IniStorage(Encoding.UTF8);

            var settings = SettingsHelper.CreateEmptySettings();
            settings.LoadData(ini, openFileDialog.FileName, SettingsHelper.UpgradeSettings);

            if (!SettingsHelper.CheckValidSettings(settings))
            {
                message = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow",
                    "InvalidSettingsWarning",
                    "The file does not appear to contain valid PDFCreator settings.");
                caption = TranslationHelper.Instance.TranslatorInstance.GetTranslation("ApplicationSettingsWindow", "InvalidSettings",
                    "Invalid settings");

                MessageWindow.ShowTopMost(message, caption, MessageWindowButtons.OK, MessageWindowIcon.Error);
                return;
            }

            SettingsHelper.ApplySettings(settings);

            if (UpdateSettings != null)
            {
                UpdateSettings(settings);
            }

            var viewModel = ViewModel;
            DataContext = null;
            viewModel.ApplicationSettings = settings.ApplicationSettings;
            DataContext = viewModel;
        }
    }
}