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
			dnGREP.Properties.Settings settings1 = new dnGREP.Properties.Settings();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsForm));
			this.grShell = new System.Windows.Forms.GroupBox();
			this.cbRegisterShell = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tbEditorPath = new System.Windows.Forms.TextBox();
			this.rbSpecificEditor = new System.Windows.Forms.RadioButton();
			this.rbDefaultEditor = new System.Windows.Forms.RadioButton();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
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
			this.grShell.Size = new System.Drawing.Size(402, 47);
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
			this.groupBox1.Controls.Add(this.btnBrowse);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.tbEditorPath);
			this.groupBox1.Controls.Add(this.rbSpecificEditor);
			this.groupBox1.Controls.Add(this.rbDefaultEditor);
			this.groupBox1.Location = new System.Drawing.Point(3, 58);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(402, 103);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Editor";
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(105, 64);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(288, 32);
			this.label1.TabIndex = 3;
			this.label1.Text = "(use %file and %line keywords to specify file location and line number)";
			// 
			// tbEditorPath
			// 
			this.tbEditorPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			settings1.CaseSensitive = true;
			settings1.CustomEditor = "";
			settings1.FilePattern = "";
			settings1.FilterAllSizes = true;
			settings1.FilterSpecificSize = false;
			settings1.IncludeHidden = false;
			settings1.IncludeSubfolder = true;
			settings1.Multiline = false;
			settings1.ReplaceWith = "";
			settings1.SearchFolder = "";
			settings1.SearchFor = "";
			settings1.SearchRegex = true;
			settings1.SearchText = false;
			settings1.SettingsKey = "";
			settings1.SizeFrom = "0";
			settings1.SizeTo = "1000";
			settings1.UseCustomEditor = false;
			this.tbEditorPath.DataBindings.Add(new System.Windows.Forms.Binding("Text", settings1, "CustomEditor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbEditorPath.Location = new System.Drawing.Point(108, 41);
			this.tbEditorPath.Name = "tbEditorPath";
			this.tbEditorPath.Size = new System.Drawing.Size(249, 20);
			this.tbEditorPath.TabIndex = 2;
			this.tbEditorPath.Text = settings1.CustomEditor;
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
			this.btnClose.Location = new System.Drawing.Point(317, 169);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 2;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			// 
			// btnBrowse
			// 
			this.btnBrowse.Location = new System.Drawing.Point(363, 39);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new System.Drawing.Size(30, 23);
			this.btnBrowse.TabIndex = 4;
			this.btnBrowse.Text = "...";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			// 
			// openFileDialog
			// 
			this.openFileDialog.Title = "Path to custom editor...";
			// 
			// OptionsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(406, 197);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.grShell);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MaximumSize = new System.Drawing.Size(500, 221);
			this.MinimumSize = new System.Drawing.Size(200, 221);
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
	}
}