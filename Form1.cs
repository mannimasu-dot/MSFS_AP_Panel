using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;

namespace MSFS_AP_Panel
{
    public partial class Form1 : Form
    {

        // Konstante für die Windows-Nachrichtenschleife (SimConnect-Kommunikation)
        private const int WM_USER_SIMCONNECT = 0x0402;

        private SimConnect? simconnect = null;
        private readonly System.Windows.Forms.Timer connectionTimer;
        private bool isConnected = false;
        // Flag, ob Client-Events/Notification-Groups bereits registriert wurden
        private bool eventsRegistered = false;

        // SimConnect-Daten-Definitionen / Requests
        private enum DEFINITIONS : uint { AP_STATE = 0 }
        // Nur noch CHANGED-Subscription verwenden; ONCE-Logik entfernt
        private enum DATA_REQUESTS : uint { REQUEST_AP_STATE_CHANGED = 0 }
        private enum CLIENT_EVENTS : uint { AP_MASTER = 0,  AP_MASTER_ON = 1, AP_MASTER_OFF = 2}
        private const CLIENT_EVENTS EventID = CLIENT_EVENTS.AP_MASTER;
        // Notification-Groups und Input-Gruppen definieren (für Mapping/Transmit)
        private enum NOTIFICATION_GROUPS : uint { GROUP0 = 0 }
        private enum INPUT_GROUPS : uint { INPUT0 = 0 }
        #pragma warning disable CS0649
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct ApStateStruct
        {
            public int ap_master; // BOOL als INT32
        }
        #pragma warning restore CS0649

        // Klick-Handler: schaltet den Autopiloten per SimConnect-Client-Event um
        private void BtnAutopilot_Click(object sender, EventArgs e)
        {
            try
            {
                if (simconnect == null)
                {
                    UpdateStatus("Nicht verbunden mit MSFS", System.Drawing.Color.OrangeRed);
                    return;
                }
                // Sende das zuvor gemappte Client-Event an den Simulator (Toggle)
                try
                {
                    // Client-Event an den Simulator senden (Notification-Group wird beim Verbinden registriert)
                    simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, (Enum)EventID, 1u, NOTIFICATION_GROUPS.GROUP0, 0u);
                    UpdateStatus("Autopilot-Event gesendet: ap_master", System.Drawing.Color.DarkGreen);
                }
                catch (Exception ex)
                {
                    UpdateStatus("Fehler beim Senden des Autopilot-Events: " + ex.Message, System.Drawing.Color.Red);
                }
                return;
            }
            catch { }
        }

        // Asynchrones Retry/Backoff-Wrapper für die synchrone ReRegisterDataDefinitionsAndRequests.
        // Führt das synchrone Re-Register im UI-Thread aus und aktualisiert ein Tooltip mit dem Fortschritt.
        private async Task ReRegisterDataDefinitionsAndRequestsAsync()
        {
            var backoffs = new[] { 200, 500, 1000 };

            for (int attempt = 0; attempt < backoffs.Length; attempt++)
            {
                try
                {
                    // Sicherstellen, dass SimConnect-Aufrufe im UI-Thread erfolgen, der das SimConnect-Handle erstellt hat
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => ReRegisterDataDefinitionsAndRequests()));
                    }
                    else
                    {
                        ReRegisterDataDefinitionsAndRequests();
                    }

