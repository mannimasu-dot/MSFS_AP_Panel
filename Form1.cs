using Microsoft.FlightSimulator.SimConnect;
using System;
// using System.Collections.Generic; // entfernt: Probe-Hilfsfunktion nicht mehr benötigt
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MSFS_AP_Panel
{
    public partial class Form1 : Form
    {

        // Konstante für die Windows-Nachrichtenschleife (SimConnect-Kommunikation)
        private const int WM_USER_SIMCONNECT = 0x0402;

        private SimConnect? simconnect = null;
        // Edit-Lock: true, während der Benutzer txtAltSoll bearbeitet bis Enter gedrückt wird
        private bool isEditingAltSoll = false;
        // Edit-Lock für txtVS (VS-Soll)
        private bool isEditingVS = false;
        // Edit-Lock für txtHDGSoll (HDG-Soll)
        private bool isEditingHdgSoll = false;
        // Edit-Lock für txtNav1 (NAV1-Frequenz)
        // Wird aktuell vom UI-Flow nicht benötigt, aber für Konsistenz belassen
        private bool isEditingNav1 = false;
        // Aktueller APR-Status (true = APPROACH ON)
        private bool isAprOn = false;
        // Aktueller ALT-Status (true = ALT ON)
        private bool isAltOn = false;
        // Aktueller HDG-Status (true = HDG Hold ON)
        private bool isHdgOn = false;
        // Zuletzt bekannter VS-Soll
        private int lastVsSollInt = 0;
        // Letzter bekannter Sollwert (ganzzahlig, Fuß)
        private int lastAltTargetInt = 0;
        // Letzter bekannter HDG-Sollwert (ganzzahlig, Grad)
        private int lastHdgTargetInt = 0;
        // SimConnect-Daten-Definitionen / Requests
        private enum DEFINITIONS : uint { AP_STATE = 0, ALTITUDE = 1, ALT_TARGET = 2, VERTICAL_SPEED = 3, VERT_HOLD = 4, ALT_LOCK = 5, HEADING = 6, HEADING_LOCK = 7, HEADING_TARGET = 8, APPROACH_ACTIVE = 9, NAV1_FREQUENCY = 10, AIRSPEED = 11 }
        // Nur noch CHANGED-Subscription verwenden; ONCE-Logik entfernt
        private enum DATA_REQUESTS : uint { REQUEST_AP_STATE_CHANGED = 0, REQUEST_ALTITUDE = 1, REQUEST_ALT_TARGET = 2, REQUEST_VERTICAL_SPEED = 3, REQUEST_VERT_HOLD = 4, REQUEST_ALT_LOCK = 5, REQUEST_HEADING = 6, REQUEST_HEADING_LOCK = 7, REQUEST_HEADING_TARGET = 8, REQUEST_APPROACH = 9, REQUEST_NAV1 = 10, REQUEST_AIRSPEED = 11 }
        private enum CLIENT_EVENTS : uint { AP_MASTER = 0, AP_PANEL_ALTITUDE_SET = 1, AP_PANEL_VS_SET = 2, AP_VS_VAR_SET_ENGLISH = 3, AP_ALT_VAR_SET_ENGLISH = 4, AP_PANEL_HEADING_HOLD = 5, AP_PANEL_HEADING_BUG_SET = 6, AP_APR_HOLD = 7, NAV1_RADIO_SET = 8 }
        private const CLIENT_EVENTS EventID = CLIENT_EVENTS.AP_MASTER;

        // Notification-Groups definieren (für Mapping/Transmit)
        private enum NOTIFICATION_GROUPS : uint { GROUP0 = 0 }
#pragma warning disable CS0649

        private static class SimVarNames
        {
            // SimVar-Namen
            public const string AUTOPILOT_MASTER = "AUTOPILOT MASTER";  //btnAutopilot => Autopilot ON/OFF
            public const string PLANE_ALTITUDE = "PLANE ALTITUDE";  //lblAltIst => Anzeige der aktuellen Höhe
            public const string AUTOPILOT_ALTITUDE_LOCK = "AUTOPILOT ALTITUDE LOCK";  //btnALT => Anzeige Höhenregelung aktive
            public const string AUTOPILOT_ALTITUDE_LOCK_VAR = "AUTOPILOT ALTITUDE LOCK VAR";  //txtAltSoll => Anzeige und Setzen der Sollhöhe
            public const string VERTICAL_SPEED = "VERTICAL SPEED";  //ctrVS => Anzeige der aktuellen Vertikalgeschwindigkeit
            public const string AUTOPILOT_VERTICAL_HOLD_VAR = "AUTOPILOT VERTICAL HOLD VAR";  //txtVS => Anzeige und Setzen der Soll-Vertikalgeschwindigkeit
            public const string PLANE_HEADING_DEGREES_TRUE = "PLANE HEADING DEGREES MAGNETIC"; // lblHDGIst => Anzeige Flugrichtung (HDG)
            public const string AUTOPILOT_HEADING_LOCK = "AUTOPILOT HEADING LOCK"; // btnHDG => Anzeige Heading Hold aktiv
            public const string AUTOPILOT_HEADING_LOCK_DIR = "AUTOPILOT HEADING LOCK DIR"; // txtHDGSoll => Anzeige Heading (HDG) Sollwert
            public const string AUTOPILOT_APPROACH_ACTIVE = "AUTOPILOT APPROACH ACTIVE"; // btnAPPR => Anzeige Anflug (ILS / Approach) aktiv
            public const string NAV_ACTIVE_FREQUENCY_1 = "NAV ACTIVE FREQUENCY:1"; // txtNav1 => Anzeige NAV1-Frequenz
            public const string AIRSPEED_INDICATED = "AIRSPEED INDICATED"; // Fluggeschwindigkeit in Knoten
        }

        private static class SimEventNames
        {
            // Event-Strings
            public const string AP_MASTER = "AP_MASTER";  // schaltet den Autopiloten ein/aus
            public const string AP_PANEL_ALTITUDE_SET = "AP_PANEL_ALTITUDE_SET";  // setzt die Sollhöhe (ALT) auf den Wert von AUTOPILOT ALTITUDE LOCK VAR
            public const string AP_PANEL_VS_SET = "AP_PANEL_VS_SET"; // setzt die Soll-Vertikalgeschwindigkeit (VS) auf den Wert von AUTOPILOT VERTICAL HOLD VAR
            public const string AP_VS_VAR_SET_ENGLISH = "AP_VS_VAR_SET_ENGLISH"; // setzt die Soll-Vertikalgeschwindigkeit (VS) auf den Wert von AUTOPILOT VERTICAL HOLD VAR
            public const string AP_ALT_VAR_SET_ENGLISH = "AP_ALT_VAR_SET_ENGLISH"; // setzt die Sollhöhe (ALT) auf den Wert von AUTOPILOT ALTITUDE LOCK VAR
            public const string AP_PANEL_HEADING_HOLD = "AP_PANEL_HEADING_HOLD"; // schaltet Flugrichtung (HDG) ein/aus
            public const string HEADING_BUG_SET = "HEADING_BUG_SET"; // setzt die Soll Flugrichtung HDG 
            public const string AP_APR_HOLD = "AP_APR_HOLD"; // setzt den Automatik Anflug (ILS-System) ON/OFF
            public const string NAV1_RADIO_SET = "NAV1_RADIO_SET"; // setzt die Empfangs-Frequenz des ILS Automatik Anflug (ILS-System)
        }

        public Form1()
        {
            InitializeComponent();
            // Fenster immer im Vordergrund halten
            this.TopMost = true;
            // Setze Standard-Starttext (Label vom Designer)
            lblStatus.Text = "Start...";
            TryConnect();
        }

        private void TryConnect()
        {
            try
            {
                UpdateStatus("TryConnect: start", System.Drawing.Color.Blue);

                // Verbindung zu MSFS herstellen (verwendet das Fenster-Handle)
                simconnect = new SimConnect("MSFS_AP_Panel", this.Handle, WM_USER_SIMCONNECT, null, 0);
                UpdateStatus("SimConnect: Message empfang erstellt", System.Drawing.Color.DarkGreen);

                // Event-Handler registrieren
                simconnect.OnRecvOpen += Simconnect_OnRecvOpen;
                simconnect.OnRecvQuit += Simconnect_OnRecvQuit;
                simconnect.OnRecvException += Simconnect_OnRecvException;
                // Empfängt Datennachrichten (bytype)
                simconnect.OnRecvSimobjectDataBytype += Simconnect_OnRecvSimobjectDataBytype;
                // Manche Requests liefern SIMOBJECT_DATA (nicht BYTYPE) - beide Handler registrieren
                simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;
                UpdateStatus("SimConnect: Event-Handler registriert", System.Drawing.Color.DarkGreen);

                // Benutzer informieren
                UpdateStatus("Verbunden: Warte auf SimConnect-Events...", System.Drawing.Color.DarkGreen);
            }
            catch (COMException)
            {
                // MSFS läuft nicht: Panel zeigt Hinweis und schließt sich sauber nach Beenden des MessageBox-Dialogs
                MessageBox.Show("MSFS nicht gestartet. Bitte zuerst MSFS starten, dann Panel öffnen.", "MSFS nicht gefunden", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"{DateTime.Now:O} [Status] MSFS nicht gestartet. Bitte zuerst MSFS starten, dann Panel öffnen.");
                // Close the form after the message box — ensure Close runs on the UI thread after constructor completes
                this.BeginInvoke((Action)(() => { this.Close(); }));
                return;
            }
            catch (Exception ex)
            {
                // Alle anderen Fehler sichtbar machen
                UpdateStatus("Fehler beim Verbinden: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        // txtNav1 handlers
        private void TxtNav1_Enter(object? sender, EventArgs e)
        {
            if (sender is TextBox tb && tb.Name == "txtNav1")
            {
                isEditingNav1 = true;
                Debug.WriteLine($"{DateTime.Now:O} [Debug] Benutzer beginnt Editieren txtNav1");
            }
        }

        private void TxtNav1_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (sender is TextBox tb)
                    {
                        isEditingNav1 = false;
                        Debug.WriteLine($"{DateTime.Now:O} [Debug] Benutzer beendet Editieren txtNav1 mit Enter");

                        // Eingabewert parsen und NAV1_RADIO_SET senden
                        try
                        {
                            var sc = simconnect;
                            if (sc == null)
                            {
                                UpdateStatus("SimConnect nicht verbunden", System.Drawing.Color.Orange);
                                return;
                            }

                            string text = tb.Text?.Trim() ?? string.Empty;
                            // Erwartete Formate wie "110.30". In das von FSTramp/FSUIPC7 verwendete Param-Format konvertieren:
                            // High-Byte = MHz - 94, Low-Byte = BCD der Nachkommastellen (Zehner/Einheiten)
                            // Beispiel: 110.30 -> high = 110-94 = 16 (0x10), low = BCD(30) = 0x30 -> param = 0x1030 = 4144
                            // Akzeptiert Eingaben mit oder ohne Punkt; bei einem Format ohne Punkt (z. B. 11030) werden die
                            //   letzten zwei Ziffern als Nachkommastellen interpretiert.
                            string cleaned = string.Empty;
                            foreach (char c in text)
                                if (char.IsDigit(c) || c == '.') cleaned += c;

                            int mhz = 0;
                            int frac = 0;
                            if (cleaned.Contains('.'))
                            {
                                var parts = cleaned.Split('.', 2);
                                _ = int.TryParse(parts[0], out mhz);
                                // Bis zu zwei Ziffern des Nachkommateils verwenden
                                string fracPart = parts.Length > 1 ? parts[1] : "";
                                if (fracPart.Length == 1) fracPart += "0"; // e.g. .3 -> 30
                                if (fracPart.Length > 2) fracPart = fracPart[..2];
                                _ = int.TryParse(fracPart, out frac);
                            }
                            else
                            {
                                // Kein Punkt: die letzten zwei Ziffern sind die Nachkommastellen
                                string digits = string.Empty;
                                foreach (char c in cleaned)
                                    if (char.IsDigit(c)) digits += c;
                                if (digits.Length <= 2)
                                {
                                    // Ungültiges Format
                                    UpdateStatus("Ungültiger NAV1-Wert", System.Drawing.Color.Orange);
                                    return;
                                }
                                int all = int.Parse(digits);
                                frac = all % 100;
                                mhz = all / 100;
                            }

                            // Param berechnen
                            int high = mhz - 94;
                            if (high < 0) high = 0;
                            if (high > 255) high = 255;
                            int fracTens = frac / 10;
                            int fracUnits = frac % 10;
                            int low = (fracTens << 4) | (fracUnits & 0xF);
                            int param = (high << 8) | (low & 0xFF);

                            Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event NAV1_RADIO_SET param=0x{param:X4} ({param}) for input '{text}'");
                            sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.NAV1_RADIO_SET, (uint)param, NOTIFICATION_GROUPS.GROUP0, 0u);
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus("Fehler beim Senden des NAV1-Werts: " + ex.Message, System.Drawing.Color.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("TxtNav1_KeyDown: " + ex.Message, System.Drawing.Color.Orange);
            }
        }

        // Verbindungen hergestellen
        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            // Wenn sich SimConnect hier meldet, sind wir Verbunden
            UpdateStatus("Verbunden mit MSFS 2024", System.Drawing.Color.DarkGreen);

            // --------------- Autopilot-Status ----------------
            // Initialer Autopilot-Zustand unbekannt -> auf aus setzen
            UpdateAutopilotIndicator(false);
            Debug.WriteLine($"{DateTime.Now:O} [Status] Set btnAutopilot initial false");
            try
            {
                // DataDefinition für AUTOPILOT MASTER registrieren
                Debug.Assert(simconnect != null, "SimConnect ist beim OnRecvOpen gesetzt");
                {
                    // Register autopilot status mit "AUTOPILOT MASTER"
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.AP_STATE,
                            SimVarNames.AUTOPILOT_MASTER,
                            "Bool",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INT32,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei AddToDataDefinition: " + ex.Message, System.Drawing.Color.Red);
                    }

                    // Autopilot-Status Managed struct-Layout registrieren
                    try
                    {
                        simconnect!.RegisterDataDefineStruct<int>(DEFINITIONS.AP_STATE);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei RegisterDataDefineStruct: " + ex.Message, System.Drawing.Color.Red);
                    }

                    // Autopilot-Änderungen abonnieren
                    try
                    {
                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_AP_STATE_CHANGED,
                            DEFINITIONS.AP_STATE,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                                        Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                                        Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei RequestDataOnSimObject(CHANGED): " + ex.Message, System.Drawing.Color.Red);
                    }

                    // Client-Ereignisse für die Autopilot-Steuerung auf Simulator-Ereignisse abbilden
                    try
                    {
                        // Map client events to sim events
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_MASTER, SimEventNames.AP_MASTER);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_PANEL_ALTITUDE_SET, SimEventNames.AP_PANEL_ALTITUDE_SET);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_PANEL_VS_SET, SimEventNames.AP_PANEL_VS_SET);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_VS_VAR_SET_ENGLISH, SimEventNames.AP_VS_VAR_SET_ENGLISH);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_ALT_VAR_SET_ENGLISH, SimEventNames.AP_ALT_VAR_SET_ENGLISH);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_PANEL_HEADING_HOLD, SimEventNames.AP_PANEL_HEADING_HOLD);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_APR_HOLD, SimEventNames.AP_APR_HOLD);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.AP_PANEL_HEADING_BUG_SET, SimEventNames.HEADING_BUG_SET);
                        simconnect!.MapClientEventToSimEvent((Enum)CLIENT_EVENTS.NAV1_RADIO_SET, SimEventNames.NAV1_RADIO_SET);

                        // Register the client events with the notification group
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, EventID, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.AP_PANEL_ALTITUDE_SET, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.AP_PANEL_VS_SET, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.AP_VS_VAR_SET_ENGLISH, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.AP_ALT_VAR_SET_ENGLISH, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.AP_PANEL_HEADING_HOLD, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.NAV1_RADIO_SET, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.AP_APR_HOLD, true);
                        simconnect!.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, CLIENT_EVENTS.AP_PANEL_HEADING_BUG_SET, true);
                        simconnect!.SetNotificationGroupPriority(NOTIFICATION_GROUPS.GROUP0, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Mappen von Client-Events: " + ex.Message, System.Drawing.Color.Red);
                    }

                    // DataDefinition für Flughöhe (PLANE ALTITUDE) registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.ALTITUDE,
                            SimVarNames.PLANE_ALTITUDE,
                            "Feet",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        simconnect!.RegisterDataDefineStruct<AltitudeStruct>(DEFINITIONS.ALTITUDE);

                        // Altitude-Änderungen abonnieren (CHANGED -> liefert nur Änderungen)
                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_ALTITUDE,
                            DEFINITIONS.ALTITUDE,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei Altitude-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für Autopilot-Zielhöhe (Soll) registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.ALT_TARGET,
                            SimVarNames.AUTOPILOT_ALTITUDE_LOCK_VAR,
                            "Feet",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        // Register as plain double (no custom struct)
                        simconnect!.RegisterDataDefineStruct<double>(DEFINITIONS.ALT_TARGET);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_ALT_TARGET,
                            DEFINITIONS.ALT_TARGET,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei AltTarget-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für AUTOPILOT ALTITUDE LOCK (ON/OFF) registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.ALT_LOCK,
                            SimVarNames.AUTOPILOT_ALTITUDE_LOCK,
                            "Bool",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INT32,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        // Bool wird als INT32 geliefert -> als int registrieren
                        simconnect!.RegisterDataDefineStruct<int>(DEFINITIONS.ALT_LOCK);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_ALT_LOCK,
                            DEFINITIONS.ALT_LOCK,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei AltLock-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für Vertical Speed (Sollwert) registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.VERTICAL_SPEED,
                            SimVarNames.VERTICAL_SPEED,
                            "Feet per minute",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        simconnect!.RegisterDataDefineStruct<double>(DEFINITIONS.VERTICAL_SPEED);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_VERTICAL_SPEED,
                            DEFINITIONS.VERTICAL_SPEED,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei VerticalSpeed-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für Heading registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.HEADING,
                            SimVarNames.PLANE_HEADING_DEGREES_TRUE,
                            "Degrees",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        simconnect!.RegisterDataDefineStruct<double>(DEFINITIONS.HEADING);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_HEADING,
                            DEFINITIONS.HEADING,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei Heading-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für Vertical Speed Soll registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.VERT_HOLD,
                            SimVarNames.AUTOPILOT_VERTICAL_HOLD_VAR,
                            "Feet per minute",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        simconnect!.RegisterDataDefineStruct<double>(DEFINITIONS.VERT_HOLD);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_VERT_HOLD,
                            DEFINITIONS.VERT_HOLD,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei VertHold-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für AUTOPILOT HEADING LOCK (ON/OFF) registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.HEADING_LOCK,
                            SimVarNames.AUTOPILOT_HEADING_LOCK,
                            "Bool",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INT32,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        // Bool wird als INT32 geliefert -> als int registrieren
                        simconnect!.RegisterDataDefineStruct<int>(DEFINITIONS.HEADING_LOCK);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_HEADING_LOCK,
                            DEFINITIONS.HEADING_LOCK,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei HeadingLock-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für AUTOPILOT HEADING LOCK DIR (Sollwert) registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.HEADING_TARGET,
                            SimVarNames.AUTOPILOT_HEADING_LOCK_DIR,
                            "Degrees",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        simconnect!.RegisterDataDefineStruct<double>(DEFINITIONS.HEADING_TARGET);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_HEADING_TARGET,
                            DEFINITIONS.HEADING_TARGET,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei HeadingTarget-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für AUTOPILOT APPROACH ACTIVE (ON/OFF) registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.APPROACH_ACTIVE,
                            SimVarNames.AUTOPILOT_APPROACH_ACTIVE,
                            "Bool",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INT32,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        // Bool wird als INT32 geliefert -> als int registrieren
                        simconnect!.RegisterDataDefineStruct<int>(DEFINITIONS.APPROACH_ACTIVE);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_APPROACH,
                            DEFINITIONS.APPROACH_ACTIVE,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei Approach-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für NAV ACTIVE FREQUENCY:1 registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.NAV1_FREQUENCY,
                            SimVarNames.NAV_ACTIVE_FREQUENCY_1,
                            "MHz",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        simconnect!.RegisterDataDefineStruct<double>(DEFINITIONS.NAV1_FREQUENCY);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_NAV1,
                            DEFINITIONS.NAV1_FREQUENCY,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei NAV1-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                    // DataDefinition für AIRSPEED_INDICATED registrieren und CHANGED-Subscription
                    try
                    {
                        simconnect!.AddToDataDefinition(
                            DEFINITIONS.AIRSPEED,
                            SimVarNames.AIRSPEED_INDICATED,
                            "Knots",
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.FLOAT64,
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED);

                        simconnect!.RegisterDataDefineStruct<double>(DEFINITIONS.AIRSPEED);

                        simconnect!.RequestDataOnSimObject(
                            DATA_REQUESTS.REQUEST_AIRSPEED,
                            DEFINITIONS.AIRSPEED,
                            SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                            Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                            0u,
                            0u,
                            SimConnect.SIMCONNECT_UNUSED);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei Airspeed-Definition/Request: " + ex.Message, System.Drawing.Color.Orange);
                    }

                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Fehler bei DataDefinition/Request: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        // Fallback-Handler, falls Daten als SIMOBJECT_DATA (nicht BYTYPE) geliefert werden
        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            try
            {
                // dwData kann hier ebenfalls ein object[] sein
                // In object casten, um Pattern-Matching gegen verschiedene Laufzeittypen zu ermöglichen
                object? field = (object?)data.dwData;

                // Ist-Höhe (PLANE ALTITUDE) -> lblAltIst
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_ALTITUDE)
                {
                    try
                    {
                        double altFt;

                        // Direktes Struct
                        if (field is AltitudeStruct alt)
                        {
                            altFt = alt.altitude;
                        }
                        // Manchmal liefert der Wrapper ein Array mit einem Element
                        else if (field is object[] arrAlt && arrAlt.Length > 0)
                        {
                            object v = arrAlt[0] ?? 0.0;
                            if (v is AltitudeStruct alt2)
                            {
                                altFt = alt2.altitude;
                            }
                            else if (v is double dv)
                            {
                                altFt = dv;
                            }
                            else if (v is float fv)
                            {
                                altFt = fv;
                            }
                            else if (v is IConvertible)
                            {
                                altFt = Convert.ToDouble(v);
                            }
                            else
                            {
                                UpdateStatus("Unbekannter Altitude-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange);
                                return;
                            }
                        }
                        // Falls dwData direkt ein numerischer Wert ist
                        else if (field is double dv2)
                        {
                            altFt = dv2;
                        }
                        else if (field is float fv2)
                        {
                            altFt = fv2;
                        }
                        else
                        {
                            UpdateStatus("Altitude-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange);
                            return;
                        }

                        // UI-Update thread-sicher — aktualisiere AltitudeTape (skalenorientierte Anzeige)
                        int altInt = (int)Math.Round(altFt);
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(() => altitudeTape.Value = altInt);
                        }
                        else
                        {
                            altitudeTape.Value = altInt;
                        }

                        // Nachdem die Altitude erfolgreich verarbeitet wurde, abbrechen,
                        // damit der nachfolgende allgemeine Array‑Fallback nicht ausgeführt wird
                        // (vermeidet Convert.ToInt32 auf einem AltitudeStruct).
                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der Altitude-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // AUTOPILOT ALTITUDE LOCK VAR (Sollhöhe) -> txtAltSoll (nur wenn nicht editierend)
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_ALT_TARGET)
                {
                    try
                    {
                        double val;

                        if (field is object[] arrAltT && arrAltT.Length > 0)
                        {
                            object v = arrAltT[0] ?? 0.0;
                            if (v is double dv) val = dv;
                            else if (v is float fv) val = fv;
                            else if (v is IConvertible) val = Convert.ToDouble(v);
                            else { UpdateStatus("Unbekannter AltTarget-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is double dv2) val = dv2;
                        else if (field is float fv2) val = fv2;
                        else { UpdateStatus("AltTarget-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        int intVal = (int)Math.Round(val);
                        lastAltTargetInt = intVal;

                        // Nur aktualisieren, wenn der Benutzer nicht gerade editiert
                        if (!isEditingAltSoll)
                        {
                            if (this.InvokeRequired) this.BeginInvoke(() => txtAltSoll.Text = intVal.ToString());
                            else txtAltSoll.Text = intVal.ToString();
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der AltTarget-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // Vertikale Ist-Geschwindigkeit (VERTICAL SPEED) -> ctrVS
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_VERTICAL_SPEED)
                {
                    try
                    {
                        double vsVal;
                        if (field is object[] arrVs && arrVs.Length > 0)
                        {
                            object v = arrVs[0] ?? 0.0;
                            if (v is double dv)
                            {
                                vsVal = dv;
                            }
                            else if (v is float fv)
                            {
                                vsVal = fv;
                            }
                            else if (v is IConvertible)
                            {
                                vsVal = Convert.ToDouble(v);
                            }
                            else
                            {
                                UpdateStatus("Unbekannter VerticalSpeed-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange);
                                return;
                            }
                        }
                        else if (field is double dv2)
                        {
                            vsVal = dv2;
                        }
                        else if (field is float fv2)
                        {
                            vsVal = fv2;
                        }
                        else
                        {
                            UpdateStatus("VerticalSpeed-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange);
                            return;
                        }

                        int vsInt = (int)Math.Round(vsVal);
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(() => { ctrVS.Value = vsInt; ctrVS.Unit = "ft/min"; });
                        }
                        else
                        {
                            ctrVS.Value = vsInt;
                            ctrVS.Unit = "ft/min";
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der VerticalSpeed-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // Heading (PLANE HEADING DEGREES TRUE) -> lblHDGIst
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_HEADING)
                {
                    try
                    {
                        double headingVal;
                        if (field is object[] arrH && arrH.Length > 0)
                        {
                            object v = arrH[0] ?? 0.0;
                            if (v is double dv) headingVal = dv;
                            else if (v is float fv) headingVal = fv;
                            else if (v is IConvertible) headingVal = Convert.ToDouble(v);
                            else { UpdateStatus("Unbekannter Heading-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is double dv2) headingVal = dv2;
                        else if (field is float fv2) headingVal = fv2;
                        else { UpdateStatus("Heading-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        int headingInt = (int)Math.Round(headingVal) % 360;
                        if (headingInt < 0) headingInt += 360;
                        // Aktualisiere das HeadingTape-Control (skalenorientierte Anzeige).
                        if (this.InvokeRequired) this.BeginInvoke(() => headingTape.Value = headingInt);
                        else headingTape.Value = headingInt;

                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der Heading-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // Vertikale Soll-Geschwindigkeit (VERTICAL HOLD VAR) -> txtVS (nur wenn nicht editierend)
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_VERT_HOLD)
                {
                    try
                    {
                        double val;
                        if (field is object[] arrHold && arrHold.Length > 0)
                        {
                            object v = arrHold[0] ?? 0.0;
                            if (v is double dv) val = dv;
                            else if (v is float fv) val = fv;
                            else if (v is IConvertible) val = Convert.ToDouble(v);
                            else { UpdateStatus("Unbekannter VertHold-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is double dv2) val = dv2;
                        else if (field is float fv2) val = fv2;
                        else { UpdateStatus("VertHold-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        int intVal = (int)Math.Round(val);
                        lastVsSollInt = intVal;

                        if (!isEditingVS)
                        {
                            if (this.InvokeRequired) this.BeginInvoke(() => txtVS.Text = intVal.ToString());
                            else txtVS.Text = intVal.ToString();
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der VertHold-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // AUTOPILOT ALT ON/OFF -> btnALT
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_ALT_LOCK)
                {
                    try
                    {
                        int altLock;
                        if (field is object[] arrLock && arrLock.Length > 0)
                        {
                            object v = arrLock[0] ?? 0;
                            if (v is IConvertible) altLock = Convert.ToInt32(v);
                            else { UpdateStatus("Unbekannter AltLock-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is IConvertible conv) altLock = Convert.ToInt32(conv);
                        else { UpdateStatus("AltLock-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        bool altOn = altLock != 0;
                        UpdateAltIndicator(altOn);
                        // lokalen Status pflegen
                        isAltOn = altOn;
                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der AltLock-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // AUTOPILOT HEADING LOCK (ON/OFF) -> btnHDG
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_HEADING_LOCK)
                {
                    try
                    {
                        int hdgLock;
                        if (field is object[] arrLock && arrLock.Length > 0)
                        {
                            object v = arrLock[0] ?? 0;
                            if (v is IConvertible) hdgLock = Convert.ToInt32(v);
                            else { UpdateStatus("Unbekannter HeadingLock-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is IConvertible conv) hdgLock = Convert.ToInt32(conv);
                        else { UpdateStatus("HeadingLock-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        bool hdgOn = hdgLock != 0;
                        UpdateHdgIndicator(hdgOn);
                        // lokalen Status pflegen
                        isHdgOn = hdgOn;
                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der HeadingLock-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // AUTOPILOT APPROACH ACTIVE (ON/OFF) -> btnAPPR
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_APPROACH)
                {
                    try
                    {
                        int aprVal;
                        if (field is object[] arrLock && arrLock.Length > 0)
                        {
                            object v = arrLock[0] ?? 0;
                            if (v is IConvertible) aprVal = Convert.ToInt32(v);
                            else { UpdateStatus("Unbekannter Approach-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is IConvertible conv) aprVal = Convert.ToInt32(conv);
                        else { UpdateStatus("Approach-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        bool aprOn = aprVal != 0;
                        UpdateButtonIndicator(btnAPPR, aprOn);
                        // lokalen Status pflegen
                        isAprOn = aprOn;
                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der Approach-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // NAV1 ACTIVE FREQUENCY -> txtNav1
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_NAV1)
                {
                    try
                    {
                        double freqMHz;
                        if (field is object[] arrLock && arrLock.Length > 0)
                        {
                            object v = arrLock[0] ?? 0.0;
                            if (v is IConvertible) freqMHz = Convert.ToDouble(v);
                            else { UpdateStatus("Unbekannter NAV1-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is IConvertible conv) freqMHz = Convert.ToDouble(conv);
                        else { UpdateStatus("NAV1-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        // Formatierung: z.B. 110.300 -> "110.30"
                        string txt = freqMHz.ToString("F2");
                        // Nur aktualisieren, wenn der Benutzer nicht gerade in txtNav1 editieret
                        if (!isEditingNav1)
                        {
                            if (this.InvokeRequired) this.BeginInvoke(() => txtNav1.Text = txt);
                            else txtNav1.Text = txt;
                        }
                        else
                        {
                            Debug.WriteLine($"{DateTime.Now:O} [Debug] txtNav1: Update unterdrückt, Benutzer editiert");
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der NAV1-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // AIRSPEED_INDICATED -> label1
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_AIRSPEED)
                {
                    try
                    {
                        double tas;
                        if (field is object[] arrAirspeed && arrAirspeed.Length > 0)
                        {
                            object v = arrAirspeed[0] ?? 0.0;
                            if (v is IConvertible) tas = Convert.ToDouble(v);
                            else { UpdateStatus("Unbekannter Airspeed-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is IConvertible conv) tas = Convert.ToDouble(conv);
                        else { UpdateStatus("Airspeed-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        // Aktualisiere AirspeedTape value (thread-sicher). Cache Referenz und benutze double direkt.
                        double airspeedVal = Math.Round(tas, 1);
                        var tape = airspeedTape;
                        if (tape == null) return;
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke((Action)(() => tape.Value = airspeedVal));
                        }
                        else
                        {
                            tape.Value = airspeedVal;
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der Airspeed-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

                // Autopilot -Status (AUTOPILOT MASTER) -> btnAutopilot
                if (field is object[] arrMaster && arrMaster.Length > 0)
                {
                    // dwData kann hier auch ein boxed numeric value sein (Wrapper liefert manchmal Arrays)
                    try
                    {
                        // Wenn das erste Element ein numerischer Wert ist, konvertiere zu int
                        object first = arrMaster[0];
                        if (first is IConvertible convFirst)
                        {
                            try
                            {
                                int ap_master = Convert.ToInt32(convFirst);
                                bool isOn = ap_master != 0;
                                UpdateAutopilotIndicator(isOn);
                                Debug.WriteLine($"{DateTime.Now:O} [Status] Set btnAutopilot {isOn}");
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus("OnRecvSimobjectData: Fehler bei numerischer Konvertierung - " + ex.Message, System.Drawing.Color.Red);
                            }
                        }
                        else
                        {
                            // Unerwarteter Datentyp (z.B. AltitudeStruct) — protokollieren und abbrechen
                            UpdateStatus("OnRecvSimobjectData: unerwarteter Datentyp im Array: " + (first?.GetType().FullName ?? "null"), System.Drawing.Color.Orange);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Konvertierungsfehler weiterhin explizit protokollieren
                        UpdateStatus("OnRecvSimobjectData: Konvertierungsfehler - " + ex.Message, System.Drawing.Color.Red);
                    }
                }

                // AUTOPILOT HEADING LOCK DIR (Sollwert) -> txtHDGSoll (nur wenn nicht editierend)
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_HEADING_TARGET)
                {
                    try
                    {
                        double val;
                        if (field is object[] arrHold && arrHold.Length > 0)
                        {
                            object v = arrHold[0] ?? 0.0;
                            if (v is double dv) val = dv;
                            else if (v is float fv) val = fv;
                            else if (v is IConvertible) val = Convert.ToDouble(v);
                            else { UpdateStatus("Unbekannter HeadingTarget-Datentyp: " + (v?.GetType().FullName ?? "null"), System.Drawing.Color.Orange); return; }
                        }
                        else if (field is double dv2) val = dv2;
                        else if (field is float fv2) val = fv2;
                        else { UpdateStatus("HeadingTarget-Antwort: unbekanntes Datenformat.", System.Drawing.Color.Orange); return; }

                        int intVal = (int)Math.Round(val) % 360;
                        if (intVal < 0) intVal += 360;
                        lastHdgTargetInt = intVal;

                        if (!isEditingHdgSoll)
                        {
                            if (this.InvokeRequired) this.BeginInvoke(() => txtHDGSoll.Text = intVal.ToString());
                            else txtHDGSoll.Text = intVal.ToString();
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Verarbeiten der HeadingTarget-Antwort: " + ex.Message, System.Drawing.Color.Orange);
                    }
                }

            }
            catch (Exception ex)
            {
                UpdateStatus($"OnRecvSimobjectData: Exception - {ex.Message}", System.Drawing.Color.Red);
            }
        }

        // Klick-Handler: schaltet den Approach-Hold per SimConnect-Client-Event
        private void BtnAPPR_Click(object sender, EventArgs e)
        {
            try
            {
                var sc = simconnect;
                if (sc == null)
                {
                    UpdateStatus("SimConnect nicht verbunden", System.Drawing.Color.Orange);
                    return;
                }

                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_APR_HOLD toggle");
                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_APR_HOLD, 0u, NOTIFICATION_GROUPS.GROUP0, 0u);
            }
            catch (Exception ex)
            {
                UpdateStatus("Fehler beim Senden des AP_APR_HOLD-Events: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        // Gemeinsame Hilfsfunktion zum Aktualisieren eines Toggle-Buttons (Blau = ON)
        private static void UpdateButtonIndicator(Button btn, bool isOn)
        {
            if (btn is null) return;
            if (btn.InvokeRequired)
            {
                btn.Invoke(new Action(() => UpdateButtonIndicator(btn, isOn)));
                return;
            }

            if (isOn)
            {
                Debug.WriteLine($"{DateTime.Now:O} [Status] {btn.Name}: Set On");
                btn.BackColor = System.Drawing.Color.DodgerBlue;
                btn.ForeColor = System.Drawing.Color.White;
            }
            else
            {
                Debug.WriteLine($"{DateTime.Now:O} [Status] {btn.Name}: Set OFF");
                btn.BackColor = System.Drawing.SystemColors.Control;
                btn.ForeColor = System.Drawing.Color.Black;
            }
        }

        // Aktualisiert die Anzeige des ALT-Buttons (Blau = ON)
        private void UpdateAltIndicator(bool isOn) => UpdateButtonIndicator(btnALT, isOn);

        // Aktualisiert die Anzeige des Autopilot-Buttons (Blau = ON)
        private void UpdateAutopilotIndicator(bool isOn) => UpdateButtonIndicator(btnAutopilot, isOn);

        // Aktualisiert die Anzeige des HDG-Buttons (Blau = ON)
        private void UpdateHdgIndicator(bool isOn) => UpdateButtonIndicator(btnHDG, isOn);

        // Benutzer-Interaktion: txtAltSoll
        private void TxtAltSoll_Enter(object? sender, EventArgs e)
        {
            if (sender is TextBox tb && tb.Name == "txtVS")
            {
                isEditingVS = true;
                Debug.WriteLine($"{DateTime.Now:O} [Debug] Benutzer beginnt Editieren txtVS");
            }
            else
            {
                isEditingAltSoll = true;
                Debug.WriteLine($"{DateTime.Now:O} [Debug] Benutzer beginnt Editieren txtAltSoll");
            }
        }

        private async void TxtAltSoll_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    // Sende den Sollwert an den Simulator; unterscheide nach Sender
                    if (sender is TextBox tb)
                    {
                        if (tb.Name == "txtAltSoll")
                        {
                            if (!int.TryParse(tb.Text.Trim(), out int newAlt))
                            {
                                UpdateStatus("Ungültiger Sollwert", System.Drawing.Color.Orange);
                                return;
                            }
                            double altToSend = newAlt; // Fuß
                            try
                            {
                                var sc = simconnect;
                                if (sc == null)
                                {
                                    // Sicherheitscheck: Abbrechen, wenn SimConnect nicht verfügbar
                                    isEditingAltSoll = false;
                                    isEditingVS = false;
                                    return;
                                }

                                // Sende Event Sequenz entsprechend:
                                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event ALT Sollwert=" + (uint)newAlt);
                                // 1) AP_PANEL_ALTITUDE_SET = 0
                                // 2) AP_PANEL_VS_SET = 1
                                // 3) AP_ALT_VAR_SET_ENGLISH = <alt in ft> (z.B. 5000)
                                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_PANEL_ALTITUDE_SET, 0u, NOTIFICATION_GROUPS.GROUP0, 0u);
                                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_PANEL_ALTITUDE_SET = 0");
                                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_PANEL_VS_SET, 1u, NOTIFICATION_GROUPS.GROUP0, 0u);
                                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_PANEL_VS_SET = 1");
                                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_ALT_VAR_SET_ENGLISH, (uint)newAlt, NOTIFICATION_GROUPS.GROUP0, 0u);
                                Debug.WriteLine($"{DateTime.Now:O} [Status] AP_ALT_VAR_SET_ENGLISH = " + (uint)newAlt);
                                await Task.Delay(200);

                                // Aktuellen Istwert holen
                                int currentAlt = 0;
                                try
                                {
                                    var lbl = lblAltIst.Text?.Trim() ?? string.Empty;
                                    if (lbl.EndsWith("ft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Entferne die Einheit "ft" effizient
                                        lbl = lbl[..^2].Trim();
                                    }
                                    // Verwende das Rückgabeergebnis bewusst, damit CA1806 nicht ausgelöst wird
                                    _ = int.TryParse(lbl, out currentAlt);
                                }
                                catch
                                {
                                    currentAlt = 0;
                                }

                                // Bestimme Vorzeichen für VS: +500 wenn neuer Soll > Ist, -500 wenn neuer Soll < Ist
                                int vsToSend = 0;
                                if (newAlt < currentAlt)
                                    vsToSend = -500;
                                else
                                    vsToSend = 500;

                                // TransmitClientEvent erwartet ein uint; negative Werte werden per unchecked in zwei-komplement gebettet
                                uint vsParam = unchecked((uint)vsToSend);
                                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_VS_VAR_SET_ENGLISH, vsParam, NOTIFICATION_GROUPS.GROUP0, 0u);
                                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_VS_VAR_SET_ENGLISH = {vsToSend}");

                                // Aktualisiere lokalen letzten Sollwert
                                lastAltTargetInt = newAlt;
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus("Fehler beim Senden ALT Sollwert: " + ex.Message, System.Drawing.Color.Red);
                            }
                            finally { isEditingAltSoll = false; }
                        }
                        else if (tb.Name == "txtVS")
                        {
                            if (!int.TryParse(tb.Text.Trim(), out int newVs))
                            {
                                UpdateStatus("Ungültiger VS-Sollwert", System.Drawing.Color.Orange);
                                return;
                            }
                            double vsToSend = newVs; // Fuß pro Minute
                            try
                            {
                                var sc = simconnect;
                                if (sc == null)
                                {
                                    // Sicherheitscheck: Abbrechen, wenn SimConnect nicht verfügbar
                                    isEditingAltSoll = false;
                                    isEditingVS = false;
                                    return;
                                }
                                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event VS Sollwert=" + (uint)newVs);
                                // Transmit expects an unsigned integer parameter
                                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_VS_VAR_SET_ENGLISH, (uint)newVs, NOTIFICATION_GROUPS.GROUP0, 0u);
                                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_VS_VAR_SET_ENGLISH = {newVs}");
                                lastVsSollInt = newVs;
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus("Fehler beim Senden VS-Sollwert: " + ex.Message, System.Drawing.Color.Red);
                                Debug.WriteLine($"{DateTime.Now:O} [Debug] Fehler SetDataOnSimObject VERT_HOLD: {ex}");
                            }
                            finally { isEditingVS = false; }
                        }
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    // Abbrechen, revert auf zuletzt bekannten Wert
                    if (sender is TextBox tb2)
                    {
                        if (tb2.Name == "txtAltSoll") { txtAltSoll.Text = lastAltTargetInt.ToString(); isEditingAltSoll = false; }
                        else if (tb2.Name == "txtVS") { txtVS.Text = lastVsSollInt.ToString(); isEditingVS = false; }
                    }
                    e.SuppressKeyPress = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{DateTime.Now:O} [Debug] TxtAltSoll_KeyDown Exception: {ex}");
            }
        }

#pragma warning restore CS0649

        // Altitude struct (für PLANE ALTITUDE)
#pragma warning disable CS0649
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct AltitudeStruct
        {
            public double altitude; // in Fuß
        }
#pragma warning restore CS0649

        // Klick-Handler: schaltet den Autopiloten per SimConnect-Client-Event um
        private void BtnAutopilot_Click(object sender, EventArgs e)
        {
            try
            {
                // Sende das zuvor gemappte Client-Event an den Simulator (Toggle)
                try
                {
                    var sc = simconnect;
                    if (sc == null)
                    {
                        UpdateStatus("SimConnect nicht verbunden", System.Drawing.Color.Orange);
                        return;
                    }

                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_MASTER Autopilot toggle");
                    // 1) AP_MASTER = 0 (Autopilot aus)
                    sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_MASTER, 0u, NOTIFICATION_GROUPS.GROUP0, 0u);
                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_MASTER = 0");
                }
                catch (Exception ex)
                {
                    UpdateStatus("Fehler beim Senden des Autopilot-Events: " + ex.Message, System.Drawing.Color.Red);
                }
                return;
            }
            catch { }
        }

        // Klick-Handler: schaltet Heading Hold per SimConnect-Client-Event
        private void BtnHDG_Click(object sender, EventArgs e)
        {
            try
            {
                var sc = simconnect;
                if (sc == null)
                {
                    UpdateStatus("SimConnect nicht verbunden", System.Drawing.Color.Orange);
                    return;
                }

                // Senden: AP_PANEL_HEADING_HOLD = 0
                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event HDG Autopilot toggle");
                sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_PANEL_HEADING_HOLD, 0u, NOTIFICATION_GROUPS.GROUP0, 0u);
                Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_PANEL_HEADING_HOLD = 0");
            }
            catch (Exception ex)
            {
                UpdateStatus("Fehler beim Senden des HDG-Events: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        // Benutzer-Interaktion: txtHDGSoll
        private void TxtHdgSoll_Enter(object? sender, EventArgs e)
        {
            if (sender is TextBox tb && tb.Name == "txtHDGSoll")
            {
                isEditingHdgSoll = true;
                Debug.WriteLine($"{DateTime.Now:O} [Debug] Benutzer beginnt Editieren txtHDGSoll");
            }
        }

        private void TxtHdgSoll_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (sender is TextBox tb)
                    {
                        // Wert parsen und als HEADING_BUG_SET an den Simulator senden
                        isEditingHdgSoll = false;
                        Debug.WriteLine($"{DateTime.Now:O} [Debug] Benutzer beendet Editieren txtHDGSoll mit Enter");
                        try
                        {
                            var sc = simconnect;
                            if (sc == null)
                            {
                                UpdateStatus("SimConnect nicht verbunden", System.Drawing.Color.Orange);
                                return;
                            }

                            string text = tb.Text?.Trim() ?? string.Empty;
                            if (text.EndsWith('°')) text = text[..^1].Trim();
                            if (!int.TryParse(text, out int hdgValue))
                            {
                                UpdateStatus("Ungültiger Heading-Wert", System.Drawing.Color.Orange);
                                return;
                            }

                            // SimConnect erwartet das Event-Argument als uint (z.B. Grad)
                            uint param = unchecked((uint)hdgValue);
                            Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event HEADING_BUG_SET = {hdgValue}");
                            sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_PANEL_HEADING_BUG_SET, param, NOTIFICATION_GROUPS.GROUP0, 0u);
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus("Fehler beim Senden des Heading-Werts: " + ex.Message, System.Drawing.Color.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("TxtHdgSoll_KeyDown: " + ex.Message, System.Drawing.Color.Orange);
            }
        }

        // BtnALT-Handler: schaltet ALT Regelung ein/aus
        private async void BtnALT_Click(object sender, EventArgs e)
        {
            try
            {
                var sc = simconnect;
                if (sc == null)
                {
                    UpdateStatus("SimConnect nicht verbunden", System.Drawing.Color.Orange);
                    return;
                }

                if (isAltOn)
                {
                    // ALT Ausschalten
                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event ALT AUS");
                    sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_PANEL_ALTITUDE_SET, 0u, NOTIFICATION_GROUPS.GROUP0, 0u);
                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_PANEL_ALTITUDE_SET = 0");
                    sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_VS_VAR_SET_ENGLISH, 0u, NOTIFICATION_GROUPS.GROUP0, 0u);
                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_VS_VAR_SET_ENGLISH = 0");
                    isAltOn = false;
                    UpdateAltIndicator(false);
                }
                else
                {
                    // ALT Einschalten
                    int currentAlt = 0;
                    try
                    {
                        var lbl = lblAltIst.Text?.Trim() ?? string.Empty;
                        if (lbl.EndsWith("ft", StringComparison.OrdinalIgnoreCase)) lbl = lbl[..^2].Trim();
                        _ = int.TryParse(lbl, out currentAlt);
                    }
                    catch { currentAlt = 0; }

                    int vsValue = 0;
                    if (lastAltTargetInt > currentAlt) vsValue = 500;
                    else if (lastAltTargetInt < currentAlt) vsValue = -500;

                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event ALT EIN");
                    sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_PANEL_VS_SET, 1u, NOTIFICATION_GROUPS.GROUP0, 0u);
                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_PANEL_VS_SET = 1");
                    uint vsParam = unchecked((uint)vsValue);
                    sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)CLIENT_EVENTS.AP_VS_VAR_SET_ENGLISH, vsParam, NOTIFICATION_GROUPS.GROUP0, 0u);
                    Debug.WriteLine($"{DateTime.Now:O} [Status] Send Event AP_VS_VAR_SET_ENGLISH = {vsValue}");

                    isAltOn = true;
                    UpdateAltIndicator(true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Fehler beim Umschalten von ALT: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        // Event: MSFS wurde geschlossen
        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            // MSFS hat beendet — schließe das Panel
            Disconnect();
        }

        // Event: SimConnect wirft eine Ausnahme
        private void Simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            UpdateStatus($"SimConnectFehler: {data.dwException}", System.Drawing.Color.Red);
            try { Debug.WriteLine($"{DateTime.Now:O} [Status] [SimConnect Exception] dwException={data.dwException}, dwSendID={data.dwSendID}, dwIndex={data.dwIndex}, dwVersion={data.dwVersion}"); } catch { }
        }

        // Process empfängt SimConnect Daten
        private void Simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            try
            {
                if (data.dwRequestID == (uint)DATA_REQUESTS.REQUEST_AP_STATE_CHANGED)
                {
                    //  Im Managed Wrapper wird dwData als object[] übergeben, wenn RegisterDataDefineStruct verwendet wird.
                    if (data.dwData is object[] arr && arr.Length > 0)
                    {
                        Debug.WriteLine($"{DateTime.Now:O} [Debug] OnRecvSimobjectDataBytype REQUEST_AP_STATE_CHANGED dwRequestID={data.dwRequestID}, raw[0]={{0}}, type={{1}}", arr[0], arr[0]?.GetType().FullName);
                        try
                        {
                            int ap_master = Convert.ToInt32(arr[0]);
                            bool isOn = ap_master != 0;
                            UpdateAutopilotIndicator(isOn);
                            Debug.WriteLine($"{DateTime.Now:O} [Status] Set btnAutopilot {isOn}");
                        }
                        catch (Exception)
                        {
                            // Konvertierungsfehler für ap_master ignorieren
                        }

                        // Falls arr[1] vorhanden ist, versuchen wir, es als ALT_LOCK zu lesen
                        if (arr.Length > 1)
                        {
                            try
                            {
                                int altLock = Convert.ToInt32(arr[1]);
                                UpdateAltIndicator(altLock != 0);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Fehler beim Verarbeiten von AP-Daten: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        private void Disconnect()
        {
            simconnect?.Dispose();
            simconnect = null;
            UpdateAutopilotIndicator(false);
            Debug.WriteLine($"{DateTime.Now:O} [Status] Set btnAutopilot false");
            // Wenn die Verbindung verloren geht, Panel beenden (MSFS wurde beendet)
            Application.Exit();
        }

        // Thread-sicheres Aktualisieren der Statuszeile
        // Maximale Zeichenanzahl vor dem Abschneiden
        private const int MaxStatusLength = 60;
        private void UpdateStatus(string text, System.Drawing.Color color)
        {
            // Ignorieren von leeren Aktualisierungen, um ein versehentliches Löschen der Statuszeile zu vermeiden.
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(new Action(() => UpdateStatus(text, color)));
                return;
            }

            // Sicherstellen, dass die Statusleiste Element-Tooltips anzeigt (kann im Designer überschrieben werden)
            try { statusStrip1.ShowItemToolTips = true; } catch { }

            // Stellen Sie sicher, dass das Status-Label sichtbar ist und den verfügbaren Platz einnimmt.
            try
            {
                lblStatus.Visible = true;
                statusStrip1.BringToFront();
                // „Spring“ ermöglicht es dem Label, den verbleibenden Platz in der Statusleiste auszufüllen.
                lblStatus.Spring = true;
                lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
            catch { }

            // Wenn der Text die zulässige Länge überschreitet, wird der gekürzte Text angezeigt
            //   und der vollständige Text wird im Tooltip platziert
            if (!string.IsNullOrEmpty(text) && text.Length > MaxStatusLength)
            {
                string shortText = string.Concat(text.AsSpan(0, MaxStatusLength - 3), "...");
                lblStatus.Text = shortText;
                try { lblStatus.ToolTipText = text; } catch { }
            }
            else
            {
                lblStatus.Text = text ?? string.Empty;
                try { lblStatus.ToolTipText = string.Empty; } catch { }
            }

            // In das Ausgabe-Fenster von Visual Studio schreiben (Debug-Ausgabe)
            try { Debug.WriteLine($"{DateTime.Now:O} [Status] {text}"); } catch { }

            lblStatus.ForeColor = color;
        }

        // Verarbeitet die eingehenden Windows-Nachrichten von SimConnect
        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                try
                {
                    simconnect?.ReceiveMessage();
                }
                catch (Exception ex)
                {
                    UpdateStatus("Fehler ReceiveMessage: " + ex.Message, System.Drawing.Color.Red);
                    Disconnect();
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }


        // Beim Schließen des Fensters die Verbindung sauber trennen
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            simconnect?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
