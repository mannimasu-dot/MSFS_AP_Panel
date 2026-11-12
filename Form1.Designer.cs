namespace MSFS_AP_Panel
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Statusleiste und Label
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.Button btnAutopilot;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnAutopilot = new Button();
            statusStrip1 = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            grpALT = new GroupBox();
            altitudeTape = new AltitudeTape();
            ctrVS = new CenterProgressBar();
            txtVS = new TextBox();
            txtAltSoll = new TextBox();
            btnALT = new Button();
            lblAltIst = new Label();
            grpHDG = new GroupBox();
            btnHDG = new Button();
            txtHDGSoll = new TextBox();
            headingTape = new HeadingTape();
            lblHDGIst = new Label();
            grpNAV1 = new GroupBox();
            btnAPPR = new Button();
            txtNav1 = new TextBox();
            grpAirSpeed = new GroupBox();
            airspeedTape = new AirspeedTape();
            statusStrip1.SuspendLayout();
            grpALT.SuspendLayout();
            grpHDG.SuspendLayout();
            grpNAV1.SuspendLayout();
            grpAirSpeed.SuspendLayout();
            SuspendLayout();
            // 
            // btnAutopilot
            // 
            btnAutopilot.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnAutopilot.Cursor = Cursors.Hand;
            btnAutopilot.Location = new Point(10, 8);
            btnAutopilot.Name = "btnAutopilot";
            btnAutopilot.Size = new Size(222, 36);
            btnAutopilot.TabIndex = 0;
            btnAutopilot.Text = "Autopilot";
            btnAutopilot.Click += BtnAutopilot_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip1.Location = new Point(0, 438);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.ShowItemToolTips = true;
            statusStrip1.Size = new Size(244, 22);
            statusStrip1.SizingGrip = false;
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(39, 17);
            lblStatus.Text = "Ready";
            // 
            // grpALT
            // 
            grpALT.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpALT.Controls.Add(altitudeTape);
            grpALT.Controls.Add(ctrVS);
            grpALT.Controls.Add(txtVS);
            grpALT.Controls.Add(txtAltSoll);
            grpALT.Controls.Add(btnALT);
            grpALT.Location = new Point(10, 45);
            grpALT.Name = "grpALT";
            grpALT.Size = new Size(219, 144);
            grpALT.TabIndex = 1;
            grpALT.TabStop = false;
            grpALT.Text = "ALT";
            // 
            // altitudeTape
            // 
            altitudeTape.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            altitudeTape.BackColor = Color.FromArgb(34, 34, 34);
            altitudeTape.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            altitudeTape.ForeColor = Color.White;
            altitudeTape.Location = new Point(10, 17);
            altitudeTape.Name = "altitudeTape";
            altitudeTape.Size = new Size(198, 60);
            altitudeTape.TabIndex = 0;
            altitudeTape.VisibleSpanFeet = 6000;
            // 
            // ctrVS
            // 
            ctrVS.BackColor = SystemColors.Control;
            ctrVS.BorderColor = Color.Silver;
            ctrVS.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            ctrVS.ForeColor = Color.Black;
            ctrVS.Location = new Point(111, 84);
            ctrVS.Name = "ctrVS";
            ctrVS.NegativeColor = Color.LightSkyBlue;
            ctrVS.PositiveColor = Color.LightGreen;
            ctrVS.Size = new Size(97, 22);
            ctrVS.TabIndex = 3;
            // 
            // txtVS
            // 
            txtVS.Location = new Point(10, 83);
            txtVS.Name = "txtVS";
            txtVS.Size = new Size(84, 23);
            txtVS.TabIndex = 2;
            txtVS.Enter += TxtAltSoll_Enter;
            txtVS.KeyDown += TxtAltSoll_KeyDown;
            // 
            // txtAltSoll
            // 
            txtAltSoll.Location = new Point(10, 112);
            txtAltSoll.Name = "txtAltSoll";
            txtAltSoll.Size = new Size(84, 23);
            txtAltSoll.TabIndex = 3;
            txtAltSoll.Enter += TxtAltSoll_Enter;
            txtAltSoll.KeyDown += TxtAltSoll_KeyDown;
            // 
            // btnALT
            // 
            btnALT.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnALT.Cursor = Cursors.Hand;
            btnALT.Location = new Point(109, 112);
            btnALT.Name = "btnALT";
            btnALT.Size = new Size(101, 23);
            btnALT.TabIndex = 2;
            btnALT.Text = "ALT";
            btnALT.Click += BtnALT_Click;
            // 
            // lblAltIst
            // 
            lblAltIst.Location = new Point(0, 0);
            lblAltIst.Name = "lblAltIst";
            lblAltIst.Size = new Size(100, 23);
            lblAltIst.TabIndex = 0;
            lblAltIst.Visible = false;
            // 
            // grpHDG
            // 
            grpHDG.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpHDG.Controls.Add(btnHDG);
            grpHDG.Controls.Add(txtHDGSoll);
            grpHDG.Controls.Add(headingTape);
            grpHDG.Location = new Point(10, 189);
            grpHDG.Name = "grpHDG";
            grpHDG.Size = new Size(219, 97);
            grpHDG.TabIndex = 2;
            grpHDG.TabStop = false;
            grpHDG.Text = "HDG";
            // 
            // btnHDG
            // 
            btnHDG.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnHDG.Cursor = Cursors.Hand;
            btnHDG.Location = new Point(109, 65);
            btnHDG.Name = "btnHDG";
            btnHDG.Size = new Size(101, 23);
            btnHDG.TabIndex = 2;
            btnHDG.Text = "HDG";
            btnHDG.Click += BtnHDG_Click;
            // 
            // txtHDGSoll
            // 
            txtHDGSoll.Location = new Point(10, 66);
            txtHDGSoll.Name = "txtHDGSoll";
            txtHDGSoll.Size = new Size(84, 23);
            txtHDGSoll.TabIndex = 1;
            txtHDGSoll.Enter += TxtHdgSoll_Enter;
            txtHDGSoll.KeyDown += TxtHdgSoll_KeyDown;
            // 
            // headingTape
            // 
            headingTape.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            headingTape.BackColor = Color.FromArgb(34, 34, 34);
            headingTape.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            headingTape.ForeColor = Color.White;
            headingTape.Location = new Point(10, 18);
            headingTape.Name = "headingTape";
            headingTape.Size = new Size(198, 42);
            headingTape.TabIndex = 0;
            // 
            // lblHDGIst
            // 
            lblHDGIst.Location = new Point(0, 0);
            lblHDGIst.Name = "lblHDGIst";
            lblHDGIst.Size = new Size(100, 23);
            lblHDGIst.TabIndex = 0;
            lblHDGIst.Visible = false;
            // 
            // grpNAV1
            // 
            grpNAV1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpNAV1.Controls.Add(btnAPPR);
            grpNAV1.Controls.Add(txtNav1);
            grpNAV1.Location = new Point(10, 290);
            grpNAV1.Name = "grpNAV1";
            grpNAV1.Size = new Size(219, 61);
            grpNAV1.TabIndex = 3;
            grpNAV1.TabStop = false;
            grpNAV1.Text = "NAV1";
            // 
            // btnAPPR
            // 
            btnAPPR.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnAPPR.Cursor = Cursors.Hand;
            btnAPPR.Location = new Point(109, 22);
            btnAPPR.Name = "btnAPPR";
            btnAPPR.Size = new Size(101, 23);
            btnAPPR.TabIndex = 2;
            btnAPPR.Text = "Approach";
            btnAPPR.UseVisualStyleBackColor = true;
            btnAPPR.Click += BtnAPPR_Click;
            // 
            // txtNav1
            // 
            txtNav1.Location = new Point(10, 22);
            txtNav1.Name = "txtNav1";
            txtNav1.Size = new Size(84, 23);
            txtNav1.TabIndex = 0;
            txtNav1.Enter += TxtNav1_Enter;
            txtNav1.KeyDown += TxtNav1_KeyDown;
            // 
            // grpAirSpeed
            // 
            grpAirSpeed.BackColor = SystemColors.Control;
            grpAirSpeed.Controls.Add(airspeedTape);
            grpAirSpeed.Location = new Point(10, 351);
            grpAirSpeed.Name = "grpAirSpeed";
            grpAirSpeed.Size = new Size(219, 79);
            grpAirSpeed.TabIndex = 4;
            grpAirSpeed.TabStop = false;
            grpAirSpeed.Text = "Air Speed";
            // 
            // airspeedTape
            // 
            airspeedTape.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            airspeedTape.BackColor = Color.FromArgb(34, 34, 34);
            airspeedTape.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            airspeedTape.ForeColor = Color.White;
            airspeedTape.Location = new Point(10, 21);
            airspeedTape.Name = "airspeedTape";
            airspeedTape.Size = new Size(198, 42);
            airspeedTape.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(244, 460);
            Controls.Add(grpAirSpeed);
            Controls.Add(grpNAV1);
            Controls.Add(grpHDG);
            Controls.Add(grpALT);
            Controls.Add(statusStrip1);
            Controls.Add(btnAutopilot);
            Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            Text = "MSFS Autopilot";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            grpALT.ResumeLayout(false);
            grpALT.PerformLayout();
            grpHDG.ResumeLayout(false);
            grpHDG.PerformLayout();
            grpNAV1.ResumeLayout(false);
            grpNAV1.PerformLayout();
            grpAirSpeed.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtAltSoll;
        private System.Windows.Forms.Label lblAltIst;
        private MSFS_AP_Panel.AltitudeTape altitudeTape;
        private Button btnALT;
        private System.Windows.Forms.GroupBox grpALT;
        private CenterProgressBar ctrVS;
        private System.Windows.Forms.TextBox txtVS;
        private GroupBox grpHDG;
        private Label lblHDGIst;
        private MSFS_AP_Panel.HeadingTape headingTape;
        private Button btnHDG;
        private TextBox txtHDGSoll;
        private GroupBox grpNAV1;
        private Button btnAPPR;
        private TextBox txtNav1;
        private GroupBox grpAirSpeed;
        private AirspeedTape airspeedTape;
    }
}
