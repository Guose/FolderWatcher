using System;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Threading;

namespace FolderWatcher
{
    public partial class MainWindow : Window
    {        
        private bool myBDirty;
        private bool myBIsWatching;
        private FileSystemWatcher myWatcher;
        private FileSystemWatcher myWatch2;
        private string watchPath = string.Empty;
        private string destPath = string.Empty;
        private string fileName = string.Empty;
        private string savedFile = string.Empty;
        private int count = 1;
        private List<string> textFile;
        private StringBuilder mySb;
        private Mutex mutex;

        public MainWindow()
        {
            InitializeComponent();
            myBDirty = false;
            myBIsWatching = false;
            textFile = new List<string>();
            mySb = new StringBuilder();
            mutex = new Mutex();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 400);
            timer.Start();
        }

        private void btnStartWatch_Click(object sender, RoutedEventArgs e)
        {
            if (myBIsWatching)
            {
                myBIsWatching = false;
                myWatcher.EnableRaisingEvents = false;
                myWatcher.Dispose();
                myWatch2.EnableRaisingEvents = false;
                myWatch2.Dispose();
                btnStartWatch.Background = Brushes.LightGray;
                btnStartWatch.Content = "Start Watching";                
            }
            else
            {
                myBIsWatching = true;
                btnStartWatch.Background = Brushes.Red;
                btnStartWatch.Content = "Stop Watching";

                if (txtDestPath.Text == "" || txtWatchPath.Text == "")
                {
                    System.Windows.MessageBox.Show("Please set paths to watch folder and destination folder", "PATHS CANNOT BE EMPTY", MessageBoxButton.OK, MessageBoxImage.Stop);
                    myBIsWatching = false;
                    btnStartWatch.Background = Brushes.LightGray;
                    btnStartWatch.Content = "Start Watching";
                }
                else
                {
                    myWatcher = new FileSystemWatcher(watchPath, "*.*");
                    myWatch2 = new FileSystemWatcher(destPath, "*.*");

                    if (radWatchFolder.IsChecked == true)
                    {
                        myWatcher.Filter = "*.*";
                        myWatcher.Path = watchPath + "\\";
                    }
                    else
                    {
                        myWatcher.Filter = txtWatchPath.Text.Substring(txtWatchPath.Text.LastIndexOf('\\') + 1);
                        myWatcher.Path = txtWatchPath.Text.Substring(0, txtWatchPath.Text.Length - myWatcher.Filter.Length);
                    }

                    if (chkSubFolders.IsChecked == true)
                    {
                        myWatcher.IncludeSubdirectories = true;
                    }

                    myWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.DirectoryName;

                    myWatcher.Created += new FileSystemEventHandler(OnCreated);
                    myWatcher.Deleted += new FileSystemEventHandler(OnDeleted);
                    myWatcher.EnableRaisingEvents = true;

                    myWatch2.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    myWatch2.Created += new FileSystemEventHandler(OnChanged);
                    myWatch2.EnableRaisingEvents = true;
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            
            string destFileName = string.Empty;
            string changeType = e.ChangeType.ToString();

            if (changeType == "Created")
            {
                mutex.WaitOne();
                destFileName = Path.GetFileName(e.FullPath.ToString());

                Dispatcher.BeginInvoke((Action)(() => lsbDestnFolder.Items.Add(
                destFileName + "  (" + changeType + ")")));

                Thread.Sleep(15);                

                mySb.Remove(0, mySb.Length);
                mySb.Append(e.FullPath);
                mySb.Append(" ");
                mySb.Append(changeType);
                mySb.Append("   ");
                mySb.Append(DateTime.Now.ToString());
                textFile.Add(mySb.ToString());

                mutex.ReleaseMutex();
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            mutex.WaitOne();
            string changeType = e.ChangeType.ToString();
            
            fileName = Path.GetFileName(e.FullPath.ToString());
            
            Dispatcher.BeginInvoke((Action)(() => lsbWatchFolder.Items.Add(
                count.ToString()+ ") " + fileName + "  (" + changeType + ") ")));
            Thread.Sleep(100);
            count++;            

            mySb.Remove(0, mySb.Length);
            mySb.Append(e.FullPath);
            mySb.Append(" ");
            mySb.Append(changeType);
            mySb.Append("   ");
            mySb.Append(DateTime.Now.ToString());
            textFile.Add(mySb.ToString());

            myBDirty = true;
            mutex.ReleaseMutex();
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            mutex.WaitOne();
            
            string changeType = e.ChangeType.ToString();

            fileName = Path.GetFileName(e.FullPath.ToString());

            Dispatcher.BeginInvoke((Action)(() => lsbWatchFolder.Items.Add(
                    fileName + "  (" + changeType + ")")));

            Thread.Sleep(15);            

            mySb.Remove(0, mySb.Length);
            mySb.Append(e.FullPath);
            mySb.Append(" ");
            mySb.Append(changeType);
            mySb.Append("   ");
            mySb.Append(DateTime.Now.ToString());
            textFile.Add(mySb.ToString());

            myBDirty = true;
            mutex.ReleaseMutex();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (myBDirty)
            {
                myBDirty = false;

                mutex.WaitOne();
                string sourceLoc = string.Empty;
                string[] files = Directory.GetFiles(watchPath, "*.*");
                try
                {                    
                    foreach (string file in files)
                    {
                        fileName = Path.GetFileName(file);

                        //Gets fileName and adds an enumerated value to end of fileName
                        savedFile = SetSavedFileName(fileName, destPath, true);
                        sourceLoc = watchPath + @"\" + fileName;
                        Thread.Sleep(40);
                        File.Move(sourceLoc, savedFile);
                    }
                }
                catch (Exception ex)
                {
                    //System.Windows.MessageBox.Show("There was an error: " + ex.Message + "\n\nPlease review " + savedFile, 
                    //    "PLEASE REVIEW LAST OUTPUT", MessageBoxButton.OK, MessageBoxImage.Hand);
                    textFile.Add("EXCEPTION:\t" + ex.Message + "\t" + DateTime.Now.ToString());
                }               

                lsbWatchFolder.SelectedIndex = lsbWatchFolder.Items.Count - 1;
                lsbDestnFolder.SelectedIndex = lsbDestnFolder.Items.Count - 1;
                mutex.ReleaseMutex();
            }
        }        

        private string SetSavedFileName(string fName, string destination, bool newFileCreated)
        {
            //Gets fileName and adds an enumerated number to end of file name
            string fDir = destination;
            string name = Path.GetFileNameWithoutExtension(fName);
            string fExt = Path.GetExtension(fName);
            int n = 2;

            if (newFileCreated)
            {
                do
                {
                    fName = Path.Combine(destination, String.Format("{0}-{1}{2}", name, (n++), fExt));
                }
                while (File.Exists(fName));
            }
            else
            {
                fName = Path.Combine(destination, String.Format("{0}-{1}{2}", name, (n++), fExt));
            }

            return fName;
        }

        private void btnToWatch_Click(object sender, RoutedEventArgs e)
        {
            if (radWatchFolder.IsChecked == true)
            {
                FolderBrowserDialog fld = new FolderBrowserDialog();
                DialogResult result = fld.ShowDialog();
                if (result.ToString() == "OK")
                {
                    txtWatchPath.Text = fld.SelectedPath;
                    watchPath = fld.SelectedPath;
                }
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                DialogResult dialogRes = ofd.ShowDialog();
                if (dialogRes.ToString() == "OK")
                {
                    txtWatchPath.Text = ofd.FileName;
                }
            }
        }

        private void btnDestination_Click(object sender, RoutedEventArgs e)
        {
            if (radWatchFolder.IsChecked == true)
            {
                FolderBrowserDialog fld = new FolderBrowserDialog();
                DialogResult result = fld.ShowDialog();
                if (result.ToString() == "OK")
                {
                    txtDestPath.Text = fld.SelectedPath;
                    destPath = fld.SelectedPath;
                }
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                DialogResult dialogRes = ofd.ShowDialog();
                if (dialogRes.ToString() == "OK")
                {
                    txtWatchPath.Text = ofd.FileName;
                }
            }
        }

        private void btnExportLog_Click(object sender, RoutedEventArgs e)
        {
            using (StreamWriter sw = new StreamWriter(destPath + "\\" + DateTime.Now.ToString("MM-dd-yy") + "_Log.txt"))
            {
                for (int i = 0; i < textFile.Count; i++)
                {
                    sw.WriteLine(textFile[i]);
                }
                sw.Close();
            }
            System.Windows.MessageBox.Show("Log has been saved to " + destPath, "LOG EXPORTED");

            myBDirty = true;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            lsbDestnFolder.Items.Clear();
            lsbWatchFolder.Items.Clear();
            textFile = new List<string>();
            count = 0;
            mySb = new StringBuilder();
        }

        private void lsbDestnFolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lsbDestnFolder.Items.Count > 0)
            {
                lsbDestnFolder.ScrollIntoView(lsbDestnFolder.Items[lsbDestnFolder.SelectedIndex = lsbDestnFolder.Items.Count - 1]);
            }            
        }

        private void lsbWatchFolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lsbWatchFolder.Items.Count > 0)
            {
                lsbWatchFolder.ScrollIntoView(lsbWatchFolder.Items[lsbWatchFolder.SelectedIndex = lsbWatchFolder.Items.Count - 1]);
            }            
        }
    }
}
