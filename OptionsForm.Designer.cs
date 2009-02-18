namespace dnGREP
{
	partial class OptionsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsForm));
			this.grShell = new System.Windows.Forms.GroupBox();
			this.cbRegisterShell = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.rbSpecificEditor = new System.Windows.Forms.RadioButton();
			this.rbDefaultEditor = new System.Windows.Forms.RadioButton();
			this.btnClose = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.label2 = new System.Windows.Forms.Label();
			this.tbEditorArgs = new System.Windows.Forms.TextBox();
			this.tbEditorPath = new System.Windows.Forms.TextBox();
			this.grShell.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// grShell
			// 
			this.grShell.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grShell.Controls.Add(this.cbRegisterShell);
			this.grShell.Location = new System.Drawing.Point(3, 5);
			this.grShell.Name = "grShell";
			this.grShell.Size = new System.Drawing.Size(454, 47);
			this.grShell.TabIndex = 0;
			this.grShell.TabStop = false;
			this.grShell.Text = "Shell integration";
			// 
			// cbRegisterShell
			// 
			this.cbRegisterShell.AutoSize = true;
			this.cbRegisterShell.Location = new System.Drawing.Point(9, 19);
			this.cbRegisterShell.Name = "cbRegisterShell";
			this.cbRegisterShell.Size = new System.Drawing.Size(199, 17);
			this.cbRegisterShell.TabIndex = 0;
			this.cbRegisterShell.Text = "Enable Windows Explorer integration";
			this.cbRegisterShell.UseVisualStyleBackColor = true;
			this.cbRegisterShell.CheckedChanged += new System.EventHandler(this.cbRegisterShell_CheckedChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.tbEditorArgs);
			this.groupBox1.Controls.Add(this.btnBrowse);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.tbEditorPath);
			this.groupBox1.Controls.Add(this.rbSpecificEditor);
			this.groupBox1.Controls.Add(this.rbDefaultEditor);
			this.groupBox1.Location = new System.Drawing.Point(3, 58);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(454, 118);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Editor";
			// 
			// btnBrowse
			// 
			this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowse.Location = new System.Drawing.Point(415, 39);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new System.Drawing.Size(30, 23);
			this.btnBrowse.TabIndex = 4;
			this.btnBrowse.Text = "...";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(105, 95);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(340, 21);
			this.label1.TabIndex = 3;
			this.label1.Text = "(use %file and %line keywords to specify file location and line number)";
			// 
			// rbSpecificEditor
			// 
			this.rbSpecificEditor.AutoSize = true;
			this.rbSpecificEditor.Location = new System.Drawing.Point(9, 42);
			this.rbSpecificEditor.Name = "rbSpecificEditor";
			this.rbSpecificEditor.Size = new System.Drawing.Size(92, 17);
			this.rbSpecificEditor.TabIndex = 1;
			this.rbSpecificEditor.TabStop = true;
			this.rbSpecificEditor.Text = "Custom editor:";
			this.rbSpecificEditor.UseVisualStyleBackColor = true;
			// 
			// rbDefaultEditor
			// 
			this.rbDefaultEditor.AutoSize = true;
			this.rbDefaultEditor.Location = new System.Drawing.Point(9, 19);
			this.rbDefaultEditor.Name = "rbDefaultEditor";
			this.rbDefaultEditor.Size = new System.Drawing.Size(143, 17);
			this.rbDefaultEditor.TabIndex = 0;
			this.rbDefaultEditor.TabStop = true;
			this.rbDefaultEditor.Text = "Default file specific editor";
			this.rbDefaultEditor.UseVisualStyleBackColor = true;
			this.rbDefaultEditor.CheckedChanged += new System.EventHandler(this.rbEditorCheckedChanged);
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.Location = new System.Drawing.Point(381, 181);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 2;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			// 
			// openFileDialog
			// 
			this.openFileDialog.Title = "Path to custom editor...";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(105, 71);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(57, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Arguments";
			// 
			// tbEditorArgs
			// 
			this.tbEditorArgs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbEditorArgs.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "CustomEditorArgs", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbEditorArgs.Location = new System.Drawing.Point(168, 68);
			this.tbEditorArgs.Name = "tbEditorArgs";
			this.tbEditorArgs.Size = new System.Drawing.Size(241, 20);
			this.tbEditorArgs.TabIndex = 5;
			this.tbEditorArgs.Text = global::dnGREP.Properties.Settings.Default.CustomEditorArgs;
			// 
			// tbEditorPath
			// 
			this.tbEditorPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbEditorPath.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "CustomEditor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbEditorPath.Location = new System.Drawing.Point(108, 41);
			this.tbEditorPath.Name = "tbEditorPath";
			this.tbEditorPath.Size = new System.Drawing.Size(301, 20);
			this.tbEditorPath.TabIndex = 2;
			this.tbEditorPath.Text = global::dnGREP.Properties.Settings.Default.CustomEditor;
			// 
			// OptionsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(458, 208);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.grShell);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.Name = "OptionsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.Load += new System.EventHandler(this.OptionsForm_Load);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OptionsForm_FormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formKeyDown);
			this.grShell.ResumeLayout(false);
			this.grShell.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox grShell;
		private System.Windows.Forms.CheckBox cbRegisterShell;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbEditorPath;
		private System.Windows.Forms.RadioButton rbSpecificEditor;
		private System.Windows.Forms.RadioButton rbDefaultEditor;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox tbEditorArgs;
	}
}