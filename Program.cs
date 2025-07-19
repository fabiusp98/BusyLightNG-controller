namespace busyLightNG
{
    using System;
    using System.IO.Ports;
    using System.Timers;
    using busyLightNG.resources;
    using Microsoft.Win32;

    internal class Program
    {
        //Menu stuff
        public static NotifyIcon icon;
        public static ContextMenuStrip IconMenuStrip;
        public static ToolStripMenuItem ExitButton;
        public static ToolStripMenuItem InfoButton;
        public static ToolStripMenuItem SerialButton;
        public static ToolStripMenuItem ModeButton;
        public static ToolStripMenuItem ModeAutoButton;
        public static ToolStripMenuItem ModeManualOnButton;
        public static ToolStripMenuItem ModeManualOffButton;
        public static ToolStripMenuItem AboutButton;
        public static System.Timers.Timer UpdateTimer;

        //State
        static bool ReconnectState = false;

        //Serial
        public static SerialPort SPort;

        //Entry point
        [STAThread]
        static void Main()
        {
            //Housekeeping
            ApplicationConfiguration.Initialize();
            UpdateTimer = new System.Timers.Timer(1000);
            UpdateTimer.Elapsed += UpdateStatusStub;
            UpdateTimer.AutoReset = true;


            //Set up systray icon and change appearance
            icon = new NotifyIcon();
            icon.Icon = Properties.Resources.ico_err;

            //Set up context menu and items
            IconMenuStrip = new ContextMenuStrip();

            InfoButton = new ToolStripMenuItem("Info"); //Info line
            InfoButton.Text = "BusyLightNG 1.1";
            InfoButton.Enabled = false;
            IconMenuStrip.Items.Add(InfoButton);

            ModeAutoButton = new ToolStripMenuItem("Auto"); //Mode auto button
            ModeAutoButton.Image = Properties.Resources.Activity;
            ModeAutoButton.Text = "Automatic";
            ModeAutoButton.Checked = true;
            ModeAutoButton.Click += new System.EventHandler(ModeAutoButtonClick);

            ModeManualOnButton = new ToolStripMenuItem("ManOn"); //Mode manual on button
            ModeManualOnButton.Image = Properties.Resources.BreakpointTemporary;
            ModeManualOnButton.Text = "Manual - On call";
            ModeManualOnButton.Click += new System.EventHandler(ModeManOnButtonClick);

            ModeManualOffButton = new ToolStripMenuItem("ManOff"); //Mode manual off button
            ModeManualOffButton.Text = "Manual - Available";
            ModeManualOffButton.Image = Properties.Resources.BreakpointTemporaryAvailable;
            ModeManualOffButton.Click += new System.EventHandler(ModeManOffButtonClick);

            ModeButton = new ToolStripMenuItem("Mode"); //Mode button
            ModeButton.Text = "Mode";
            ModeButton.Image = Properties.Resources.Settings;
            ModeButton.DropDownItems.AddRange([ModeAutoButton, ModeManualOnButton, ModeManualOffButton]);
            ModeButton.Click += new System.EventHandler(ModeAutoButtonClick);
            IconMenuStrip.Items.Add(ModeButton);

            SerialButton = new ToolStripMenuItem("Serial"); //Mode button
            SerialButton.Text = "Serial port";
            SerialButton.Image = Properties.Resources.ConnectToEnvironment;
            UpdateSerialPorts();    //Manually update serial port listing
            IconMenuStrip.Items.Add(SerialButton);

            AboutButton = new ToolStripMenuItem("About"); //About button
            AboutButton.Image = Properties.Resources.QuestionMark;
            AboutButton.Text = "Exit";
            IconMenuStrip.Items.Add(AboutButton);
            AboutButton.Click += new System.EventHandler(AboutButtonClick);

            ExitButton = new ToolStripMenuItem("Exit"); //Exit button
            ExitButton.Image = Properties.Resources.Close;
            ExitButton.Text = "About";
            IconMenuStrip.Items.Add(ExitButton);
            ExitButton.Click += new System.EventHandler(ExitButtonClick);

            //Display icon
            icon.ContextMenuStrip = IconMenuStrip;
            icon.Visible = true;

            //Set up serial port
            SPort = new SerialPort();
            SPort.BaudRate = 115200;
            SPort.Parity = Parity.None;
            SPort.DataBits = 8;
            SPort.StopBits = StopBits.One;
            SPort.Handshake = Handshake.None;

            Application.Run();  //Spawn main thread without forms
        }

        //Menu event handlers
        private static void ExitButtonClick(object sender, EventArgs e) //On exit, close serial port before quitting
        {
            SPort.Close();
            Application.Exit();
        } 
        private static void ModeAutoButtonClick(object sender, EventArgs e)
        {
            ModeManualOnButton.Checked = false; //Change ticked line
            ModeManualOffButton.Checked = false;
            ModeAutoButton.Checked = true;
            ReconnectState = false; //Show error if serial port fails
            UpdateStatus(); //Run a status update immediately (to avoid the 1s delay)
            UpdateTimer.Start();    //Start timer
        }
        private static void ModeManOnButtonClick(object sender, EventArgs e)
        {
            ModeManualOnButton.Checked = true;  //Change ticked line
            ModeManualOffButton.Checked = false;
            ModeAutoButton.Checked = false;
            UpdateTimer.Stop(); //Stop automatic update timer
            icon.Icon = Properties.Resources.ico_on_man;    //Change icon
            SetColor(1);    //Change color
        }
        private static void ModeManOffButtonClick(object sender, EventArgs e)
        {
            ModeManualOnButton.Checked = false; //Change ticked line
            ModeManualOffButton.Checked = true;
            ModeAutoButton.Checked = false;
            UpdateTimer.Stop(); //Stop automatic update timer
            icon.Icon = Properties.Resources.ico_off_man;   //Change icon
            SetColor(0);    //Change color
        }
        private static void SerialButtonClick(object sender, EventArgs e)
        {
            if(sender.GetType().GetProperty("Name").GetValue(sender) == "Update")   //If update button, manually refresh available serial ports
            {
                UpdateSerialPorts();
            } else //Otherwise, try to set up Serial connection
            {
                try
                {
                    UpdateTimer.Stop(); //Stop auto update timer (if running)
                    SPort.Close();  //Close serial port
                    SPort.PortName = (string)sender.GetType().GetProperty("Name").GetValue(sender); //Change port assignment
                    SPort.Open();   //Open serial port with new port
                    ReconnectState = false; //Show error if serial port fails
                    UpdateStatus(); //Run status update and start timer as usual
                    UpdateTimer.Start();
                    icon.ShowBalloonTip(5000, "Connected to BusyLight", "Connected to BusyLight on "+ (string)sender.GetType().GetProperty("Name").GetValue(sender), ToolTipIcon.Info); //Show popup
                }
                catch (Exception excp)  //If serial port fails to open
                {
                    icon.Icon = Properties.Resources.ico_err;   //Set error icon
                    icon.ShowBalloonTip(5000, "Unable to open serial port", excp.ToString(), ToolTipIcon.Error);    //Show popup
                }
            }

        }
        private static void AboutButtonClick(object sender, EventArgs e)    //When about button clicked, instanciate and show about dialog
        {
            AboutDialog aboutDialog = new AboutDialog();
            aboutDialog.Show();
        }

        //Status check
        private static void UpdateStatusStub(Object source, ElapsedEventArgs e) //Stub for unused event context data
        {
            UpdateStatus();
        }
        private static void UpdateStatus()
        {
            try
            {
                RegistryKey currUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default); //Open registry hive on HKEY_CURRENT_USER
                bool MicInUse = false;  //Result register for looping with state

                //First check for Win32 applications
                String[] Subkeys = currUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged").GetSubKeyNames();    //Get all subfolders under the magic path

                foreach (var item in Subkeys)   //Loop across all the subfolders
                {
                    long a = (long) currUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged\\" + item).GetValue("LastUsedTimeStop");
                    if (a == 0) //Check if last used time is zero
                    {
                        MicInUse = true;    //If it is, set register
                    }
                }

                Subkeys = currUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone").GetSubKeyNames();    //Get all subfolders under the magic path

                foreach (var item in Subkeys)   //Loop across all the subfolders
                {
                    if(item == "NonPackaged")
                    {
                        continue;
                    }
                    if(currUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\" + item).GetValue("LastUsedTimeStop") == null)   //Don't look for "LastUsedTimeStop" if it doesn't exist
                    {
                        continue;
                    }

                    long a = (long)currUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\" + item).GetValue("LastUsedTimeStop");
                    if (a == 0) //Check if last used time is zero
                    {
                        MicInUse = true;    //If it is, set register
                    }
                }

                //Then check for Modern applications

                if (MicInUse) 
                {
                    if(SetColor(1)) //Apply new icon only if serial port write happens succesfully
                    {
                        icon.Icon = Properties.Resources.ico_on;
                    }             
                } else
                {
                    if(SetColor(0))
                    {
                        icon.Icon = Properties.Resources.ico_off;
                    }  
                }
            }
            catch (Exception e) //If reading from the registry fails
            {
                    UpdateTimer.Stop();
                    SPort.Close();  //Close serial port
                    icon.Icon = Properties.Resources.ico_err;   //Set error icon
                    icon.ShowBalloonTip(5000, "Runtime error", e.ToString(), ToolTipIcon.Error);    //Show popup

            }
        }

        //Serial port handling
        private static bool SetColor(int color) //0: green, 1: red
        {
            try
            {
                if (ReconnectState)  //If connection failed before
                {
                    SPort.Open();   //Try to reopen the serial port (or fail silently)
                    ReconnectState = false; //Set up context for a working port
                    icon.ShowBalloonTip(5000, "Connected to BusyLight", "Reestabilished connection with device", ToolTipIcon.Info); //Show popup

                }

                SPort.Write(color.ToString());

                return (true);  //Return succesful execution to calling function
            }
            catch (Exception e)
            {
                if(!ReconnectState)
                {
                    //UpdateTimer.Stop();
                    ReconnectState = true;  //Set context for auto-reconnect
                    SPort.Close();
                    icon.Icon = Properties.Resources.ico_err;
                    icon.ShowBalloonTip(5000, "Serial port error", e.ToString(), ToolTipIcon.Error);
                }
                 //Return failed execution to calling function
            }

            return (false); //Execution never ends up here, but compiler is unhappy otherwise :-)

        }
        private static void UpdateSerialPorts()
        {
            SerialButton.DropDownItems.Clear(); //Delete everything
            foreach (string s in SerialPort.GetPortNames()) //Bulk add serial ports
            {
                SerialButton.DropDownItems.Add(new ToolStripMenuItem(s, Properties.Resources.SerialPort, SerialButtonClick, s));
            }
            SerialButton.DropDownItems.Add(new ToolStripMenuItem("Update", Properties.Resources.UpdateListItem, SerialButtonClick, "Update"));  //Add back update button as last
        }
    }
}