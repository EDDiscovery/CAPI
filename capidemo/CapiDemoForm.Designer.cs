namespace CAPIDemo
{
    partial class CapiDemoForm
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
            this.buttonLoginOne = new System.Windows.Forms.Button();
            this.buttonProfile = new System.Windows.Forms.Button();
            this.buttonMarket = new System.Windows.Forms.Button();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.buttonShipYard = new System.Windows.Forms.Button();
            this.buttonLoginTwo = new System.Windows.Forms.Button();
            this.buttonJournal = new System.Windows.Forms.Button();
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.SuspendLayout();
            // 
            // buttonLoginOne
            // 
            this.buttonLoginOne.Location = new System.Drawing.Point(47, 13);
            this.buttonLoginOne.Name = "buttonLoginOne";
            this.buttonLoginOne.Size = new System.Drawing.Size(75, 23);
            this.buttonLoginOne.TabIndex = 0;
            this.buttonLoginOne.Text = "Login One";
            this.buttonLoginOne.UseVisualStyleBackColor = true;
            this.buttonLoginOne.Click += new System.EventHandler(this.buttonLoginOne_Click);
            // 
            // buttonProfile
            // 
            this.buttonProfile.Location = new System.Drawing.Point(47, 116);
            this.buttonProfile.Name = "buttonProfile";
            this.buttonProfile.Size = new System.Drawing.Size(75, 23);
            this.buttonProfile.TabIndex = 1;
            this.buttonProfile.Text = "Profile";
            this.buttonProfile.UseVisualStyleBackColor = true;
            this.buttonProfile.Click += new System.EventHandler(this.buttonProfile_Click);
            // 
            // buttonMarket
            // 
            this.buttonMarket.Location = new System.Drawing.Point(47, 167);
            this.buttonMarket.Name = "buttonMarket";
            this.buttonMarket.Size = new System.Drawing.Size(75, 23);
            this.buttonMarket.TabIndex = 2;
            this.buttonMarket.Text = "Market";
            this.buttonMarket.UseVisualStyleBackColor = true;
            this.buttonMarket.Click += new System.EventHandler(this.buttonMarket_Click);
            // 
            // richTextBox
            // 
            this.richTextBox.Location = new System.Drawing.Point(137, 12);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(1085, 796);
            this.richTextBox.TabIndex = 3;
            this.richTextBox.Text = "";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(47, 392);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 2;
            this.button4.Text = "Logout";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.buttonLogout_Click);
            // 
            // buttonShipYard
            // 
            this.buttonShipYard.Location = new System.Drawing.Point(47, 216);
            this.buttonShipYard.Name = "buttonShipYard";
            this.buttonShipYard.Size = new System.Drawing.Size(75, 23);
            this.buttonShipYard.TabIndex = 2;
            this.buttonShipYard.Text = "Shipyard";
            this.buttonShipYard.UseVisualStyleBackColor = true;
            this.buttonShipYard.Click += new System.EventHandler(this.buttonShipyard_Click);
            // 
            // buttonLoginTwo
            // 
            this.buttonLoginTwo.Location = new System.Drawing.Point(47, 58);
            this.buttonLoginTwo.Name = "buttonLoginTwo";
            this.buttonLoginTwo.Size = new System.Drawing.Size(75, 23);
            this.buttonLoginTwo.TabIndex = 0;
            this.buttonLoginTwo.Text = "Login Two";
            this.buttonLoginTwo.UseVisualStyleBackColor = true;
            this.buttonLoginTwo.Click += new System.EventHandler(this.buttonLoginTwo_Click);
            // 
            // buttonJournal
            // 
            this.buttonJournal.Location = new System.Drawing.Point(47, 286);
            this.buttonJournal.Name = "buttonJournal";
            this.buttonJournal.Size = new System.Drawing.Size(75, 23);
            this.buttonJournal.TabIndex = 2;
            this.buttonJournal.Text = "Journal";
            this.buttonJournal.UseVisualStyleBackColor = true;
            this.buttonJournal.Click += new System.EventHandler(this.buttonjournal_Click);
            // 
            // dateTimePicker
            // 
            this.dateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker.Location = new System.Drawing.Point(21, 260);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.Size = new System.Drawing.Size(101, 20);
            this.dateTimePicker.TabIndex = 4;
            // 
            // CapiDemoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1260, 820);
            this.Controls.Add(this.dateTimePicker);
            this.Controls.Add(this.richTextBox);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.buttonJournal);
            this.Controls.Add(this.buttonShipYard);
            this.Controls.Add(this.buttonMarket);
            this.Controls.Add(this.buttonProfile);
            this.Controls.Add(this.buttonLoginTwo);
            this.Controls.Add(this.buttonLoginOne);
            this.Name = "CapiDemoForm";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonLoginOne;
        private System.Windows.Forms.Button buttonProfile;
        private System.Windows.Forms.Button buttonMarket;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button buttonShipYard;
        private System.Windows.Forms.Button buttonLoginTwo;
        private System.Windows.Forms.Button buttonJournal;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
    }
}

