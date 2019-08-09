using System;
using System.Windows;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Qs1DirFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _cancelled; //true if cancel button is clicked
        private int _fileCounter; //counts total files
        private int _filesToRead; //total number of files
        private string _megaText;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private BackgroundWorker _worker; //handles multithreaded UI-engine communication
        public MainWindow()
        {
            InitializeComponent();
            InstantiateWorker();
            _fileCounter = 0;
            _filesToRead = 0;
            _megaText = string.Empty;
        }

        private void InstantiateWorker()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += backgroundDoWork;
            _worker.ProgressChanged += backgroundProgress;
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerCompleted += backgroundFinished;
        }

        //is called by UI main thread and begins the read process
        private void backgroundDoWork(object sender, DoWorkEventArgs e)
        {
            string rootFolder = e.Argument.ToString();
            if (Directory.Exists(rootFolder) == true)
            {
                _filesToRead = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories).Length;
                List<string> dirs = new List<string>(Directory.GetDirectories(rootFolder));
                foreach(string d in dirs)
                {
                    if(IsWithinDateRange(d) == true)
                    {
                        ReadCustomerInfo(d);
                    }
                    _fileCounter = _fileCounter + 1;
                    _worker.ReportProgress((int)(100 * ((double)_fileCounter / (double)_filesToRead)), $"Reading from {(Path.GetFileName(Path.GetDirectoryName(d)))}");
                }
            }
        }
        //is called when .ReportProgress is called by internal DoWork
        private void backgroundProgress(object sender, ProgressChangedEventArgs e)
        {
            ProgBar.Value = (int)e.ProgressPercentage;
            LoadingText.Content = e.UserState.ToString();
        }
        //when reading files are finished, this is called
        private void backgroundFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_cancelled == true)
            {
                _cancelled = false;
                _fileCounter = 0;
                InstantiateWorker();
                return;
            }
            OutputText.Text = DumpContentsToFile();
            LoadView.Visibility = Visibility.Hidden;
            ResultView.Visibility = Visibility.Visible;
            _fileCounter = 0;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelled = true;
            _worker = null;
        }

        private void ChangeRoot(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog newFolderWindow = new FolderBrowserDialog();
            newFolderWindow.ShowNewFolderButton = false;
            newFolderWindow.Description = "Choose an existing folder";
            newFolderWindow.RootFolder = Environment.SpecialFolder.MyComputer;
            newFolderWindow.SelectedPath = RootBox.Text; //\\qau3\Customers\
            newFolderWindow.ShowDialog();
            RootBox.Text = newFolderWindow.SelectedPath;
        }

        private void RootBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(RootBox.Text) == false)
            {
                RootBox.Text = @"D:\QEServer\Customers\";
            }
        }

        //Button clicked, start reading files
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Start doing stuff");
            ParseSettings.Visibility = Visibility.Hidden; //show loading screen
            LoadView.Visibility = Visibility.Visible;
            LoadingText.Content = "";
            ProgBar.Value = 0;
            _startDate = StartDate.SelectedDate;
            _endDate = EndDate.SelectedDate;
            _worker.RunWorkerAsync(RootBox.Text);
        }

        //reads through a given customer folder, searching for CustomerInfo.ini file to parse
        private void ReadCustomerInfo(string path)
        {
            string customerInfoPath = string.Concat(path, @"\CustomerInfo.ini");
            if(File.Exists(customerInfoPath) == false)
            {
                Console.WriteLine(string.Concat("No file found for ", path));
                return;
            }

            Ini meep = new Ini(customerInfoPath); //transform ini file to a nice ini class


            Dictionary<string, string> qs1DirList = meep.GetSection("QS1Information");
            foreach(var prop in qs1DirList)
            {
                if(string.Compare(prop.Key, "NumberQS1Dirs") != 0)
                {
                    string line = $"SysNum: {prop.Value}, Name: {meep.GetProperty(prop.Key, "CorporateName")}";
                    int numberOfDrives = 0;
                    int numberOfRoms = 0;
                    int.TryParse(meep.GetProperty(prop.Key, "NumberOfDrives"), out numberOfDrives);
                    line = string.Concat(line, $", NumDirs: {numberOfDrives}, Dirs:");
                    for(int i = 1; i <= numberOfDrives; i++)
                    {
                        line = string.Concat(line, $" {meep.GetProperty(prop.Key, $"QS1Dir{i.ToString("D2")}")}");
                    }
                    
                    string cdDrive = string.Empty;
                    if(meep.PropertyExists("CDRomInformation", "QS1CDDrive01") == true)
                    {
                        cdDrive = meep.GetProperty("CDRomInformation", "QS1CDDrive01");
                        int.TryParse(meep.GetProperty("CDRomInformation", "NumberQS1CDROMs"), out numberOfRoms);
                    }
                    line = string.Concat(line, $" CD: {cdDrive}");
                    
                    if(numberOfDrives != 1)
                    {
                        _megaText = $"{_megaText}{line}\n";
                    }
                    
                }
            }
        }

        //very self-explanitory
        private bool IsWithinDateRange(string filename)
        {
            DateTime filetime = Directory.GetLastWriteTime(filename);
            if (_startDate == null && _endDate == null)
            {
                return true;
            }
            else if (_startDate == null && filetime.CompareTo(((DateTime)_endDate).AddSeconds(86399)) <= 0)
            {
                return true;
            }
            else if (_endDate == null && filetime.CompareTo(_startDate) >= 0)
            {
                return true;
            }
            else if (_startDate != null && _endDate != null && filetime.CompareTo(((DateTime)_endDate).AddSeconds(86399)) <= 0 && filetime.CompareTo(_startDate) >= 0)
            {
                return true;
            }
            return false;
        }

        private string DumpContentsToFile()
        {
            if (String.IsNullOrWhiteSpace(_megaText) == true) //no contents to dump
            {
                return "Nothing was read";
            }

            string outputPath = "Diagnosis.txt";
            int i = 1;
            while (File.Exists(outputPath) == true)
            {
                i = i + 1;
                outputPath = $"Diagnosis ({i}).txt";
            }
            StreamWriter outfile = new StreamWriter(outputPath);
            outfile.WriteLine(_megaText);
            outfile.Close();
            _megaText = string.Empty;
            return $"Saved to {outputPath}";
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            ResultView.Visibility = Visibility.Hidden;
            ParseSettings.Visibility = Visibility.Visible;
        }
    }
}
