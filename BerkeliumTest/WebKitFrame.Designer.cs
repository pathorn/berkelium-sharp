namespace BerkeliumWinFormsTest {
    partial class WebKitFrame {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent () {
            this.components = new System.ComponentModel.Container();
            this.ToolTipProvider = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // WebKitFrame
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.CausesValidation = false;
            this.Name = "WebKitFrame";
            this.Load += new System.EventHandler(this.WebKitFrame_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.UserControl_Paint);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UserControl_MouseMove);
            this.Leave += new System.EventHandler(this.WebKitFrame_Leave);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.UserControl_KeyUp);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UserControl_MouseDown);
            this.Resize += new System.EventHandler(this.UserControl_Resize);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.WebKitFrame_KeyPress);
            this.Enter += new System.EventHandler(this.WebKitFrame_Enter);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UserControl_MouseUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.UserControl_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolTip ToolTipProvider;
    }
}
