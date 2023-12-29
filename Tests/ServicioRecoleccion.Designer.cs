
namespace Tests
{
    partial class ServicioRecoleccion
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_inicio = new System.Windows.Forms.Button();
            this.btn_detener = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblSalida = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btn_inicio
            // 
            this.btn_inicio.Location = new System.Drawing.Point(114, 48);
            this.btn_inicio.Name = "btn_inicio";
            this.btn_inicio.Size = new System.Drawing.Size(105, 62);
            this.btn_inicio.TabIndex = 0;
            this.btn_inicio.Text = "Iniciar";
            this.btn_inicio.UseVisualStyleBackColor = true;
            this.btn_inicio.Click += new System.EventHandler(this.inicioServicio);
            // 
            // btn_detener
            // 
            this.btn_detener.Location = new System.Drawing.Point(114, 101);
            this.btn_detener.Name = "btn_detener";
            this.btn_detener.Size = new System.Drawing.Size(105, 61);
            this.btn_detener.TabIndex = 1;
            this.btn_detener.Text = "Detener";
            this.btn_detener.UseVisualStyleBackColor = true;
            this.btn_detener.Click += new System.EventHandler(this.detenerServicio);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(12, 218);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(310, 297);
            this.txtLog.TabIndex = 2;
            // 
            // lblSalida
            // 
            this.lblSalida.AutoSize = true;
            this.lblSalida.Location = new System.Drawing.Point(12, 199);
            this.lblSalida.Name = "lblSalida";
            this.lblSalida.Size = new System.Drawing.Size(50, 16);
            this.lblSalida.TabIndex = 3;
            this.lblSalida.Text = "Salida:";
            // 
            // ServicioRecoleccion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 527);
            this.Controls.Add(this.lblSalida);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btn_detener);
            this.Controls.Add(this.btn_inicio);
            this.MaximizeBox = false;
            this.Name = "ServicioRecoleccion";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ServicioRecoleccion";
            this.Load += new System.EventHandler(this.ServicioRecoleccion_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_inicio;
        private System.Windows.Forms.Button btn_detener;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblSalida;
    }
}