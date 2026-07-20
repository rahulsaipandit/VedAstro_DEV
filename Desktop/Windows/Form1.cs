using System.Diagnostics;

namespace Desktop_Windows
{
    public partial class Form1 : Form
    {
        private Process _process;

        public Form1()
        {
            InitializeComponent();
        }
       

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                // Kill API.exe process running via Func Core Tools
                //NOTE : else port will run background and occupy port
                // Wait for the process to exit
                _process?.WaitForExit(5000); // wait for 5 seconds

                // Kill the process when the form is closing
                _process?.Kill();
            }
            catch (Exception exception)
            {
                //silent fail, as not many stable possibilities
            }

        }

        private void relaunchButton_Click(object sender, EventArgs e)
        {

            StartApiServer();

        }

        private void StartApiServer()
        {
            //the API is a plain ASP.NET Core Kestrel app now, not an Azure Functions host, so it's
            //just run directly (was "Azure.Functions.Cli/func.exe start" against an api-build/ folder)
            string apiBuildPath = Path.Combine(Application.StartupPath, "api-build");
            string apiExecPath = Path.Combine(apiBuildPath, "API.exe");

            _process = new Process();
            _process.StartInfo.WorkingDirectory = apiBuildPath; // Set working directory to apiBuildPath
            _process.StartInfo.FileName = apiExecPath;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;

            _process.OutputDataReceived += (sender, data) => AppendToTextBox(data.Data);
            _process.ErrorDataReceived += (sender, data) => AppendToTextBox(data.Data);

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        private void AppendToTextBox(string text)
        {
            if (serverOutput.InvokeRequired)
            {
                serverOutput.Invoke((Action<string>)AppendToTextBox, text);
            }
            else
            {
                serverOutput.AppendText(text + Environment.NewLine);
            }
        }
    }
}
