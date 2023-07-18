using System;
using uwpGUI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.LLRP.LTK.LLRPV1;
using Org.LLRP.LTK.LLRPV1.DataType;
using Org.LLRP.LTK.LLRPV1.Impinj;
using System.Reflection.PortableExecutable;
using Windows.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using System.Data.SqlClient;
using Windows.UI.Xaml;
using System.Collections.ObjectModel;
using System.IO.Ports; // version 6.0
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Collections;
using System.Data.Common;
using Windows.Graphics.Display;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace backend
{
    class sensor 
    {
        SerialPort sp;
        MainPage mainPage;
        Reader reader;
        private int? currentRange;
        private int power;
        private int sensitivity;
        private int oldSpeed;
        private bool CheckGSSWorking = false;
        static readonly double[,] ranges = { { 0, 2.5 }, { 3.3, 4.5 }, { 5.2, 9 }, { 11, 19 }, { 21, 29 }, { 31, 43 }, { 46, 58 }, { 62, 77 }, { 82, 95 }, { 100, Double.PositiveInfinity } };
        public sensor(MainPage main,Reader R)
        {
            mainPage = main;
            reader = R;
            sp = new SerialPort("/dev/ttyUSB0", 19200, Parity.None, 8, StopBits.One);
            try
            {
                sp.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            }
            catch (Exception e)
            { Console.WriteLine("1st block error {0}", e.Message); }
        }
        public void open()
        {
            mainPage.SerialBrush = new SolidColorBrush(Colors.Green);
            sp.Open();
        }
        public void close()
        {
            mainPage.SerialBrush = new SolidColorBrush(Colors.Red);
            sp.Close();
        }
        public static int? DetermineRange(double value)
        {
            int? rangeIndex = null;

            for (int i = 0; i < ranges.GetLength(0); i++)
            {
                //Console.WriteLine("Min Range is:{0}, Max Range is:{1}, Speed is: {2}", ranges[i, 0], ranges[i, 1], value);
                if (value >= ranges[i, 0] && value <= ranges[i, 1])
                {
                    rangeIndex = i;
                    break;
                }
            }
            return rangeIndex;
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            bool changed = false;
            float speed = 0;
            int? newRange = null;
            string indata = null;
            lock (sp)
            {
                if (sp.IsOpen)
                    indata = sp.ReadLine();
                else
                    mainPage.DisplayDialog("Serial Port is close cannot read data","Serial port is close");
            }
            string[] parts = indata.Split(",");
            if (parts.Length > 2)
            {
                float.TryParse(parts[1], out speed);
                speed *= (float)0.621371;  // km/h (GSS speed output) to mph
                mainPage.SpeedText = speed.ToString();
                newRange = DetermineRange(speed);

                if (newRange != currentRange && newRange != null)
                {
                    switch (newRange)
                    {
                        case 0:
                            power = 32; sensitivity = -66;
                            changed = true;
                            break;
                        case 1:
                            power = 35; sensitivity = -66;
                            changed = true;
                            break;
                        case 2:
                            power = 42; sensitivity = -66;
                            changed = true;
                            break;
                        case 3:
                            power = 50; sensitivity = -66;
                            changed = true;
                            break;
                        case 4:
                            power = 57; sensitivity = -66;
                            changed = true;
                            break;
                        case 5:
                            power = 65; sensitivity = -66;
                            changed = true;
                            break;
                        case 6:
                            power = 72; sensitivity = -66;
                            changed = true;
                            break;
                        case 7:
                            power = 80; sensitivity = -66;
                            changed = true;
                            break;
                        case 8:
                            power = 90; sensitivity = -66;
                            changed = true;
                            break;
                        case 9:
                            power = 100; sensitivity = -66;
                            changed = true;
                            break;
                        default:

                            break;
                    }
                }

                if (reader.IsConnected())
                {
                    if (changed)
                    {
                        Console.WriteLine("current speed is:{0}, previous speed is: {1}", speed, oldSpeed);
                        lock (reader)
                        {
                            try
                            {
                                reader.power_dbm = (double)Math.Round((((double)power / 100 * 31.5) * 4), MidpointRounding.ToEven) / 4; // TxPowerInDbm only accept certain value 
                                reader.change_power(power);

                            }
                            catch (Exception error)
                            {
                                mainPage.DisplayDialog(string.Format("Lock error messsage: {0}", error.ToString()),"");
                            }

                        }
                    }
                }

                //Console.WriteLine("Speed:{0}, Direction: {1}", SDK.speed , gssDirection);
                if (!CheckGSSWorking)
                {
                    Console.WriteLine("GSS Working");
                    CheckGSSWorking = true;
                }
            }
        }
    }
     class Reader
    {
        MainPage mainPage;
        LLRPClient Client;
        public double power_dbm;
        private sensor Sensor;

        //Tags tag;
        //static readonly string format = "yyyy/MM/dd HH:mm:ss.fffff"; // the date and time formatting
        public Reader(MainPage main, sensor s) 
        {
            mainPage= main;
            Client = new LLRPClient();
            Sensor = s;
            ENUM_ConnectionAttemptStatusType status;
            Client.Open("SpeedwayR-13-DB-A2.local", 2000, out status); // UKRREN Reader
            //reader.Open("SpeedwayR-14-4E-40.local", 2000, out status); // Darby Reader
            if (status != ENUM_ConnectionAttemptStatusType.Success)
            {
                mainPage.DisplayDialog("Error", string.Format("Could not connect: {0}", status));
                CoreApplication.Exit();
            }
            Client.OnRoAccessReportReceived += new delegateRoAccessReport(OnReportEvent);



            MSG_IMPINJ_ENABLE_EXTENSIONS imp_msg = new MSG_IMPINJ_ENABLE_EXTENSIONS();
            MSG_ERROR_MESSAGE msg_err;
            MSG_CUSTOM_MESSAGE cust_rsp = Client.CUSTOM_MESSAGE(imp_msg, out msg_err, 8000);

            MSG_IMPINJ_ENABLE_EXTENSIONS_RESPONSE msg_rsp = cust_rsp as MSG_IMPINJ_ENABLE_EXTENSIONS_RESPONSE;
            if (msg_rsp != null)
            {
                if (msg_rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                {
                    mainPage.DisplayDialog("Error", msg_rsp.LLRPStatus.StatusCode.ToString());
                    Client.Close();
                    return;
                }
            }
            else if (msg_err != null)
            {
                mainPage.DisplayDialog("Error", msg_err.ToString());
                Client.Close();
                return;
            }
            else
            {
                mainPage.DisplayDialog("Timeout", "Enable Extensions Command Timed out");
                Client.Close();
                return;
            }
            Delete_RoSpec();
            Set_ReaderConfig();// Set initial power function
            Add_RoSpec();
            Enable_RoSpec();
        }
        public bool IsConnected() 
        {
            return Client.IsConnected;
        }
        void Add_RoSpec()
        {
            MSG_ERROR_MESSAGE msg_err;
            MSG_ADD_ROSPEC msg = new MSG_ADD_ROSPEC();

            // Reader Operation Spec (ROSpec)
            msg.ROSpec = new PARAM_ROSpec();
            // ROSpec must be disabled by default
            msg.ROSpec.CurrentState = ENUM_ROSpecState.Disabled;
            // The ROSpec ID can be set to any number
            // You must use the same ID when enabling this ROSpec
            msg.ROSpec.ROSpecID = 123;

            // ROBoundarySpec
            // Specifies the start and stop triggers for the ROSpec
            msg.ROSpec.ROBoundarySpec = new PARAM_ROBoundarySpec();
            // Immediate start trigger
            // The reader will start reading tags as soon as the ROSpec
            // is enabled
            msg.ROSpec.ROBoundarySpec.ROSpecStartTrigger = new PARAM_ROSpecStartTrigger();
            msg.ROSpec.ROBoundarySpec.ROSpecStartTrigger.ROSpecStartTriggerType = ENUM_ROSpecStartTriggerType.Immediate;

            // No stop trigger. Keep reading tags until the ROSpec is disabled.
            msg.ROSpec.ROBoundarySpec.ROSpecStopTrigger = new PARAM_ROSpecStopTrigger();
            msg.ROSpec.ROBoundarySpec.ROSpecStopTrigger.ROSpecStopTriggerType = ENUM_ROSpecStopTriggerType.Null;
            //msg.ROSpec.ROBoundarySpec.ROSpecStopTrigger.DurationTriggerValue = 100;

            // Antenna Inventory Spec (AISpec)
            // Specifies which antennas and protocol to use
            msg.ROSpec.SpecParameter = new UNION_SpecParameter();
            PARAM_AISpec aiSpec = new PARAM_AISpec();
            aiSpec.AntennaIDs = new UInt16Array();

            PARAM_C1G2InventoryCommand c1g2Inv = new PARAM_C1G2InventoryCommand();
            PARAM_C1G2RFControl c1g2RF = new PARAM_C1G2RFControl();
            c1g2RF.ModeIndex = 1000;
            c1g2RF.Tari = 0;
            c1g2Inv.C1G2RFControl = c1g2RF;

            // Set the session.
            PARAM_C1G2SingulationControl c1g2Sing = new PARAM_C1G2SingulationControl();
            c1g2Sing.Session = new TwoBits(1);
            c1g2Sing.TagPopulation = 1;
            c1g2Sing.TagTransitTime = 0;
            c1g2Inv.C1G2SingulationControl = c1g2Sing;
            c1g2Inv.TagInventoryStateAware = false;
            // Set the search mode.
            PARAM_ImpinjInventorySearchMode impISM = new PARAM_ImpinjInventorySearchMode();
            impISM.InventorySearchMode = ENUM_ImpinjInventorySearchType.Dual_Target;
            c1g2Inv.Custom.Add(impISM);


            // Enable all antennas
            aiSpec.AntennaIDs.Add(0);
            //PARAM_C1G2UHFRFModeTableEntry RFMode = new PARAM_C1G2UHFRFModeTableEntry();
            //RFMode.ModeIdentifier = 1000;
            // No AISpec stop trigger. It stops when the ROSpec stops.
            aiSpec.AISpecStopTrigger = new PARAM_AISpecStopTrigger();
            aiSpec.AISpecStopTrigger.AISpecStopTriggerType = ENUM_AISpecStopTriggerType.Null;
            aiSpec.InventoryParameterSpec = new PARAM_InventoryParameterSpec[1];
            aiSpec.InventoryParameterSpec[0] = new PARAM_InventoryParameterSpec();
            aiSpec.InventoryParameterSpec[0].InventoryParameterSpecID = 1234;
            aiSpec.InventoryParameterSpec[0].ProtocolID = ENUM_AirProtocols.EPCGlobalClass1Gen2;
            aiSpec.InventoryParameterSpec[0].AntennaConfiguration = new PARAM_AntennaConfiguration[1];
            aiSpec.InventoryParameterSpec[0].AntennaConfiguration[0] = new PARAM_AntennaConfiguration();
            aiSpec.InventoryParameterSpec[0].AntennaConfiguration[0].AntennaID = 0;
            // Add the inventory command to the AI spec.
            aiSpec.InventoryParameterSpec[0].AntennaConfiguration[0].AirProtocolInventoryCommandSettings.Add(c1g2Inv);
            msg.ROSpec.SpecParameter.Add(aiSpec);

            // Report Spec
            msg.ROSpec.ROReportSpec = new PARAM_ROReportSpec();
            // Send a report for every tag read
            msg.ROSpec.ROReportSpec.ROReportTrigger = ENUM_ROReportTriggerType.Upon_N_Tags_Or_End_Of_ROSpec;
            msg.ROSpec.ROReportSpec.N = 1;
            msg.ROSpec.ROReportSpec.TagReportContentSelector = new PARAM_TagReportContentSelector();
            msg.ROSpec.ROReportSpec.TagReportContentSelector.EnableFirstSeenTimestamp = true;
            msg.ROSpec.ROReportSpec.TagReportContentSelector.EnablePeakRSSI = true;
            MSG_ADD_ROSPEC_RESPONSE rsp;
            lock (this)
            {
                rsp = this.Client.ADD_ROSPEC(msg, out msg_err, 2000);
            }
            if (rsp != null)
            {
                // Success
                //Log.Information("Reader operation spec added successfully");
            }
            else if (msg_err != null)
            {
                // Error
                mainPage.DisplayDialog("Error",msg_err.ToString());
            }
            else
            {
                // Timeout
                mainPage.DisplayDialog("Timeout","Timeout Error.");
            }
        }
        public void Delete_RoSpec()
        {
            MSG_DELETE_ROSPEC msg = new MSG_DELETE_ROSPEC();
            msg.ROSpecID = 0;
            MSG_ERROR_MESSAGE msg_err;

            MSG_DELETE_ROSPEC_RESPONSE rsp;
            lock (this)
            {
                rsp = this.Client.DELETE_ROSPEC(msg, out msg_err, 2000);
            }
            if (rsp != null)
            {
                // Success
                //Sensor.close();
                mainPage.ReaderBrush = new SolidColorBrush(Colors.Red);
                //Log.Information("Reader operation spec deleted successfully");
            }
            else if (msg_err != null)
            {
                // Error
                mainPage.DisplayDialog("Error", msg_err.ToString());
            }
            else
            {
                // Timeout
                mainPage.DisplayDialog("Timeout", "Timeout Error.");
            }
        }
        void Enable_RoSpec()
        {
            MSG_ERROR_MESSAGE msg_err;
            MSG_ENABLE_ROSPEC msg = new MSG_ENABLE_ROSPEC();
            msg.ROSpecID = 123;
            MSG_ENABLE_ROSPEC_RESPONSE rsp;
            lock (this)
            {
                rsp = this.Client.ENABLE_ROSPEC(msg, out msg_err, 2000);
            }
            if (rsp != null)
            {
                // Success
                //Sensor.open();
                mainPage.ReaderBrush = new SolidColorBrush(Colors.Green);
            }
            else if (msg_err != null)
            {
                // Error
                mainPage.DisplayDialog("Error", msg_err.ToString());
            }
            else
            {
                // Timeout
                mainPage.DisplayDialog("Timeout", "Timeout Error.");
            }
            //MSG_ENABLE_EVENTS_AND_REPORTS Emsg = new MSG_ENABLE_EVENTS_AND_REPORTS();
            //reader.ENABLE_EVENTS_AND_REPORTS(Emsg, out msg_err, 2000);
        }
        void Set_ReaderConfig()
        {
            MSG_ERROR_MESSAGE msg_err;
            MSG_SET_READER_CONFIG msg = new MSG_SET_READER_CONFIG();



            msg.AntennaConfiguration = new PARAM_AntennaConfiguration[1];
            msg.AntennaConfiguration[0] = new PARAM_AntennaConfiguration();
            msg.AntennaConfiguration[0].RFTransmitter = new PARAM_RFTransmitter();
            msg.AntennaConfiguration[0].RFTransmitter.ChannelIndex = 1;
            msg.AntennaConfiguration[0].RFTransmitter.HopTableID = 1;
            //msg.AntennaConfiguration[0].RFTransmitter.TransmitPower = 87; //max power

            msg.AntennaConfiguration[0].RFTransmitter.TransmitPower = 1; //min power


            // Create a one-element array to hold the GPI configuration
            // We are only enabling one GPI in this example
            msg.ReaderEventNotificationSpec = new PARAM_ReaderEventNotificationSpec();
            PARAM_EventNotificationState[] even = new PARAM_EventNotificationState[1];
            even[0] = new PARAM_EventNotificationState();
            even[0].EventType = ENUM_NotificationEventType.GPI_Event;
            even[0].NotificationState = true;
            msg.ReaderEventNotificationSpec.EventNotificationState = even;
            //msg.ReaderEventNotificationSpec.EventNotificationState[0].EventType = ENUM_NotificationEventType.GPI_Event;
            //msg.ReaderEventNotificationSpec.EventNotificationState[0].NotificationState = true;

            msg.GPIPortCurrentState = new PARAM_GPIPortCurrentState[4];
            //PARAM_C1G2UHFRFModeTableEntry x = new PARAM_C1G2UHFRFModeTableEntry();
            //x.ModeIdentifier = 1000;
            for (int i = 0; i < 4; i++)
            {
                msg.GPIPortCurrentState[i] = new PARAM_GPIPortCurrentState();
                // Enable GPI port #1
                msg.GPIPortCurrentState[i].GPIPortNum = (ushort)(i + 1);
                msg.GPIPortCurrentState[i].Config = true;
            }
            MSG_SET_READER_CONFIG_RESPONSE rsp;
            lock (this)
            {
                rsp = this.Client.SET_READER_CONFIG(msg, out msg_err, 2000);
            }
            if (rsp != null)
            {
                // Success
                //Log.Information("Reader configuration set successfully");
            }
            else if (msg_err != null)
            {
                // Error
                mainPage.DisplayDialog("Error", msg_err.ToString());
            }
            else
            {
                // Timeout
                mainPage.DisplayDialog("Timeout", "Timeout Error");
            }
        }
        public void change_power(double power)
        {
            MSG_SET_READER_CONFIG msg = new MSG_SET_READER_CONFIG();
            MSG_ERROR_MESSAGE msg_err = new MSG_ERROR_MESSAGE();
            power = power / 100 * 31.5;
            power_dbm = (double)Math.Round((power * 4), MidpointRounding.ToEven) / 4;
            ushort i = (ushort)(power / 0.25 - 39);
            msg.AntennaConfiguration = new PARAM_AntennaConfiguration[1];
            msg.AntennaConfiguration[0] = new PARAM_AntennaConfiguration();
            msg.AntennaConfiguration[0].AirProtocolInventoryCommandSettings = new UNION_AirProtocolInventoryCommandSettings();
            //PARAM_C1G2InventoryCommand cmd = new PARAM_C1G2InventoryCommand();
            //msg.AntennaConfiguration[0].AirProtocolInventoryCommandSettings.Add(cmd);
            msg.AntennaConfiguration[0].AntennaID = 0;

            //msg.AntennaConfiguration[0].RFReceiver = new PARAM_RFReceiver();
            //// Receiver sensitivity
            //msg.AntennaConfiguration[0].RFReceiver.ReceiverSensitivity = 1;

            msg.AntennaConfiguration[0].RFTransmitter = new PARAM_RFTransmitter();
            msg.AntennaConfiguration[0].RFTransmitter.ChannelIndex = 1;
            msg.AntennaConfiguration[0].RFTransmitter.HopTableID = 1;
            // Transmit power
            msg.AntennaConfiguration[0].RFTransmitter.TransmitPower = i;
            MSG_SET_READER_CONFIG_RESPONSE rsp;
            lock (this)
            {
                rsp = this.Client.SET_READER_CONFIG(msg, out msg_err, 2000);
            }
            if (rsp != null)
            {
                // Success
                //Console.WriteLine(rsp.ToString());
            }
            else if (msg_err != null)
            {
                // Error
                mainPage.DisplayDialog(string.Format(msg_err.ToString()),"Change power error");
            }
            else
            {
                // Timeout
                mainPage.DisplayDialog(string.Format(msg_err.ToString()), "Change power Timeout");
            }
        }

        private void OnReportEvent(MSG_RO_ACCESS_REPORT msg)
        {
            if (msg.TagReportData != null)
            {
                DateTime deviceTime = new DateTime(); // the time when the read tag recieved by the device (PC/RPI)
                DateTime currenttime = new DateTime(); // the time when the tag is read by the reader
                double difference = (double)deviceTime.Subtract(currenttime).Ticks / TimeSpan.TicksPerSecond;
                for (int i = 0; i < msg.TagReportData.Length; i++)
                {
                    if (msg.TagReportData[i].EPCParameter.Count > 0)
                    {

                        string epc;
                        if (msg.TagReportData[i].EPCParameter[0].GetType() == typeof(PARAM_EPC_96))
                        {
                            epc = ((PARAM_EPC_96)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                        }
                        else
                        {
                            epc = ((PARAM_EPCData)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                        }
                        DateTime current_time = new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(msg.TagReportData[i].FirstSeenTimestampUTC.Microseconds / 1000);
                        deviceTime = DateTime.Now;
                        mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                             mainPage.setTags(new Tags
                            {
                                EPC = epc,
                                Date_Time = current_time,
                                Speed = 0,
                                Power = 0,
                                Peak_RSSI = msg.TagReportData[i].PeakRSSI.PeakRSSI,
                                Time_PC = deviceTime
                            });
                        });
                    }
                }
            }
            else
                return;
        }

    }
    //public class Tags
    //{
    //    static public ObservableCollection<Tags> tagsList;
    //    public string EPC { get; set; }
    //    public DateTime FirstSeen { get; set; }
    //    public DateTime LastSeen { get; set; }
    //    public double Accuracy { get; set; }
    //    public int SeenTimes { get; set; }
    //    public double RealSpeed { get; set; }
    //    public double RealAccuracy { get; set; }
    //    public TimeSpan MinDelay { get; set; }
    //    public TimeSpan MaxDelay { get; set; }
    //    public TimeSpan DiffT4LastSeen { get; set; }
    //    public TimeSpan FirstAssetExactDetection { get; set; }
    //    public double MinusAccuracy { get; set; }
    //    public double PlusAccuracy { get; set; }
    //    public double MaxAccuracyError { get; set; }
    //}
    public class Tags
    {
        static public ObservableCollection<Tags> tagsList = new ObservableCollection<Tags>();
        public string EPC { get; set; }
        public DateTime Date_Time { get; set; }
        public double Speed { get; set; }
        public double Power { get; set; }
        public double Peak_RSSI { get; set; }
        public DateTime Time_PC { get; set; }
    }
}
