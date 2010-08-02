namespace BerkeliumWinFormsTest {
    partial class FloatingWindow {
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
            this.SuspendLayout();
            // 
            // FloatingWindow
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(64, 64);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FloatingWindow";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "FloatingWindow";
            this.Deactivate += new System.EventHandler(this.FloatingWindow_Deactivate);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FloatingWindow_MouseUp);
            this.Shown += new System.EventHandler(this.FloatingWindow_Shown);
            this.Activated += new System.EventHandler(this.FloatingWindow_Activated);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FloatingWindow_MouseDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FloatingWindow_KeyPress);
            this.MouseLeave += new System.EventHandler(this.FloatingWindow_MouseLeave);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FloatingWindow_KeyUp);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FloatingWindow_MouseMove);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FloatingWindow_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion
    }
}