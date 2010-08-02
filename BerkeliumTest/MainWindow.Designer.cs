namespace BerkeliumWinFormsTest {
    partial class MainWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose (bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent () {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.AddressBar = new System.Windows.Forms.TextBox();
            this.Toolbar = new System.Windows.Forms.ToolStrip();
            this.tbBack = new System.Windows.Forms.ToolStripButton();
            this.tbForward = new System.Windows.Forms.ToolStripButton();
            this.tbRefresh = new System.Windows.Forms.ToolStripButton();
            this.tbStop = new System.Windows.Forms.ToolStripButton();
            this.WebKit = new BerkeliumWinFormsTest.WebKitFrame();
            this.Toolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // AddressBar
            // 
            this.AddressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.AddressBar.Location = new System.Drawing.Point(98, 3);
            this.AddressBar.Name = "AddressBar";
            this.AddressBar.Size = new System.Drawing.Size(534, 20);
            this.AddressBar.TabIndex = 0;
            this.AddressBar.TabStop = false;
            this.AddressBar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AddressBar_KeyDown);
            // 
            // Toolbar
            // 
            this.Toolbar.Dock = System.Windows.Forms.DockStyle.None;
            this.Toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.Toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbBack,
            this.tbForward,
            this.tbRefresh,
            this.tbStop});
            this.Toolbar.Location = new System.Drawing.Point(0, 0);
            this.Toolbar.Name = "Toolbar";
            this.Toolbar.Size = new System.Drawing.Size(126, 25);
            this.Toolbar.TabIndex = 2;
            this.Toolbar.Text = "toolStrip1";
            // 
            // tbBack
            // 
            this.tbBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbBack.Enabled = false;
            this.tbBack.Image = ((System.Drawing.Image)(resources.GetObject("tbBack.Image")));
            this.tbBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbBack.Name = "tbBack";
            this.tbBack.Size = new System.Drawing.Size(23, 22);
            this.tbBack.ToolTipText = "Back";
            this.tbBack.Click += new System.EventHandler(this.tbBack_Click);
            // 
            // tbForward
            // 
            this.tbForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbForward.Enabled = false;
            this.tbForward.Image = ((System.Drawing.Image)(resources.GetObject("tbForward.Image")));
            this.tbForward.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbForward.Name = "tbForward";
            this.tbForward.Size = new System.Drawing.Size(23, 22);
            this.tbForward.ToolTipText = "Forward";
            this.tbForward.Click += new System.EventHandler(this.tbForward_Click);
            // 
            // tbRefresh
            // 
            this.tbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbRefresh.Enabled = false;
            this.tbRefresh.Image = ((System.Drawing.Image)(resources.GetObject("tbRefresh.Image")));
            this.tbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbRefresh.Name = "tbRefresh";
            this.tbRefresh.Size = new System.Drawing.Size(23, 22);
            this.tbRefresh.ToolTipText = "Refresh";
            this.tbRefresh.Click += new System.EventHandler(this.tbRefresh_Click);
            // 
            // tbStop
            // 
            this.tbStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbStop.Enabled = false;
            this.tbStop.Image = ((System.Drawing.Image)(resources.GetObject("tbStop.Image")));
            this.tbStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbStop.Name = "tbStop";
            this.tbStop.Size = new System.Drawing.Size(23, 22);
            this.tbStop.ToolTipText = "Stop";
            this.tbStop.Click += new System.EventHandler(this.tbStop_Click);
            // 
            // WebKit
            // 
            this.WebKit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.WebKit.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.WebKit.CausesValidation = false;
            this.WebKit.Location = new System.Drawing.Point(1, 27);
            this.WebKit.Name = "WebKit";
            this.WebKit.Size = new System.Drawing.Size(631, 424);
            this.WebKit.TabIndex = 1;
            this.WebKit.TabStop = false;
            this.WebKit.AddressChanged += new System.Action<object, string>(this.WebKit_AddressChanged);
            this.WebKit.PageLoaded += new System.Action<object>(this.WebKit_PageLoaded);
            this.WebKit.ScriptAlert += new System.Action<object, BerkeliumWinFormsTest.ScriptAlertEventArgs>(this.WebKit_ScriptAlert);
            this.WebKit.LoadingStateChanged += new System.Action<object, bool>(this.WebKit_LoadingStateChanged);
            this.WebKit.WindowOpened += new System.Action<object, BerkeliumWinFormsTest.WebKitFrame, System.Drawing.Rectangle, string>(this.WebKit_WindowOpened);
            this.WebKit.NavigationRequested += new System.Action<object, BerkeliumWinFormsTest.NavigationRequestedEventArgs>(this.WebKit_NavigationRequested);
            this.WebKit.TitleChanged += new System.Action<object, string>(this.WebKit_TitleChanged);
            this.WebKit.ChromeSend += new System.Action<object, string, string[]>(this.WebKit_ChromeSend);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 452);
            this.Controls.Add(this.Toolbar);
            this.Controls.Add(this.WebKit);
            this.Controls.Add(this.AddressBar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainWindow";
            this.Text = "Berkelium Test";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Toolbar.ResumeLayout(false);
            this.Toolbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox AddressBar;
        private WebKitFrame WebKit;
        private System.Windows.Forms.ToolStrip Toolbar;
        private System.Windows.Forms.ToolStripButton tbBack;
        private System.Windows.Forms.ToolStripButton tbForward;
        private System.Windows.Forms.ToolStripButton tbRefresh;
        private System.Windows.Forms.ToolStripButton tbStop;

    }
}

