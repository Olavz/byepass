using Byepass.Properties;
using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using tessnet2;

namespace Byepass
{
    public partial class frmByepass : Form
    {
        private bool isFastRun = false;
        private String privatePin;
        private String processPath;
        private String tessResFolder;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        private enum ESettings
        {
            processPath, pinCode
        }


        public frmByepass()
        {
            InitializeComponent();
        }

        private Process process;
        private bool isFormRdy = false;

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void StartProcessing()
        {
            Log("Running Byepass...");
            process = Process.Start(processPath);
            isFormRdy = (process.MainWindowHandle != IntPtr.Zero);

            // Start the timer to wait for process.
            tmrWaitProcess.Start();
        }

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private void ProcessForm()
        {
            isFormRdy = (process.MainWindowHandle != IntPtr.Zero);
            if (isFormRdy)
            {
                Log("Buypass loaded.");
                // Stop timer, only want to process form once.
                tmrWaitProcess.Stop();

                SendKeys.SendWait(privatePin);
                SendKeys.SendWait("{ENTER}");

                Log("Getting Buypass screen position");
                // Get the possition of the form.
                IntPtr ptr = process.MainWindowHandle;
                Rect pRect = new Rect();
                GetWindowRect(ptr, ref pRect);

                // Define the rectangle offset of the screencapture.
                int dxTop = 166;
                int dxLeft = 50;
                Rectangle rect = new Rectangle(pRect.Left + dxLeft, pRect.Top + dxTop, 200, 50);

                // Wait for form to generate code before we do a screencapture.
                Log("Taking screenshot of code.");
                CaptureScreen(rect, 5);
                Log("Exiting Buypass");
                process.Kill();
            }
        }


        private void CaptureScreen(Rectangle rect, int retry)
        {
            if(retry <= 0)
            {
                Log("Number of allowed retries exceeded the allowed number of retries of 5 times.");
                return;
            }
            //Rectangle bounds = Screen.GetBounds(Point.Empty);
            Rectangle bounds = rect;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    //g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    g.CopyFromScreen(new Point(rect.Left, rect.Top), Point.Empty, bounds.Size);
                }
                imageDisplay.Image = new Bitmap(bitmap);
                ProcessImageOCR(bitmap, rect, retry);
            }
        }

        private void InitFileStructure()
        {
            // Get datafile, unzip file to current working directory. Add reference to tes.init();
            string tessZipPath = System.Windows.Forms.Application.StartupPath + "\\tessdata.zip";
            string tessExtractPath = System.Windows.Forms.Application.StartupPath + "\\tessdata";
            tessResFolder = tessExtractPath;

            if(!Directory.Exists(tessExtractPath))
            {
                Log("Init tessaract resource files..");

                // Copy ZIP to disk...
                System.IO.File.WriteAllBytes(tessZipPath, Properties.Resources.tessdata);

                // Unzip tessdata.zip..
                System.IO.Compression.ZipFile.ExtractToDirectory(tessZipPath, tessExtractPath);

                // Remove the zip file.
                System.IO.File.Delete(tessZipPath);

                Log(tessExtractPath);
            }

        }

        private void ProcessImageOCR(Bitmap b, Rectangle rect, int retry)
        {
            Log("Start processing password from image. (OCR)");
            var ocr = new Tesseract();
            ocr.SetVariable("tessedit_char_whitelist", "0123456789");
 
            ocr.Init(@tessResFolder, "eng", true);
            var result = ocr.DoOCR(b, Rectangle.Empty);
            foreach (Word word in result)
            {
                Log("OCR returned: " + word.Text);
                if(word.Text.Length >= 5)
                {
                    Clipboard.SetText(word.Text);
                    Log("Password copied to clipboard.");
                } else
                {
                    Thread.Sleep(1000);
                    Log("OCR not accepted. Retrying... " + retry);
                    CaptureScreen(rect, retry - 1);
                }
                
            }

            if(isFastRun)
            {
                // Close the application as we saved results to clipboard.
                Application.Exit();
            }
        }

        private void tmrWaitProcess_Tick(object sender, EventArgs e)
        {
            /**
             *   Timer is used to poll window status in order to wait 
             *   for the form to load before processing the form.
             */
            ProcessForm();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitFileStructure();


            // Load settings if settings are set.
            processPath = Settings.Default[ESettings.processPath.ToString()].ToString();
            txtProcessHome.Text = processPath;
            privatePin = Settings.Default[ESettings.pinCode.ToString()].ToString();
            bool isValidProcessPath = true;
            bool isValidPin = true;

            if (!isValidSetting(ESettings.processPath))
            {
                // Process path is not valid, configure it!
                //MessageBox.Show("path is not set");
                isValidProcessPath = false;
            }

            if (!isValidSetting(ESettings.pinCode))
            {
                // Pin is not set, better do so now.
                //MessageBox.Show("pin is not set");
                isValidPin = false;
            }

            // Check for arguments
            String[] args = Environment.GetCommandLineArgs();
            foreach(var arg in args)
            {
                if(arg.Contains("-run"))
                {
                    isFastRun = true;
                }
            }

            if(isFastRun)
            {
                if(isValidProcessPath && isValidPin)
                {
                    StartProcessing();
                }
                else
                {
                    MessageBox.Show("Could not do fast run, check that pin and application path is set!");
                }
            } 

        }


        private bool isValidSetting(ESettings es)
        {
            if(ESettings.processPath.Equals(es))
            {
                if (processPath.ToString().Length <= 0)
                {
                    // No path is set.
                    return false;
                }
            } else if(ESettings.pinCode.Equals(es))
            {
                if (privatePin.ToString().Length <= 0)
                {
                    // Pin is not set.
                    return false;
                }
            }

            return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtProcessHome.Text = ofd.FileName.ToString();
                SaveProcessPath(ofd.FileName.ToString());
            }
        }

        private void btnSetPin_Click(object sender, EventArgs e)
        {
            PinPrompt pp = new PinPrompt();
            pp.ShowDialog();
            String pin = pp.getPin();
            
            if(pin.Length > 0)
            {
                SavePin(pin);
                btnSetPin.Text = "New pin was saved!";
                Log("New pin was saved!");
            } else
            {
                btnSetPin.Text = "No pin was saved.";
                Log("No pin specified. Pin could not be saved.");
            }
        }

        private void Log(String entry)
        {
            txtLog.AppendText(DateTime.Now + " " + entry + Environment.NewLine);
        }

        private void SavePin(String pin)
        {
            this.privatePin = pin;
            Settings.Default[ESettings.pinCode.ToString()] = pin;
            Settings.Default.Save();
        }

        private void SaveProcessPath(String processPath)
        {
            this.processPath = processPath;
            Settings.Default[ESettings.processPath.ToString()] = processPath;
            Settings.Default.Save();
            Log("Application path saved!");
            Log("Path: " + processPath);
        }

        private void createDesktopShortcut()
        {
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\Byepass.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "New shortcut for a Byepass";
            shortcut.Arguments = "-run";
            shortcut.TargetPath = System.Windows.Forms.Application.StartupPath + @"\Byepass.exe";
            shortcut.Save();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartProcessing();
        }

        private void createDesktopShortcutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            createDesktopShortcut();
        }
    }
}
