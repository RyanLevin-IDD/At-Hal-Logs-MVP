namespace At_Hal_Logs
{
    partial class AppWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            btnStart = new Button();
            lblTimer = new Label();
            txtTimerRunner = new TextBox();
            SelectBoxFilterByTime = new ComboBox();
            lblEmail = new Label();
            lblPass = new Label();
            txtPassword = new TextBox();
            txtEmail = new TextBox();
            grpCredentials = new GroupBox();
            groupBox1 = new GroupBox();
            SelectBoxHoursAgo = new ComboBox();
            label2 = new Label();
            label1 = new Label();
            lblProfile = new Label();
            txtProfile = new TextBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            stopRun_btn = new Button();
            doneBox = new TextBox();
            groupBox5 = new GroupBox();
            lblTimerText = new Label();
            lblStatus = new Label();
            tabPage2 = new TabPage();
            groupBox4 = new GroupBox();
            chkEnableTimer = new CheckBox();
            lblAutoRun = new Label();
            groupBox3 = new GroupBox();
            dev_api_lbl = new Label();
            client_api_lbl = new Label();
            txtAPI_Client = new TextBox();
            txtAPI_Dev = new TextBox();
            save_btn = new Button();
            tabPage3 = new TabPage();
            box2 = new GroupBox();
            txtLogs = new TextBox();
            grpCredentials.SuspendLayout();
            groupBox1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            groupBox5.SuspendLayout();
            tabPage2.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            tabPage3.SuspendLayout();
            box2.SuspendLayout();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.BackColor = SystemColors.ControlLight;
            btnStart.Font = new Font("Segoe UI", 24F);
            btnStart.Location = new Point(56, 82);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(385, 120);
            btnStart.TabIndex = 0;
            btnStart.Text = "Fetch Logs Now";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // lblTimer
            // 
            lblTimer.AutoSize = true;
            lblTimer.BackColor = Color.WhiteSmoke;
            lblTimer.Font = new Font("Segoe UI Emoji", 18F);
            lblTimer.Location = new Point(293, 73);
            lblTimer.Margin = new Padding(2, 0, 2, 0);
            lblTimer.Name = "lblTimer";
            lblTimer.Size = new Size(71, 32);
            lblTimer.TabIndex = 1;
            lblTimer.Text = "60:00";
            // 
            // txtTimerRunner
            // 
            txtTimerRunner.BackColor = Color.WhiteSmoke;
            txtTimerRunner.BorderStyle = BorderStyle.None;
            txtTimerRunner.Font = new Font("Segoe UI", 18F);
            txtTimerRunner.Location = new Point(152, 38);
            txtTimerRunner.Margin = new Padding(2, 2, 2, 2);
            txtTimerRunner.Name = "txtTimerRunner";
            txtTimerRunner.Size = new Size(352, 32);
            txtTimerRunner.TabIndex = 2;
            txtTimerRunner.Text = "AutoRun ON";
            txtTimerRunner.TextAlign = HorizontalAlignment.Center;
            // 
            // SelectBoxFilterByTime
            // 
            SelectBoxFilterByTime.Font = new Font("Segoe UI", 18F);
            SelectBoxFilterByTime.FormattingEnabled = true;
            SelectBoxFilterByTime.Items.AddRange(new object[] { "Last 1h", "Last 6h", "Last 24h", "Last 7d" });
            SelectBoxFilterByTime.Location = new Point(11, 72);
            SelectBoxFilterByTime.Margin = new Padding(2, 2, 2, 2);
            SelectBoxFilterByTime.Name = "SelectBoxFilterByTime";
            SelectBoxFilterByTime.Size = new Size(225, 40);
            SelectBoxFilterByTime.TabIndex = 3;
            SelectBoxFilterByTime.Text = "Time Filter";
            SelectBoxFilterByTime.SelectedIndexChanged += SelectBoxFilterByTime_SelectedIndexChanged;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Location = new Point(4, 38);
            lblEmail.Margin = new Padding(2, 0, 2, 0);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(71, 32);
            lblEmail.TabIndex = 4;
            lblEmail.Text = "Email";
            // 
            // lblPass
            // 
            lblPass.AutoSize = true;
            lblPass.Location = new Point(0, 85);
            lblPass.Margin = new Padding(2, 0, 2, 0);
            lblPass.Name = "lblPass";
            lblPass.Size = new Size(111, 32);
            lblPass.TabIndex = 5;
            lblPass.Text = "Password";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(128, 80);
            txtPassword.Margin = new Padding(2, 2, 2, 2);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(308, 39);
            txtPassword.TabIndex = 6;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(128, 34);
            txtEmail.Margin = new Padding(2, 2, 2, 2);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(308, 39);
            txtEmail.TabIndex = 7;
            // 
            // grpCredentials
            // 
            grpCredentials.Controls.Add(lblPass);
            grpCredentials.Controls.Add(lblEmail);
            grpCredentials.Controls.Add(txtEmail);
            grpCredentials.Controls.Add(txtPassword);
            grpCredentials.Font = new Font("Segoe UI", 18F);
            grpCredentials.Location = new Point(40, 14);
            grpCredentials.Margin = new Padding(2, 2, 2, 2);
            grpCredentials.Name = "grpCredentials";
            grpCredentials.Padding = new Padding(2, 2, 2, 2);
            grpCredentials.Size = new Size(451, 120);
            grpCredentials.TabIndex = 8;
            grpCredentials.TabStop = false;
            grpCredentials.Text = "Credentials";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(SelectBoxHoursAgo);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(lblProfile);
            groupBox1.Controls.Add(txtProfile);
            groupBox1.Controls.Add(SelectBoxFilterByTime);
            groupBox1.Font = new Font("Segoe UI", 18F);
            groupBox1.Location = new Point(44, 269);
            groupBox1.Margin = new Padding(2, 2, 2, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(2, 2, 2, 2);
            groupBox1.Size = new Size(727, 120);
            groupBox1.TabIndex = 9;
            groupBox1.TabStop = false;
            groupBox1.Text = "Options";
            // 
            // SelectBoxHoursAgo
            // 
            SelectBoxHoursAgo.Font = new Font("Segoe UI", 18F);
            SelectBoxHoursAgo.FormattingEnabled = true;
            SelectBoxHoursAgo.Items.AddRange(new object[] { "2", "3", "4", "5" });
            SelectBoxHoursAgo.Location = new Point(271, 72);
            SelectBoxHoursAgo.Margin = new Padding(2, 2, 2, 2);
            SelectBoxHoursAgo.Name = "SelectBoxHoursAgo";
            SelectBoxHoursAgo.Size = new Size(225, 40);
            SelectBoxHoursAgo.TabIndex = 8;
            SelectBoxHoursAgo.Text = "X Hours Ago";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(271, 41);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(181, 32);
            label2.TabIndex = 7;
            label2.Text = "Fetch logs from";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(11, 41);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(178, 32);
            label1.TabIndex = 6;
            label1.Text = "Hostinger Filter";
            // 
            // lblProfile
            // 
            lblProfile.AutoSize = true;
            lblProfile.Location = new Point(518, 41);
            lblProfile.Margin = new Padding(2, 0, 2, 0);
            lblProfile.Name = "lblProfile";
            lblProfile.Size = new Size(174, 32);
            lblProfile.TabIndex = 5;
            lblProfile.Text = "Chrome Profile";
            // 
            // txtProfile
            // 
            txtProfile.Location = new Point(518, 73);
            txtProfile.Margin = new Padding(2, 2, 2, 2);
            txtProfile.Name = "txtProfile";
            txtProfile.Size = new Size(197, 39);
            txtProfile.TabIndex = 4;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Location = new Point(-1, 2);
            tabControl1.Margin = new Padding(2, 2, 2, 2);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(825, 419);
            tabControl1.TabIndex = 10;
            tabControl1.TabStop = false;
            // 
            // tabPage1
            // 
            tabPage1.BackColor = Color.WhiteSmoke;
            tabPage1.Controls.Add(stopRun_btn);
            tabPage1.Controls.Add(doneBox);
            tabPage1.Controls.Add(groupBox5);
            tabPage1.Controls.Add(btnStart);
            tabPage1.Font = new Font("Segoe UI", 12F);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Margin = new Padding(2, 2, 2, 2);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(2, 2, 2, 2);
            tabPage1.Size = new Size(817, 391);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Main";
            // 
            // stopRun_btn
            // 
            stopRun_btn.BackColor = Color.RosyBrown;
            stopRun_btn.Font = new Font("Segoe UI", 24F);
            stopRun_btn.Location = new Point(561, 82);
            stopRun_btn.Name = "stopRun_btn";
            stopRun_btn.Size = new Size(195, 120);
            stopRun_btn.TabIndex = 6;
            stopRun_btn.Text = "Stop Run";
            stopRun_btn.UseVisualStyleBackColor = false;
            stopRun_btn.Click += stopRun_btn_Click;
            // 
            // doneBox
            // 
            doneBox.BackColor = Color.WhiteSmoke;
            doneBox.BorderStyle = BorderStyle.None;
            doneBox.Font = new Font("Segoe UI", 18F);
            doneBox.ForeColor = Color.Lime;
            doneBox.Location = new Point(208, 32);
            doneBox.Margin = new Padding(2, 2, 2, 2);
            doneBox.Name = "doneBox";
            doneBox.Size = new Size(385, 32);
            doneBox.TabIndex = 5;
            doneBox.TextAlign = HorizontalAlignment.Center;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(lblTimerText);
            groupBox5.Controls.Add(lblStatus);
            groupBox5.Controls.Add(lblTimer);
            groupBox5.Controls.Add(txtTimerRunner);
            groupBox5.Font = new Font("Segoe UI", 18F);
            groupBox5.Location = new Point(56, 236);
            groupBox5.Margin = new Padding(2, 2, 2, 2);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new Padding(2, 2, 2, 2);
            groupBox5.Size = new Size(700, 120);
            groupBox5.TabIndex = 4;
            groupBox5.TabStop = false;
            groupBox5.Text = "Auto Run Status";
            // 
            // lblTimerText
            // 
            lblTimerText.AutoSize = true;
            lblTimerText.Location = new Point(27, 73);
            lblTimerText.Margin = new Padding(2, 0, 2, 0);
            lblTimerText.Name = "lblTimerText";
            lblTimerText.Size = new Size(135, 32);
            lblTimerText.TabIndex = 4;
            lblTimerText.Text = "Auto run in";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(27, 38);
            lblStatus.Margin = new Padding(2, 0, 2, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(78, 32);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "Status";
            // 
            // tabPage2
            // 
            tabPage2.BackColor = Color.WhiteSmoke;
            tabPage2.Controls.Add(groupBox4);
            tabPage2.Controls.Add(groupBox3);
            tabPage2.Controls.Add(save_btn);
            tabPage2.Controls.Add(grpCredentials);
            tabPage2.Controls.Add(groupBox1);
            tabPage2.Font = new Font("Segoe UI", 12F);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Margin = new Padding(2, 2, 2, 2);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(2, 2, 2, 2);
            tabPage2.Size = new Size(817, 391);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Settings";
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(chkEnableTimer);
            groupBox4.Controls.Add(lblAutoRun);
            groupBox4.Font = new Font("Segoe UI", 18F);
            groupBox4.Location = new Point(503, 22);
            groupBox4.Margin = new Padding(2, 2, 2, 2);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new Padding(2, 2, 2, 2);
            groupBox4.Size = new Size(267, 65);
            groupBox4.TabIndex = 12;
            groupBox4.TabStop = false;
            groupBox4.Text = "Auto Run";
            // 
            // chkEnableTimer
            // 
            chkEnableTimer.AutoSize = true;
            chkEnableTimer.Location = new Point(102, 43);
            chkEnableTimer.Margin = new Padding(2, 2, 2, 2);
            chkEnableTimer.Name = "chkEnableTimer";
            chkEnableTimer.Size = new Size(15, 14);
            chkEnableTimer.TabIndex = 13;
            chkEnableTimer.UseVisualStyleBackColor = true;
            chkEnableTimer.CheckedChanged += chkEnableTimer_CheckedChanged;
            // 
            // lblAutoRun
            // 
            lblAutoRun.AutoSize = true;
            lblAutoRun.Location = new Point(10, 34);
            lblAutoRun.Margin = new Padding(2, 0, 2, 0);
            lblAutoRun.Name = "lblAutoRun";
            lblAutoRun.Size = new Size(85, 32);
            lblAutoRun.TabIndex = 11;
            lblAutoRun.Text = "Enable";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(dev_api_lbl);
            groupBox3.Controls.Add(client_api_lbl);
            groupBox3.Controls.Add(txtAPI_Client);
            groupBox3.Controls.Add(txtAPI_Dev);
            groupBox3.Font = new Font("Segoe UI", 18F);
            groupBox3.Location = new Point(40, 146);
            groupBox3.Margin = new Padding(2, 2, 2, 2);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(2, 2, 2, 2);
            groupBox3.Size = new Size(731, 125);
            groupBox3.TabIndex = 11;
            groupBox3.TabStop = false;
            groupBox3.Text = "Endpoint URLs";
            // 
            // dev_api_lbl
            // 
            dev_api_lbl.AutoSize = true;
            dev_api_lbl.Location = new Point(8, 32);
            dev_api_lbl.Margin = new Padding(2, 0, 2, 0);
            dev_api_lbl.Name = "dev_api_lbl";
            dev_api_lbl.Size = new Size(145, 32);
            dev_api_lbl.TabIndex = 11;
            dev_api_lbl.Text = "Domains list";
            // 
            // client_api_lbl
            // 
            client_api_lbl.AutoSize = true;
            client_api_lbl.Location = new Point(15, 77);
            client_api_lbl.Margin = new Padding(2, 0, 2, 0);
            client_api_lbl.Name = "client_api_lbl";
            client_api_lbl.Size = new Size(76, 32);
            client_api_lbl.TabIndex = 10;
            client_api_lbl.Text = "Client";
            // 
            // txtAPI_Client
            // 
            txtAPI_Client.Location = new Point(169, 77);
            txtAPI_Client.Margin = new Padding(2, 2, 2, 2);
            txtAPI_Client.Name = "txtAPI_Client";
            txtAPI_Client.Size = new Size(550, 39);
            txtAPI_Client.TabIndex = 9;
            // 
            // txtAPI_Dev
            // 
            txtAPI_Dev.Location = new Point(169, 32);
            txtAPI_Dev.Margin = new Padding(2, 2, 2, 2);
            txtAPI_Dev.Name = "txtAPI_Dev";
            txtAPI_Dev.Size = new Size(550, 39);
            txtAPI_Dev.TabIndex = 8;
            // 
            // save_btn
            // 
            save_btn.Font = new Font("Segoe UI", 18F);
            save_btn.Location = new Point(503, 105);
            save_btn.Margin = new Padding(2, 2, 2, 2);
            save_btn.Name = "save_btn";
            save_btn.Size = new Size(267, 37);
            save_btn.TabIndex = 10;
            save_btn.Text = "Save Settings";
            save_btn.UseVisualStyleBackColor = true;
            save_btn.Click += save_btn_Click;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(box2);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Margin = new Padding(2, 2, 2, 2);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(2, 2, 2, 2);
            tabPage3.Size = new Size(817, 391);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Console";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // box2
            // 
            box2.Controls.Add(txtLogs);
            box2.Font = new Font("Segoe UI", 12F);
            box2.Location = new Point(13, 13);
            box2.Margin = new Padding(2, 2, 2, 2);
            box2.Name = "box2";
            box2.Padding = new Padding(2, 2, 2, 2);
            box2.Size = new Size(793, 371);
            box2.TabIndex = 0;
            box2.TabStop = false;
            box2.Text = "Logs";
            // 
            // txtLogs
            // 
            txtLogs.Dock = DockStyle.Fill;
            txtLogs.Font = new Font("Calibri", 12F);
            txtLogs.Location = new Point(2, 24);
            txtLogs.Margin = new Padding(2, 2, 2, 2);
            txtLogs.Multiline = true;
            txtLogs.Name = "txtLogs";
            txtLogs.ReadOnly = true;
            txtLogs.ScrollBars = ScrollBars.Vertical;
            txtLogs.Size = new Size(789, 345);
            txtLogs.TabIndex = 0;
            // 
            // AppWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(825, 422);
            Controls.Add(tabControl1);
            Name = "AppWindow";
            Text = "AutomationHoistinger";
            Load += AppWindow_Load;
            grpCredentials.ResumeLayout(false);
            grpCredentials.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            tabPage2.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            tabPage3.ResumeLayout(false);
            box2.ResumeLayout(false);
            box2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnStart;
        public Label lblTimer;
        private TextBox txtTimerRunner;
        private ComboBox SelectBoxFilterByTime;
        private Label lblEmail;
        private Label lblPass;
        private TextBox txtPassword;
        private TextBox txtEmail;
        private GroupBox grpCredentials;
        private GroupBox groupBox1;
        private Label lblProfile;
        private TextBox txtProfile;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private Button save_btn;
        private Label label1;
        private GroupBox groupBox3;
        private TextBox txtAPI_Dev;
        private GroupBox groupBox4;
        private CheckBox chkEnableTimer;
        private Label lblAutoRun;
        private GroupBox groupBox5;
        private Label lblTimerText;
        private Label lblStatus;
        private TextBox doneBox;
        private TabPage tabPage3;
        private GroupBox box2;
        private TextBox txtLogs;
        private Button stopRun_btn;
        private TextBox txtAPI_Client;
        private Label dev_api_lbl;
        private Label client_api_lbl;
        private Label label2;
        private ComboBox SelectBoxHoursAgo;
    }
}