                    // Erfolg im Tooltip anzeigen und zurückkehren
                    SetStatusTooltip($"Re-Register: Versuch {attempt + 1} abgeschlossen");
                    return;
                }
                catch (Exception ex)
                {
                    // Transientes Tooltip anzeigen und vor dem erneuten Versuch warten
                    SetStatusTooltip($"Re-Register Versuch {attempt + 1} fehlgeschlagen: {ex.Message}");
                    await Task.Delay(backoffs[attempt]);
                }
            }

            // Wenn alle Versuche fehlschlagen, sichtbare Fehlermeldung setzen
            UpdateStatus("Re-Register fehlgeschlagen nach mehreren Versuchen", System.Drawing.Color.Red);
        }

        // Versucht, DataDefinitions neu zu registrieren und die anfänglichen Requests zu senden.
        // Nützlich nach einem MSFS-Reconnect, wenn SimConnect mit "UNRECOGNIZED_ID" antwortet.
        private void ReRegisterDataDefinitionsAndRequests()
        {
            if (simconnect == null)
            {
                UpdateStatus("Re-Register: simconnect ist null", System.Drawing.Color.Red);
                return;
            }

            try
            {
                // DataDefinition erneut hinzufügen
                simconnect.AddToDataDefinition(
                    DEFINITIONS.AP_STATE,
                    "AUTOPILOT MASTER",
                    "Bool",
                    Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INT32,
                    0.0f,
                    SimConnect.SIMCONNECT_UNUSED);
            }
            catch (Exception ex)
            {
                UpdateStatus("Re-Register AddToDataDefinition Fehler: " + ex.Message, System.Drawing.Color.Red);
            }

            try
            {
                // Managed-Struct-Layout erneut registrieren
                simconnect.RegisterDataDefineStruct<ApStateStruct>(DEFINITIONS.AP_STATE);
            }
            catch (Exception ex)
            {
                UpdateStatus("Re-Register RegisterDataDefineStruct Fehler: " + ex.Message, System.Drawing.Color.Red);
            }

            try
            {
                // Autopilot-Änderungen direkt abonnieren (statt einer einmaligen ONCE-Anfrage)
                simconnect.RequestDataOnSimObject(
                    DATA_REQUESTS.REQUEST_AP_STATE_CHANGED,
                    DEFINITIONS.AP_STATE,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME,
                    Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
                    0u,
                    0u,
                    SimConnect.SIMCONNECT_UNUSED);
                UpdateStatus("Verbindung zu MSFS hergestellt... — Re-Register: CHANGED-Subscription aktiv", System.Drawing.Color.DarkGreen);
            }
            catch (Exception ex)
            {
                UpdateStatus("Re-Register RequestDataOnSimObject(CHANGED) Fehler: " + ex.Message, System.Drawing.Color.Red);
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

                // Direkt geliefertes Struct (nicht in einem Array geboxed)
                if (field is ApStateStruct directStruct)
                {
                    int ap_master = directStruct.ap_master;
                    bool isOn = ap_master != 0;
                    UpdateAutopilotIndicator(isOn);
                }

                if (field is object[] arr && arr.Length > 0)
                {
                    // dwData kann hier auch ein ApStateStruct sein (Wrapper liefert strukturen als boxed value)
                    try
                    {
                        // Wenn das erste Element ein struct vom Typ ApStateStruct ist, extrahiere das Feld ap_master.
                        object first = arr[0];
                        if (first is ApStateStruct aps)
                        {
                            int ap_master = aps.ap_master;
                            bool isOn = ap_master != 0;
                            UpdateAutopilotIndicator(isOn);
                        }
                        else
                        {
                            // Fallback: versuche eine numerische Konvertierung (z.B. boxed int, long, string)
                            int ap_master = Convert.ToInt32(arr[0]);
                            bool isOn = ap_master != 0;
                            UpdateAutopilotIndicator(isOn);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Konvertierungsfehler weiterhin explizit protokollieren
                        UpdateStatus("OnRecvSimobjectData: Konvertierungsfehler - " + ex.Message, System.Drawing.Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"OnRecvSimobjectData: Exception - {ex.Message}", System.Drawing.Color.Red);
            }
        }

        public Form1()
        {
            InitializeComponent();
            // Setze Standard-Starttext (Label vom Designer)
            lblStatus.Text = "Start...";

            // Timer einrichten: Versucht alle 2 Sekunden die Verbindung aufzubauen
            connectionTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            connectionTimer.Tick += ConnectionTimer_Tick;
            connectionTimer.Start();
        }

        private void ConnectionTimer_Tick(object? sender, EventArgs e)
        {
            if (!isConnected)
            {
                TryConnect();
            }
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

                isConnected = true;
                // Timer kann nach Verbindung gestoppt werden. Auf Events warten.
                connectionTimer.Stop(); 
                // Benutzer in der Statusleiste informieren, dass der Verbindungsaufbau läuft.
                UpdateStatus("Warten auf Verbindung zu MSFS...", System.Drawing.Color.Orange);
            }
            catch (COMException)
            {
                // MSFS läuft noch nicht oder ist im Ladebildschirm
                UpdateStatus("MSFS nicht gefunden. Verbindungsversuch...", System.Drawing.Color.OrangeRed);
            }
            catch (Exception ex)
            {
                // Alle anderen Fehler sichtbar machen
                UpdateStatus("Fehler beim Verbinden: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        // Event: Verbindung erfolgreich hergestellt
        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            // Wenn sich SimConnect hier meldet, sind wir Verbunden
            UpdateStatus("Verbunden mit MSFS 2024", System.Drawing.Color.DarkGreen);

            // --------------- Autopilot-Status ----------------
            // Initialer Autopilot-Zustand unbekannt -> auf aus setzen
            UpdateAutopilotIndicator(false);
            try
            {
                // DataDefinition für AUTOPILOT MASTER registrieren
                if (simconnect == null)
                {
                    UpdateStatus("Fehler OnRecvOpen: SimConnect=NULL", System.Drawing.Color.Red);
                }
                else
                {
                    // Register autopilot status mit "AUTOPILOT MASTER"
                    try
                    {
                        simconnect.AddToDataDefinition(
                            DEFINITIONS.AP_STATE,
                            "AUTOPILOT MASTER",
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
                        simconnect.RegisterDataDefineStruct<ApStateStruct>(DEFINITIONS.AP_STATE);
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler bei RegisterDataDefineStruct: " + ex.Message, System.Drawing.Color.Red);
                    }

                    // Autopilot-Änderungen abonnieren
                    try
                    {
                        simconnect.RequestDataOnSimObject(
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
                    // Toggle-Ereignisse abbilden
                        simconnect.MapClientEventToSimEvent((Enum)EventID, "ap_master");
                        // Notification-Group und Priorität nur einmal registrieren
                        if (!eventsRegistered)
                        {
                            try
                            {
                                simconnect.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, EventID, true);
                                simconnect.SetNotificationGroupPriority(NOTIFICATION_GROUPS.GROUP0, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);
                                eventsRegistered = true;
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus("Fehler beim Registrieren der Notification-Group: " + ex.Message, System.Drawing.Color.OrangeRed);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus("Fehler beim Mappen von Client-Events: " + ex.Message, System.Drawing.Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Fehler bei DataDefinition/Request: " + ex.Message, System.Drawing.Color.Red);
            }
        }

        // Event: MSFS wurde geschlossen
        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            UpdateStatus("Verbindung getrennt. Suche...", System.Drawing.Color.OrangeRed);
            // gehe nach Disconnect um alles aufzuräumen und den Timer wieder zu aktivieren
            Disconnect();
        }

        // Event: SimConnect wirft eine Ausnahme
        private void Simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            UpdateStatus($"SimConnectFehler: {data.dwException}", System.Drawing.Color.Red);
            try { Debug.WriteLine($"{DateTime.Now:O} [SimConnect Exception] dwException={data.dwException}, dwSendID={data.dwSendID}, dwIndex={data.dwIndex}, dwVersion={data.dwVersion}"); } catch { }

            // Wenn die Ausnahme auf eine unbekannte ID hinweist, versuchen wir,
            // die DataDefinitions neu zu registrieren und die anfänglichen Requests
            // erneut zu senden — löst Probleme nach einem Reconnect ab
            try
            {
                if (data.dwException == 3u) // UNRECOGNIZED_ID
                {
                    UpdateStatus("UNRECOGNIZED_ID empfangen — versuche DataDefinitions neu zu registrieren", System.Drawing.Color.OrangeRed);
                    // Setze den internen Subscription-Zustand zurück, damit eine erneute Anmeldung möglich ist.
                    // Asynchroner "fire-and-forget"-Aufruf, um Re-Register/Retry im Hintergrund auszuführen.
                    _ = ReRegisterDataDefinitionsAndRequestsAsync();
                }
            }
            catch { }
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
                        try
                        {
                            int ap_master = Convert.ToInt32(arr[0]);
                            bool isOn = ap_master != 0;
                            UpdateAutopilotIndicator(isOn);
                            // CHANGED-Subscription wird beim Verbinden / Re-Register gestartet; 
                        }
                        catch (Exception)
                        {
                            // Fallback: Konvertierungsfehler ignorieren
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
            // Abonnementstatus zurücksetzen, damit eine erneute Registrierung erfolgen kann
            isConnected = false;
            eventsRegistered = false;
            UpdateAutopilotIndicator(false);
            // Timer reaktivieren für automatischen Wiederverbindungsversuch
            connectionTimer.Start(); 
            UpdateStatus("Nicht verbunden", System.Drawing.Color.OrangeRed);
        }

        // Aktualisiert die Anzeige des Autopilot-Buttons (Blau = ON)
        private void UpdateAutopilotIndicator(bool isOn)
        {
            if (btnAutopilot.InvokeRequired)
            {
                btnAutopilot.Invoke(new Action(() => UpdateAutopilotIndicator(isOn)));
                return;
            }

            if (isOn)
            {
                btnAutopilot.UseVisualStyleBackColor = false;
                btnAutopilot.FlatStyle = FlatStyle.Flat;
                btnAutopilot.FlatAppearance.BorderSize = 1;
                btnAutopilot.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
                btnAutopilot.BackColor = System.Drawing.Color.DodgerBlue;
                btnAutopilot.ForeColor = System.Drawing.Color.White;
            }
            else
            {
                // Schaltfläche bei ausgeschaltetem AP sichtbar machen: dezenter Rahmen und Standardhintergrund
                btnAutopilot.UseVisualStyleBackColor = true;
                btnAutopilot.FlatStyle = FlatStyle.Flat;
                btnAutopilot.FlatAppearance.BorderSize = 1;
                btnAutopilot.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlDark;
                btnAutopilot.BackColor = System.Drawing.SystemColors.Control;
                btnAutopilot.ForeColor = System.Drawing.Color.Black;
            }
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

        // Threadsichere Hilfsfunktion zum Setzen des Tooltips für das Status-Label
        private void SetStatusTooltip(string tooltip)
        {
            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(new Action(() => SetStatusTooltip(tooltip)));
                return;
            }

            try { lblStatus.ToolTipText = tooltip ?? string.Empty; } catch { }
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
