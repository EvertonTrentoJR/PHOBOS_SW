using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Globalization;
using System.Threading;
using System.Collections.ObjectModel;
using DevExpress.XtraGrid.Views.Grid;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using DevExpress.CodeParser;

namespace DXApplication2
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {

        // DATALOG
        public string data = "";

        // SERIALPORT
        SerialPort serialPort = new SerialPort();

        // VISA
        Ivi.Visa.IMessageBasedSession session;

        // lIST
        private System.Data.DataTable dataTable;
        System.Data.DataTable table = new System.Data.DataTable();
        private System.Data.DataColumn column;
        private System.Data.DataRow row;
        private System.Data.DataView view;

        // INIT
        private int rowCount;
        private AppConfig cfg;

        // nyquist chart
        private DXApplication21.NyquistChart nyquistChartWindow;

        public Form1()
        {
            LoadConfig();
            InitializeComponent();

            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;


            // LCR init
            //InitializeSavedValues();

            // GRIDVIEW
            rowCount = (gridControl1.FocusedView as GridView).RowCount;

            //radioGroup_selecttest.SelectedIndex = 1;

        }
        private void LoadConfig()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("PHOBOScfg.json", optional: false, reloadOnChange: true)
                .Build();

            var root = new RootConfig();
            config.Bind(root);

            cfg = root.AppConfig;  // <--- store in your global variable
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }
        private void MakeDataTableAndDisplay()
        {
            // Create new DataColumn, set DataType, ColumnName and add to DataTable.
            column = new System.Data.DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "ID";
            table.Columns.Add(column);

            // Create second column.
            column = new System.Data.DataColumn();
            column.DataType = Type.GetType("System.String");
            column.ColumnName = "Item";
            table.Columns.Add(column);

            // Fill rows from JSON (cfg.TableData)
            if (cfg != null && cfg.TableData != null)
            {
                foreach (var rowCfg in cfg.TableData)
                {
                    var row = table.NewRow();
                    row["ID"] = rowCfg.ID;
                    row["Item"] = rowCfg.Item;
                    table.Rows.Add(row);
                }
            }

            // Create a DataView using the DataTable.
            view = new System.Data.DataView(table);
            // Set a DataGrid control's DataSource to the DataView.
            gridControl1.DataSource = view;
            gridControl1.RefreshDataSource();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            CultureInfo culture = new CultureInfo("en-US");
            if (culture == null)
            {
                culture = CultureInfo.InvariantCulture;
            }
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            //set the style
            DevExpress.LookAndFeel.UserLookAndFeel.Default.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Skin;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle("DevExpress Dark Style");

            // set the com port
            var ports = SerialPort.GetPortNames();
            cmbSerialPorts.Properties.DataSource = ports;
            cmbSerialPorts.EditValue = cfg.Port;
            tbBaudrate.EditValue = "115200";


            // apply .json config
            // Setup mode
            radioGroup_selecttest.SelectedIndex = (cfg.Setup == "LCR") ? 2 : 0;

            // Loop
            loopSweep.Checked = (cfg.Loop == "Enabled");
            loopvalue.Value = cfg.LoopCount;

            // Timer
            Cetimer.Checked = (cfg.Timer == "Enabled");
            spinTimertable.Value = cfg.TimerCount;

            //LCR config
            tbDevice.EditValue = cfg.LCRid;
            // FreqList
            nudStart.Value = cfg.freqStart;
            nudEnd.Value = cfg.freqEnd;
            nudN.Value = cfg.freqNpoints;

            radioGroup3.SelectedIndex = (cfg.freqMethod == "LOG") ? 0 : 2;

            updateAutoFreqList();

            // Sampling Mode
            switch (cfg.measMode)
            {
                case "Short":
                    radioGroup4.SelectedIndex = 0;
                    break;

                case "Medium":
                    radioGroup4.SelectedIndex = 1;
                    break;

                case "Long":
                    radioGroup4.SelectedIndex = 2;
                    break;
            }

            // Samples
            nudSamples.Value = cfg.samples;

            // Path
            tbsavepathLCR.EditValue = cfg.path;

            // Filename
            tbFilenameLCR.EditValue = cfg.filename;

            // Sweep Data
            MakeDataTableAndDisplay();

        }


        // CONECT BUTTON
        private void refreshbutton_Click(object sender, EventArgs e)
        {
            var ports = SerialPort.GetPortNames();
            cmbSerialPorts.Properties.DataSource = ports;
            cmbSerialPorts.EditValue = "";
        }
        private void connect_Click(object sender, EventArgs e)
        {
            int baudRate;

            if (!int.TryParse(tbBaudrate.Text, out baudRate))
            {
                // Parsing failed, handle the error (e.g., display a message to the user)
                tbStatus.Text = "Invalid baud rate";
                return;
            }


            if (!serialPort.IsOpen)
            {
                while (!serialPort.IsOpen)
                {
                    {
                        serialPort = new SerialPort(cmbSerialPorts.EditValue.ToString());
                        try
                        {
                            serialPort.BaudRate = baudRate;
                            serialPort.DtrEnable = true;
                            serialPort.RtsEnable = true;
                            serialPort.Open();
                            StartStreaming();

                        }
                        catch (Exception ex)
                        {
                            tbStatus.Text = ("Connection attempt failed: " + ex.Message);
                            return;
                        }

                        //tbStatus.Text = "Connected to " + cmbSerialPorts.EditValue.ToString();
                        connect.Text = "Disconnect";

                    }
                }
            }

            else
            {
                try
                {
                    StopStreaming();
                    serialPort.Close();
                    tbStatus.Text = "Serial Port Disconnected";
                    connect.Text = "Connect";

                }
                catch (Exception ex)
                {
                    tbStatus.Text = ("Connection attempt failed: " + ex.Message);
                    return;
                }
            }
        }



        int datacount = 0;

        // READ SERIAL DATA
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (!worker.CancellationPending)
            {
                try
                {
                    int dataLength = serialPort.BytesToRead;
                    if (dataLength > 0)
                    {
                        if (radioGroup_selecttest.SelectedIndex == 1)
                        {
                            string Data = serialPort.ReadLine();
                            readDateTime.Add(DateTime.Now);

                            string newData = $"{DateTime.Now:HH:mm:ss} - {Data}"; // Add timestamp

                            // Report progress and pass the received data to the UI thread
                            worker.ReportProgress(0, newData);

                            datacount++;
                            if (rowCount == datacount)
                            {
                                datacount = 0;

                                writeDateTime.Clear();
                                readDateTime.Clear();

                            }

                            if (loopSweep.Checked && loopcount == (loopvalue.Value * rowCount))
                            {
                                MessageBox.Show("LoopMode Done successfully", "SerialWrite Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                loopcount = 0;
                            }
                            else { writeAction(); }
                        }
                        else
                        {

                            string Data = serialPort.ReadLine();
                            readDateTime.Add(DateTime.Now);
                            string newData = $"{DateTime.Now:HH:mm:ss} - {Data}"; // Add timestamp
                            // Report progress and pass the received data to the UI thread
                            worker.ReportProgress(0, newData);

                            BeginInvoke(new Action(() =>
                            {
                                btnStart_Click(this, new EventArgs());
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error reading serial data: " + ex.Message);
                    break; // Exit the loop on error
                }
            }
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string newData = e.UserState as string;
            update_tbReadSerial(newData);

        }
        private void update_tbReadSerial(string newData)
        {
            if (tbReadSerial.InvokeRequired)
            {
                tbReadSerial.Invoke(new Action<string>(update_tbReadSerial), newData);
            }
            else
            {
                if (tbReadSerial.Lines.Length > 100000)
                    tbReadSerial.ResetText();
                tbReadSerial.AppendText(newData + Environment.NewLine);
            }
        }
        private void StartStreaming()
        {
            tbStatus.Text = "Start Streaming...";
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }
        private void StopStreaming()
        {
            if (backgroundWorker1.IsBusy)
            {
                // Cancel the background worker to stop data streaming
                backgroundWorker1.CancelAsync();

                // Close the serial port if it's open
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }
            else
            {
                tbStatus.Text = "Streaming not active.";
            }
        }
        private void UpdateStatus(string message)
        {
            if (tbStatus.InvokeRequired)
            {
                tbStatus.Invoke(new Action(() => { tbStatus.Text = message; }));
            }
            else
            {
                tbStatus.Text = message;
            }
        }
        private void erasebutton_Click(object sender, EventArgs e)
        {
            tbReadSerial.ResetText();
        }

        //WRITE ON SERIAL PORT
        private void Send_serial_Click(object sender, EventArgs e)
        {
            if (tbSerialWrite.EditValue != null && serialPort.IsOpen == true)
            {

                serialPort.Write(tbSerialWrite.Text);
                modesend = tbSerialWrite.Text;
                writeDateTime.Add(DateTime.Now);
                //MessageBox.Show(" Mode updated successfully", "SerialWrite Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbSerialWrite.Text = null;
            }
            else
            {
                //MessageBox.Show(" Type valid Mode", "SerialWrite Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }
        }
        private void tbSerialWrite_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Send_serial_Click(this, new EventArgs());
            }
        }

        //SWEEP LIST - BUTTONS AND VARIABLE GRID
        private void bterase_Click(object sender, EventArgs e)
        {
            int selectedRowHandle = gridView1.FocusedRowHandle;
            if (selectedRowHandle >= 0)
            {
                gridView1.DeleteRow(selectedRowHandle);
            }
        }
        private void btadd_Click(object sender, EventArgs e)
        {
            // Create a new row in the DataTable
            DataRow row = table.NewRow();

            // Check if the "ID" column exists in the DataTable
            if (table.Columns.Contains("ID"))
            {
                // Find the maximum ID value in the DataTable
                int maxID = 0;
                foreach (DataRow existingRow in table.Rows)
                {
                    int id = Convert.ToInt32(existingRow["ID"]);
                    if (id > maxID)
                    {
                        maxID = id;
                    }
                }

                // Assign the new ID to the new row (incrementing the maximum ID by 1)
                row["ID"] = maxID + 1;
            }
            else
            {
                // If the "ID" column doesn't exist, assign an initial ID of 1
                row["ID"] = 1;
            }
            string itemtable = tbSweeplist.Text;
            if (radioGroup_selecttest.SelectedIndex == 2)
            {
                if (itemtable.StartsWith("d:"))
                {
                    row["Item"] = tbSweeplist.Text;
                    table.Rows.Add(row);
                }
                else
                {
                    MessageBox.Show("For LCR measurements must operates in differential mode (d:emitter-receiver).", "Sweep Mode Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                row["Item"] = tbSweeplist.Text;
                table.Rows.Add(row);
            }



            gridControl1.DataSource = view;
            gridControl1.RefreshDataSource();
            tbSweeplist.Text = string.Empty;
        }
        private void tbSweeplist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btadd_Click(this, new EventArgs());
            }
        }
        Boolean headertblwrited = false;
        private void btSavelist_Click(object sender, EventArgs e)
        {
            if (table.Rows.Count != 0)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string[] headerstbl = new string[2];
                    headerstbl[0] = "ID";
                    headerstbl[1] = "Item";
                    using (FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Append, FileAccess.Write))
                    {

                        using (StreamWriter out_file = new StreamWriter(fs))
                        {
                            if (!headertblwrited)// Write the headers
                            {
                                out_file.WriteLine(string.Join(" ", headerstbl));
                                headertblwrited = true;
                            }

                            foreach (DataRow row in table.Rows)
                            {
                                foreach (object item in row.ItemArray)
                                {
                                    out_file.Write(item.ToString() + ' ');
                                }
                                out_file.WriteLine();
                            }
                        }

                    }

                    UpdateStatus("Data written to " + saveFileDialog1.FileName);
                }
                else
                {
                    UpdateStatus("Save into a valid repository");
                }
            }
            else
            {
                MessageBox.Show(" Table Mode Empty", "Sweep Mode Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void LoadList_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //String flpth = openFileDialog1.FileName;
                    table.Clear();

                    using (FileStream openfs = new FileStream(openFileDialog1.FileName, FileMode.Open))
                    {

                        // Read data from the selected .txt file and load it into the DataTable
                        using (StreamReader reader = new StreamReader(openfs))
                        {
                            string line;
                            // Read the header line to skip it (assuming it contains column names)
                            reader.ReadLine();
                            while ((line = reader.ReadLine()) != null)
                            {

                                string[] fields = line.Split(' ');
                                if (fields.Length >= 2) // Ensure there are at least 2 fields (ID and Item)
                                {
                                    DataRow newRow = table.NewRow();
                                    newRow["ID"] = fields[0].Trim(); // Assuming ID is the first column
                                    newRow["Item"] = fields[1].Trim(); // Assuming Item is the second column
                                    //newRow["Time"] = fields[2].Trim(); // Assuming Item is the second column
                                    table.Rows.Add(newRow);
                                }
                            }
                        }
                    }

                    // Refresh the GridView to reflect the updated data
                    gridView1.RefreshData();
                    rowCount = (gridControl1.FocusedView as GridView).RowCount;
                    MessageBox.Show("File opened and data loaded successfully!", "Open File", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Error opening file: " + ex.Message, "Open File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        int writecounter = 0;
        int loopcount = 0;
        int tenLoopCount = 0;
        string modesend;
        Stopwatch sweeptimer = new Stopwatch();
        double sweepelapsedtime;
        private void writeAction()
        {
            sweeptimer.Reset();
            sweeptimer.Start();

            int foreachcounter = 0;
            rowCount = (gridControl1.FocusedView as GridView).RowCount;
            foreach (DataRow row in table.Rows)
            {
                if (foreachcounter == writecounter)
                {
                    string serial_item = row[1].ToString();

                    BeginInvoke(new Action(() =>
                    {
                        tbSerialWrite.Text = serial_item;
                        Send_serial_Click(this, new EventArgs());
                    }));

                    break;
                }
                foreachcounter++;
            }
            tbStatus.Text = $"LoopCount status: {tenLoopCount}";
            writecounter++;
            writecounter = (writecounter == rowCount) ? 0 : writecounter;

            if (loopSweep.Checked && loopcount == (loopvalue.Value * rowCount) && radioGroup_selecttest.SelectedIndex == 2)
            {
                loopcount = 0;
                //MessageBox.Show("LoopMode Done successfully", "SerialWrite Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                BeginInvoke(new Action(() =>
                {
                    btapplylist_Click(this, new EventArgs());
                }));

            }
            else
            {
                loopcount++;
                if (loopcount % 10 == 0)
                {
                    tenLoopCount++;
                    tbStatus.Text = $"LoopCount status: {tenLoopCount}";
                }

            }

        }


        Boolean applybutton = false;
        Boolean earlyStopFlag = false;
        Boolean headerwrited = false;
        List<DateTime> writeDateTime = new List<DateTime>();
        List<DateTime> readDateTime = new List<DateTime>();
        string header;
        string[] frq;
        void session_visa()
        {
            try
            {

                session = (Ivi.Visa.IMessageBasedSession)Ivi.Visa.GlobalResourceManager.Open(tbDevice.Text);
                session.Clear();
                session = (Ivi.Visa.IMessageBasedSession)Ivi.Visa.GlobalResourceManager.Open(tbDevice.Text);
                session.TimeoutMilliseconds = 600000;
                session.FormattedIO.WriteLine("*IDN?");
                string idName = session.FormattedIO.ReadLine();


                /// E4980A VISA Commands
                /// https://www.cmc.ca/wp-content/uploads/2019/07/E4980A-User-Guide.pdf

                session.FormattedIO.WriteLine("RST;*CLS");
                session.FormattedIO.WriteLine("TRIG:SOUR BUS");
                session.FormattedIO.WriteLine("DISP:PAGE LIST");

                /// Sampling time
                if (radioGroup4.SelectedIndex == 2)
                    session.FormattedIO.WriteLine("APER LONG,1");
                else if (radioGroup4.SelectedIndex == 1)
                    session.FormattedIO.WriteLine("APER MED,1");
                else
                    session.FormattedIO.WriteLine("APER SHORT,1");


                session.FormattedIO.WriteLine("LIST:CLE:ALL");

                /// Data format
                session.FormattedIO.WriteLine("FUNC:IMP CPRP");
                session.FormattedIO.WriteLine("FORM ASC");


                /// Frequency sweep list
                session.FormattedIO.WriteLine("LIST:MODE SEQ");

                string frequencyList = tbFlist.Text;

                frq = frequencyList.Split(',');
                header = "timestamp, mode, ";
                //if (cbCalib.Checked)
                //    header = header + "R, C, ";

                for (int i = 0; i < frq.Count(); i++)
                {
                    header += frq[i].Trim() + " Cp, ";
                    header += frq[i].Trim() + " Rp, ";
                }
                header = header.TrimEnd();
                header = header.TrimEnd(',');

                Thread.Sleep(200);

                session.FormattedIO.WriteLine("LIST:FREQ " + frequencyList);
                session.FormattedIO.WriteLine("INIT:CONT ON");
                //session.FormattedIO.WriteLine("TRIG:IMM");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "LCR meter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btapplylist_Click(object sender, EventArgs e)
        {
            if (applybutton != true)
            {
                writecounter = 0;
                datacount = 0;
                loopcount = 0;
                tenLoopCount = 0;

                if (table.Rows.Count != 0 && serialPort.IsOpen)
                {
                    btapplylist.Text = "Abort";
                    headerwrited = false;
                    applybutton = true;
                    earlyStopFlag = false;

                    if (Cetimer.Checked)
                    {
                        timedAcquisition = new Thread(() => acqThread());
                        timedAcquisition.Start();
                    }

                    writeAction();
                }
                else
                {
                    MessageBox.Show(" Table Mode Empty or Serial port is Closed.", "Sweep Mode Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                btapplylist.Text = "Apply";
                applybutton = false;
                earlyStopFlag = true;
                connect_Click(this, new EventArgs());
                datacount = 0;
                writecounter = 0;
            }
        }

        private class DataPoint
        {
            public DateTime Argument { get; set; }
            public double Value { get; set; }
            public DataPoint(DateTime argument, double value)
            {
                Argument = argument;
                Value = value;
            }
        }

        // LCR METER
        private void nudStart_ValueChanged(object sender, EventArgs e)
        {
            updateAutoFreqList();
        }
        private void radioGroup3_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateAutoFreqList();
        }

        bool lcrConnected = false;
        private void btnStart_Click(object sender, EventArgs e)
        {

            if (Directory.Exists(tbsavepathLCR.Text))
            {
                if (tbsavepathLCR.Text == "")
                {
                    MessageBox.Show("Invalid Filename!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    btnStart.Enabled = false;

                    if (!lcrConnected)
                    { session_visa();
                        lcrConnected = true;
                    }

                    richTextBox1.Text = "Starting measurement...\r\n";

                    Thread t_lcr = new Thread(LCRThread);
                    t_lcr.Start();
                }
            }
            else
            {
                MessageBox.Show("Invalid Path!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public IEnumerable<double> logspace(double start, double end, int count)
        {
            double d = (double)(count - 1), p = Math.Pow(end / start, 1 / d);
            var list = Enumerable.Range(0, count).Select(i => start * Math.Pow(p, i));
            return list.Select(n => Math.Round(n, 3, MidpointRounding.AwayFromZero));
        }
        public static IEnumerable<double> Arange(double start, int count)
        {
            var list = Enumerable.Range((int)start, count).Select(v => (double)v);
            return list.Select(n => Math.Round(n, 3, MidpointRounding.AwayFromZero));
        }
        public static IEnumerable<double> Power(IEnumerable<double> exponents, double baseValue = 10.0d)
        {
            return exponents.Select(v => Math.Pow(baseValue, v));
        }
        public static IEnumerable<double> linspace(double start, double stop, int num, bool endpoint = true)
        {
            var result = new List<double>();
            if (num <= 0)
            {
                return result;
            }

            if (endpoint)
            {
                if (num == 1)
                {
                    return new List<double>() { start };
                }

                var step = (stop - start) / ((double)num - 1.0d);
                result = Arange(0, num).Select(v => (v * step) + start).ToList();
            }
            else
            {
                var step = (stop - start) / (double)num;
                result = Arange(0, num).Select(v => (v * step) + start).ToList();
            }

            return result;
        }
        void updateAutoFreqList()
        {
            if (radioGroup3.SelectedIndex == 0)
                tbFlist.Text = string.Join(", ", logspace(Convert.ToDouble(nudStart.Value), Convert.ToDouble(nudEnd.Value), Convert.ToInt16(nudN.Value)));
            else if (radioGroup3.SelectedIndex == 1)
                tbFlist.Text = string.Join(", ", linspace(Convert.ToDouble(nudStart.Value), Convert.ToDouble(nudEnd.Value), Convert.ToInt16(nudN.Value)));
        }

        void LCRThread()
        {
            try
            {
                var timer = new Stopwatch();
                TimeSpan timeTaken;
                bool firstLine = true;

                char[] ans;
                string stringpath = tbsavepathLCR.Text;
                //bool preexistingFile = File.Exists(tbsavepathLCR.Text + "\\" + tbFilenameLCR.Text + ".csv");
                bool preexistingFile = File.Exists(stringpath + "\\" + tbFilenameLCR.Text + ".csv");

                /// Write to local file
                //using (StreamWriter writer = new StreamWriter(tbsavepathLCR.Text + "\\" + tbFilenameLCR.Text + ".csv", true))
                using (StreamWriter writer = new StreamWriter(stringpath + "\\" + tbFilenameLCR.Text + ".csv", true))
                {
                    writer.AutoFlush = true;

                    /// Continuos measurement/TRIG
                    for (int i = 0; i < nudSamples.Value; i++)
                    {
                        timer.Restart();
                        Thread.Sleep(200);
                        DateTime foo = DateTime.Now;
                        long unixTime = ((DateTimeOffset)foo).ToUnixTimeMilliseconds();
                        session.FormattedIO.WriteLine("*TRG");
                        ans = session.FormattedIO.ReadLine().ToCharArray();

                        timer.Stop();

                        timeTaken = timer.Elapsed;
                        string time = "Time taken: " + timeTaken.ToString(@"m\:ss\.ffffff");
                        string s = new string(ans);
                        s = s.Replace(",+0,+0", "");
                        s = s.Replace(",+1,+0", "");

                        if (firstLine && (!preexistingFile))
                        {
                            string fs = "timestamp, mode, ";

                            foreach (var f in frq)
                            {
                                fs += f.ToString() + ", ";
                                fs += f.ToString() + ", ";
                            }

                            fs = fs.Trim().TrimEnd(',');
                            writer.WriteLine(header);

                            firstLine = false;
                        }

                        string sline = modesend + ", ";
                        List<string> splitResult = s.Split(',').ToList();
                        List<double> result = splitResult.Select(x => double.Parse(x)).ToList();
                        for (int j = 0; j < frq.Length; j++)
                        {
                            double mag = result[j * 2];
                            double ph = result[j * 2 + 1];
                            sline += mag.ToString() + ", " + ph.ToString() + ", ";
                        }

                        sline = sline.Trim().TrimEnd(',');
                        writer.WriteLine(unixTime.ToString() + ", " + sline);
                        writer.Flush();
       
                        /// Print in textbox
                        BeginInvoke(new Action(() =>
                        {
                            richTextBox1.Text += DateTime.Now.ToString() + " - Measurement number " + i.ToString() + "\r\n" + time + "\r\n" + unixTime.ToString() + ", " + s + "\r\n";


                        }));
                        Console.WriteLine(time);
                    }

                }

                BeginInvoke(new Action(() =>
               {
                   sweeptimer.Stop();
                   btnStart.Enabled = true;
                   richTextBox1.Text += "Measurement finished!\r\n";
                   sweepelapsedtime = sweeptimer.Elapsed.TotalMilliseconds;
                   tbReadSerial.Text += $"Time elapsed (ms): {sweeptimer.Elapsed.TotalMilliseconds} \r\n";
                   if (loopSweep.Checked && loopcount == (loopvalue.Value * rowCount))
                   {
                       chartTestEdit.Text = tbsavepathLCR.Text + "\\" + tbFilenameLCR.Text + ".csv";
                       simpleButton_chart_Click(this, new EventArgs());
                      
                       MessageBox.Show("LoopMode Done successfully", "SerialWrite Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                       loopcount = 0;
                   }
                   else { writeAction(); }
               }));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BeginInvoke(new Action(() =>
                {
                    btnStart.Enabled = true;
                    richTextBox1.Text += "Error!\r\n";
                }));
            }
        }

        private void btLCRpathsave_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog()) // ou VistaFolderBrowserDialog se usar Ookii
            {
                dialog.Description = "Please select a folder.";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolder = dialog.SelectedPath;
                    tbsavepathLCR.Text = Path.Combine(selectedFolder, "");
                }
            }
        }

        //timer 
        Thread timedAcquisition;

        void acqThread()
        {
            Stopwatch sw = Stopwatch.StartNew();

            while ((sw.ElapsedMilliseconds < Convert.ToUInt32(spinTimertable.Value) * 1000 + 100) && (earlyStopFlag == false))
            {
                string format = @"hh\:mm\:ss";
                TimeSpan ts = TimeSpan.FromMilliseconds(Convert.ToUInt32(spinTimertable.Value) * 1000.0 - sw.ElapsedMilliseconds + 100);
                BeginInvoke(new Action(() =>
                {
                    tbStatus.Text = String.Format("{0}", ts.ToString(format));

                }));
                Thread.Sleep(100);
            }

            BeginInvoke(new Action(() =>
            {
                btapplylist_Click(this, new EventArgs());
            }));
        }

        private void loopSweep_CheckedChanged(object sender, EventArgs e)
        {
            // Temporarily unsubscribe from the other checkbox's event
            Cetimer.CheckedChanged -= Cetimer_CheckedChanged;

            if (loopSweep.Checked)
            {
                Cetimer.Checked = false;
            }

            // Re-subscribe to the other checkbox's event
            Cetimer.CheckedChanged += Cetimer_CheckedChanged;
        }

        private void Cetimer_CheckedChanged(object sender, EventArgs e)
        {
            // Temporarily unsubscribe from the other checkbox's event
            loopSweep.CheckedChanged -= loopSweep_CheckedChanged;

            if (Cetimer.Checked)
            {
                loopSweep.Checked = false;
            }

            // Re-subscribe to the other checkbox's event
            loopSweep.CheckedChanged += loopSweep_CheckedChanged;
        }

        private void read_write_Paint(object sender, PaintEventArgs e)
        {

        }

        private void loopvalue_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void Setup_Paint(object sender, PaintEventArgs e)
        {

        }

        private void radioGroup_selecttest_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (radioGroup_selecttest.SelectedIndex == 0)
            {
                groupControl5.Enabled = true;
                groupControl4.Enabled = false;
                read_write.Enabled = false;
                btnStart.Enabled = true;
                ChartgroupControl.Enabled = false;

            }

            if (radioGroup_selecttest.SelectedIndex == 1)
            {
                groupControl4.Enabled = false;
                groupControl5.Enabled = false;
                read_write.Enabled = true;
                ChartgroupControl.Enabled = true;
                checkEditenabelchartfile.Checked = true;

            }

            if (radioGroup_selecttest.SelectedIndex == 2)
            {
                groupControl5.Enabled = true;
                groupControl4.Enabled = true;
                read_write.Enabled = true;
                btnStart.Enabled = false;
                ChartgroupControl.Enabled = false;

            }
        }


        // CpRp / Nyquist Chart

        List<double> frequencies;
        List<double> cpValues;
        List<double> rpValues;
        List<double> zLine;
        List<double> z2Line;

        // for Multielectrode chart
        List<string> modes = new List<string>();
        List<List<double>> cpMulti = new List<List<double>>();
        List<List<double>> rpMulti = new List<List<double>>();
        List<List<double>> zRealMulti;
        List<List<double>> zImagMulti;

        private void simpleButton_chart_Click(object sender, EventArgs e)
        {
            if (nyquistChartWindow == null || nyquistChartWindow.IsDisposed)
            {
                nyquistChartWindow = new DXApplication21.NyquistChart();
            }

            if (!ChartPathcheck())
            {
                return;
            }

            if (!DataChartUpload(out frequencies, out cpValues, out rpValues, out isMultiElectrode, out modes, out cpMulti, out rpMulti))
            {
                return;
            }

            if (isMultiElectrode)
            {
                if (!CpRpMultiToNyquist(frequencies, cpMulti, rpMulti, out zRealMulti, out zImagMulti))
                {
                    return;
                }

                nyquistChartWindow.ChartConfigNyquistMulti(modes, zRealMulti, zImagMulti);

                MessageBox.Show("For multielectrode data only Nyquist plot was implemented!", "Multielectrode Plot", MessageBoxButtons.OK, MessageBoxIcon.Information);


            }
            else
            {
                if (checkEditenabelchartfile.Checked)
                {
                    switch (plotRadioGroup.SelectedIndex)
                    {
                        case 0:

                            if (!CpRpToNyquist( frequencies, cpValues, rpValues, out zLine, out z2Line))
                            {
                                return;
                            }

                            nyquistChartWindow.ChartConfigNyquist(zLine, z2Line);
                            break;

                        case 1:
                            nyquistChartWindow.ChartConfigCpRp(frequencies, cpValues, rpValues);
                            break;
                    }
                }
                else
                {
                    //chartTestEdit.Text = tbsavepathLCR.Text + "\\" + tbFilenameLCR.Text + ".csv";

                    //switch (plotRadioGroup.SelectedIndex)
                    //{
                    //    case 0:

                    //        if (!CpRpToNyquist(frequencies, cpValues, rpValues, out zLine, out z2Line))
                    //        {
                    //            return;
                    //        }

                    //        nyquistChartWindow.ChartConfigNyquist(zLine, z2Line);
                    //        break;

                    //    case 1:

                    //        nyquistChartWindow.ChartConfigCpRp(frequencies, cpValues, rpValues);
                    //        break;
                    //        }
                return;
            }
            }

            if (!nyquistChartWindow.Visible)
            {
                nyquistChartWindow.Show(this);
            }

            nyquistChartWindow.BringToFront();
            nyquistChartWindow.Focus();
        }

        private void pathchartbutton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Please select the data file.";

                dialog.Filter = "CSV files (*.csv)|*.csv";

                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    chartTestEdit.Text = dialog.FileName;
                }
            }
        }

        private bool ChartPathcheck()
        {
            string filePath = chartTestEdit.Text.Trim();

            // Validate the selected file.
            if (string.IsNullOrWhiteSpace(filePath))
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "Please select a CSV file.",
                    "No File Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            if (!File.Exists(filePath))
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "The selected file could not be found.",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            if (!string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "The selected file is not a CSV file.",
                    "Invalid File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private void checkEditenabelchartfile_CheckedChanged(object sender, EventArgs e)
        {
            pathchartbutton.Enabled = checkEditenabelchartfile.Checked;
        }

        bool isMultiElectrode;
        private bool DataChartUpload(
            out List<double> frequencies,
            out List<double> cpValues,
            out List<double> rpValues,
            out bool isMultiElectrode,
            out List <string> modes,
            out List<List<double>> cpMulti,
            out List<List<double>> rpMulti)
        {
            isMultiElectrode = false;
            frequencies = new List<double>();
            cpValues = new List<double>();
            rpValues = new List<double>();
            modes = new List<string>();
            cpMulti = new List<List<double>>();
            rpMulti = new List<List<double>>();

            string filePath = chartTestEdit.Text.Trim();

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                string[] headers = lines[0]
                    .Split(',')
                    .Select(value => value.Trim())
                    .ToArray();

                // Store all complete measurement rows.
                List<string[]> dataRows = new List<string[]>();

                for (int rowIndex = 1;
                     rowIndex < lines.Length;
                     rowIndex++)
                {
                    if (string.IsNullOrWhiteSpace(lines[rowIndex]))
                    {
                        continue;
                    }

                    string[] currentRow = lines[rowIndex]
                        .Split(',')
                        .Select(value => value.Trim())
                        .ToArray();

                    if (currentRow.Length == headers.Length)
                    {
                        dataRows.Add(currentRow);
                    }
                }

                // Columns:
                // 0 = timestamp
                // 1 = mode
                // 2 = Cp at first frequency
                // 3 = Rp at first frequency
                // 4 = Cp at second frequency
                // 5 = Rp at second frequency

                isMultiElectrode = !string.IsNullOrWhiteSpace(dataRows[0][1]);

                if (isMultiElectrode)
                {

                    for (int rowIndex = 0; rowIndex < dataRows.Count; rowIndex++)
                    {
                        modes.Add(dataRows[rowIndex][1].Trim());

                        List<double> cpRow = new List<double>();
                        List<double> rpRow = new List<double>();

                        for (int columnIndex = 2;
                             columnIndex < headers.Length - 1;
                             columnIndex += 2)
                        {
                            if (rowIndex == 0)
                            {
                                string[] headerParts = headers[columnIndex].Split(
                                    new[] { ' ' },
                                    StringSplitOptions.RemoveEmptyEntries);

                                if (headerParts.Length == 0)
                                {
                                    continue;
                                }

                                double frequency;

                                bool frequencyIsValid = double.TryParse(
                                    headerParts[0],
                                    NumberStyles.Float,
                                    CultureInfo.InvariantCulture,
                                    out frequency);

                                if (!frequencyIsValid || frequency <= 0)
                                {
                                    continue;
                                }

                                frequencies.Add(frequency);
                            }

                            double cp;
                            double rp;

                            bool cpIsValid = double.TryParse(
                                dataRows[rowIndex][columnIndex],
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out cp);

                            bool rpIsValid = double.TryParse(
                                dataRows[rowIndex][columnIndex + 1],
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out rp);

                            if (!cpIsValid || !rpIsValid)
                            {
                                continue;
                            }

                            cpRow.Add(cp);
                            rpRow.Add(rp);
                        }

                        cpMulti.Add(cpRow);
                        rpMulti.Add(rpRow);
                    }
                    AverageMultiElectrodeMeasurements(ref modes, ref cpMulti, ref rpMulti);
                    return frequencies.Count > 0;
                }
                else
                {

                    for (int columnIndex = 2;
                         columnIndex < headers.Length - 1;
                         columnIndex += 2)
                    {
                        string[] headerParts = headers[columnIndex].Split(
                            new[] { ' ' },
                            StringSplitOptions.RemoveEmptyEntries);

                        if (headerParts.Length == 0)
                        {
                            continue;
                        }

                        double frequency;

                        bool frequencyIsValid = double.TryParse(
                            headerParts[0],
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out frequency);

                        if (!frequencyIsValid || frequency <= 0)
                        {
                            continue;
                        }

                        double cpSum = 0.0;
                        double rpSum = 0.0;
                        bool valuesAreValid = true;

                        for (int rowIndex = 0;
                             rowIndex < dataRows.Count;
                             rowIndex++)
                        {
                            double cp;
                            double rp;

                            bool cpIsValid = double.TryParse(
                                dataRows[rowIndex][columnIndex],
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out cp);

                            bool rpIsValid = double.TryParse(
                                dataRows[rowIndex][columnIndex + 1],
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out rp);

                            if (!cpIsValid || !rpIsValid)
                            {
                                valuesAreValid = false;
                                break;
                            }

                            cpSum += cp;
                            rpSum += rp;
                        }

                        if (!valuesAreValid)
                        {
                            continue;
                        }

                        double cpMean = cpSum / dataRows.Count;
                        double rpMean = rpSum / dataRows.Count;

                        frequencies.Add(frequency);
                        cpValues.Add(cpMean);
                        rpValues.Add(rpMean);
                    }
                }
                return frequencies.Count > 0;
            }
             
            catch (Exception ex)
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "An error occurred while reading the CSV file:\n\n" +
                    ex.Message,
                    "CSV Reading Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        private bool CpRpToNyquist(
           IList<double> frequencies,
           IList<double> cpValues,
           IList<double> rpValues,
           out List<double> zLine,
           out List<double> z2Line)
        {
            zLine = new List<double>();
            z2Line = new List<double>();

            if (frequencies == null ||
                cpValues == null ||
                rpValues == null)
            {
                return false;
            }

            if (frequencies.Count != cpValues.Count ||
                frequencies.Count != rpValues.Count)
            {
                return false;
            }

            for (int i = 0; i < frequencies.Count; i++)
            {
                double frequency = frequencies[i];
                double cp = cpValues[i];
                double rp = rpValues[i];

                if (frequency <= 0 ||
                    double.IsNaN(cp) ||
                    double.IsInfinity(cp) ||
                    double.IsNaN(rp) ||
                    double.IsInfinity(rp))
                {
                    continue;
                }

                double omega = 2.0 * Math.PI * frequency;
                double omegaCpRp = omega * cp * rp;
                double denominator = 1.0 + Math.Pow(omegaCpRp, 2.0);

                double zReal =
                    rp / denominator;

                double zImaginary =
                    (omega * cp * Math.Pow(rp, 2.0)) /
                    denominator;

                zLine.Add(zReal);
                z2Line.Add(zImaginary);
            }

            return zLine.Count > 0;
        }

        private void AverageMultiElectrodeMeasurements(
            ref List<string> modes,
            ref List<List<double>> cpMulti,
            ref List<List<double>> rpMulti)
        {
            List<string> averagedModes =new List<string>();
            List<List<double>> averagedCpMulti =new List<List<double>>();
            List<List<double>> averagedRpMulti =new List<List<double>>();

            for (int rowIndex = 0;
                 rowIndex < modes.Count;
                 rowIndex++)
            {
                string currentMode = modes[rowIndex];

                if (averagedModes.Contains(currentMode))
                {
                    continue;
                }

                List<int> matchingRows =
                    new List<int>();

                for (int searchIndex = 0;
                     searchIndex < modes.Count;
                     searchIndex++)
                {
                    if (modes[searchIndex] == currentMode)
                    {
                        matchingRows.Add(searchIndex);
                    }
                }

                if (matchingRows.Count == 0)
                {
                    continue;
                }

                int frequencyCount =
                    cpMulti[matchingRows[0]].Count;

                List<double> cpMeanRow =new List<double>();

                List<double> rpMeanRow =new List<double>();

                for (int frequencyIndex = 0;
                     frequencyIndex < frequencyCount;
                     frequencyIndex++)
                {
                    double cpSum = 0.0;
                    double rpSum = 0.0;
                    int validMeasurements = 0;

                    for (int measurementIndex = 0;
                         measurementIndex < matchingRows.Count;
                         measurementIndex++)
                    {
                        int matchingRow =matchingRows[measurementIndex];

                        if (frequencyIndex >= cpMulti[matchingRow].Count ||
                            frequencyIndex >= rpMulti[matchingRow].Count)
                        {
                            continue;
                        }

                        cpSum +=cpMulti[matchingRow][frequencyIndex];
                        rpSum +=rpMulti[matchingRow][frequencyIndex];

                        validMeasurements++;
                    }

                    if (validMeasurements > 0)
                    {
                        cpMeanRow.Add(cpSum / validMeasurements);
                        rpMeanRow.Add(rpSum / validMeasurements);
                    }
                }
                averagedModes.Add(currentMode);
                averagedCpMulti.Add(cpMeanRow);
                averagedRpMulti.Add(rpMeanRow);
            }

            modes = averagedModes;
            cpMulti = averagedCpMulti;
            rpMulti = averagedRpMulti;
        }

        private bool CpRpMultiToNyquist(
            IList<double> frequencies,
            IList<List<double>> cpMulti,
            IList<List<double>> rpMulti,
            out List<List<double>> zRealMulti,
            out List<List<double>> zImagMulti)
        {
            zRealMulti = new List<List<double>>();
            zImagMulti = new List<List<double>>();

            if (frequencies == null ||
                cpMulti == null ||
                rpMulti == null)
            {
                return false;
            }

            if (cpMulti.Count == 0 ||
                cpMulti.Count != rpMulti.Count)
            {
                return false;
            }

            for (int modeIndex = 0;
                 modeIndex < cpMulti.Count;
                 modeIndex++)
            {
                if (cpMulti[modeIndex].Count != frequencies.Count ||
                    rpMulti[modeIndex].Count != frequencies.Count)
                {
                    return false;
                }

                List<double> zRealRow =
                    new List<double>();

                List<double> zImagRow =
                    new List<double>();

                for (int frequencyIndex = 0;
                     frequencyIndex < frequencies.Count;
                     frequencyIndex++)
                {
                    double frequency =
                        frequencies[frequencyIndex];

                    double cp =
                        cpMulti[modeIndex][frequencyIndex];

                    double rp =
                        rpMulti[modeIndex][frequencyIndex];

                    double omega =
                        2.0 * Math.PI * frequency;

                    double denominator =
                        1.0 + Math.Pow(omega * rp * cp, 2.0);

                    double zReal =
                        rp / denominator;

                    double zImag =
                        omega * cp * rp * rp / denominator;

                    zRealRow.Add(zReal);
                    zImagRow.Add(zImag);
                }

                zRealMulti.Add(zRealRow);
                zImagMulti.Add(zImagRow);
            }

            return zRealMulti.Count > 0;
        }
    }
}