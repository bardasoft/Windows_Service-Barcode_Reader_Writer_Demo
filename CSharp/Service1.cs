using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Drawing;
using System.IO;
using Vintasoft.Barcode;

namespace BarcodeServiceDemo
{
    public class Service1 : System.ServiceProcess.ServiceBase
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private System.Diagnostics.EventLog _eventLog;
        private BackgroundWorker _backgroundWorker;

        public Service1()
        {
            // This call is required by the Windows.Forms Component Designer.
            InitializeComponent();

            // Create the EventLog object.
            _eventLog = new EventLog();
            _eventLog.MachineName = ".";
            // Associate the EventLog component with the new log.
            _eventLog.Source = "BarcodeServiceDemo";
            _eventLog.Log = "BarcodeServiceDemoLog";

            // Source cannot already exist before creating the log.
            if (System.Diagnostics.EventLog.SourceExists("BarcodeServiceDemo"))
                System.Diagnostics.EventLog.DeleteEventSource("BarcodeServiceDemo");

            // Logs and Sources are created as a pair.
            System.Diagnostics.EventLog.CreateEventSource("BarcodeServiceDemo", "BarcodeServiceDemoLog");

            // Create the BackgroundWorker object.
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker_RunWorkerCompleted);
        }

        // The main entry point for the process
        static void Main()
        {
            System.ServiceProcess.ServiceBase[] ServicesToRun;

            ServicesToRun = new System.ServiceProcess.ServiceBase[] { new BarcodeServiceDemo.Service1() };

            System.ServiceProcess.ServiceBase.Run(ServicesToRun);
        }

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // Service1
            // 
            this.ServiceName = "BarcodeServiceDemo";

        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            _eventLog.WriteEntry(string.Format("{0}: Start", ServiceName));

            _backgroundWorker.RunWorkerAsync(args);
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        protected override void OnStop()
        {
            _backgroundWorker.CancelAsync();
            while (_backgroundWorker.IsBusy)
                Thread.Sleep(1000);

            // TODO: Add code here to perform any tear-down necessary to stop your service.
            _eventLog.WriteEntry(string.Format("{0}: Stop", ServiceName));
        }

        /// <summary>
        /// Continues the service.
        /// </summary>
        protected override void OnContinue()
        {
            _eventLog.WriteEntry(string.Format("{0}: Continue", ServiceName));
        }

        void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_backgroundWorker.CancellationPending)
            {
                _eventLog.WriteEntry(string.Format("{0}: DoWork Start", ServiceName));

                // generate image with barcode
                DateTime timeNow = DateTime.Now;
                string imageWithBarcodePath = "";
                Bitmap imageWithBarcode = null;
                try
                {
                    BarcodeWriter barcodeWriter = new BarcodeWriter();
                    barcodeWriter.Settings.Barcode = BarcodeType.Code39;
                    barcodeWriter.Settings.Value = timeNow.ToString();
                    barcodeWriter.Settings.Padding = 20;
                    imageWithBarcode = (Bitmap)barcodeWriter.GetBarcodeAsBitmap();

                    imageWithBarcodePath = string.Format(@"d:\barcodes\{0}.png", timeNow.Millisecond);
                    imageWithBarcode.Save(imageWithBarcodePath);
                }
                catch (Exception ex)
                {
                    _eventLog.WriteEntry(string.Format("{0}: Barcode writing error: {1}", ServiceName, ex.Message));
                }
                finally
                {
                }

                // read barcode from the image
                try
                {
                    BarcodeReader barcodeReader = new BarcodeReader();
                    barcodeReader.Settings.ScanBarcodeTypes = BarcodeType.Code39;
                    barcodeReader.Settings.ScanDirection = ScanDirection.LeftToRight;

                    IBarcodeInfo[] barcodeInfo = barcodeReader.ReadBarcodes(imageWithBarcode);
                    string result = "No barcodes found.";
                    if (barcodeInfo.Length != 0)
                        result = string.Format("Barcode found: {0}", barcodeInfo[0]);

                    _eventLog.WriteEntry(string.Format("{0}: {1}", ServiceName, result));
                    File.WriteAllText(string.Format(@"d:\barcodes\{0}.txt", timeNow.Millisecond), result);

                }
                catch (Exception ex)
                {
                    _eventLog.WriteEntry(string.Format("{0}: Barcode reading error: {1}", ServiceName, ex.Message));
                }
                finally
                {
                    Thread.Sleep(1000);
                }

                _eventLog.WriteEntry(string.Format("{0}: DoWork Finish", ServiceName));

            }
        }

        void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _eventLog.WriteEntry(string.Format("{0}: RunWorkerCompleted", ServiceName));
        }

    }
}
