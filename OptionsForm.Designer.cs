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
			this.grEditor = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.rbSpecificEditor = new System.Windows.Forms.RadioButton();
			this.rbDefaultEditor = new System.Windows.Forms.RadioButton();
			this.btnClose = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.grUpdate = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.cbCheckForUpdates = new System.Windows.Forms.CheckBox();
			this.grUI = new System.Windows.Forms.GroupBox();
			this.cbShowPath = new System.Windows.Forms.CheckBox();
			this.tbUpdateInterval = new System.Windows.Forms.MaskedTextBox();
			this.tbEditorArgs = new System.Windows.Forms.TextBox();
			this.tbEditorPath = new System.Windows.Forms.TextBox();
			this.grShell.SuspendLayout();
			this.grEditor.SuspendLayout();
			this.grUpdate.SuspendLayout();
			this.grUI.SuspendLayout();
			this.SuspendLayout();
			// 
			// grShell
			// 
			this.grShell.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grShell.Controls.Add(this.cbRegisterShell);
			this.grShell.Location = new System.Drawing.Point(3, 5);
			this.grShell.Name = "grShell";
			this.grShell.Size = new System.Drawing.Size(484, 47);
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
			// grEditor
			// 
			this.grEditor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grEditor.Controls.Add(this.label2);
			this.grEditor.Controls.Add(this.tbEditorArgs);
			this.grEditor.Controls.Add(this.btnBrowse);
			this.grEditor.Controls.Add(this.label1);
			this.grEditor.Controls.Add(this.tbEditorPath);
			this.grEditor.Controls.Add(this.rbSpecificEditor);
			this.grEditor.Controls.Add(this.rbDefaultEditor);
			this.grEditor.Location = new System.Drawing.Point(2, 149);
			this.grEditor.Name = "grEditor";
			this.grEditor.Size = new System.Drawing.Size(484, 118);
			this.grEditor.TabIndex = 1;
			this.grEditor.TabStop = false;
			this.grEditor.Text = "Editor";
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
			// btnBrowse
			// 
			this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowse.Location = new System.Drawing.Point(445, 39);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new System.Drawing.Size(30, 23);
			this.btnBrowse.TabIndex = 3;
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
			this.label1.Size = new System.Drawing.Size(370, 21);
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
			this.btnClose.Location = new System.Drawing.Point(411, 270);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 0;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			// 
			// openFileDialog
			// 
			this.openFileDialog.Title = "Path to custom editor...";
			// 
			// grUpdate
			// 
			this.grUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grUpdate.Controls.Add(this.tbUpdateInterval);
			this.grUpdate.Controls.Add(this.label3);
			this.grUpdate.Controls.Add(this.cbCheckForUpdates);
			this.grUpdate.Location = new System.Drawing.Point(3, 53);
			this.grUpdate.Name = "grUpdate";
			this.grUpdate.Size = new System.Drawing.Size(484, 47);
			this.grUpdate.TabIndex = 1;
			this.grUpdate.TabStop = false;
			this.grUpdate.Text = "Checking for updates";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(239, 20);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(29, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "days";
			// 
			// cbCheckForUpdates
			// 
			this.cbCheckForUpdates.AutoSize = true;
			this.cbCheckForUpdates.Location = new System.Drawing.Point(9, 19);
			this.cbCheckForUpdates.Name = "cbCheckForUpdates";
			this.cbCheckForUpdates.Size = new System.Drawing.Size(187, 17);
			this.cbCheckForUpdates.TabIndex = 0;
			this.cbCheckForUpdates.Text = "Enable automatic checking every ";
			this.cbCheckForUpdates.UseVisualStyleBackColor = true;
			this.cbCheckForUpdates.CheckedChanged += new System.EventHandler(this.cbCheckForUpdates_CheckedChanged);
			// 
			// grUI
			// 
			this.grUI.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grUI.Controls.Add(this.cbShowPath);
			this.grUI.Location = new System.Drawing.Point(2, 101);
			this.grUI.Name = "grUI";
			this.grUI.Size = new System.Drawing.Size(484, 47);
			this.grUI.TabIndex = 2;
			this.grUI.TabStop = false;
			this.grUI.Text = "User interface";
			// 
			// cbShowPath
			// 
			this.cbShowPath.AutoSize = true;
			this.cbShowPath.Location = new System.Drawing.Point(9, 19);
			this.cbShowPath.Name = "cbShowPath";
			this.cbShowPath.Size = new System.Drawing.Size(165, 17);
			this.cbShowPath.TabIndex = 0;
			this.cbShowPath.Text = "Show file path is results panel";
			this.cbShowPath.UseVisualStyleBackColor = true;
			this.cbShowPath.CheckedChanged += new System.EventHandler(this.cbShowPath_CheckedChanged);
			// 
			// tbUpdateInterval
			// 
			this.tbUpdateInterval.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "UpdateCheckInterval", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbUpdateInterval.InsertKeyMode = System.Windows.Forms.InsertKeyMode.Overwrite;
			this.tbUpdateInterval.Location = new System.Drawing.Point(194, 17);
			this.tbUpdateInterval.Mask = "000";
			this.tbUpdateInterval.Name = "tbUpdateInterval";
			this.tbUpdateInterval.Size = new System.Drawing.Size(40, 20);
			this.tbUpdateInterval.TabIndex = 1;
			this.tbUpdateInterval.Text = global::dnGREP.Properties.Settings.Default.UpdateCheckInterval;
			// 
			// tbEditorArgs
			// 
			this.tbEditorArgs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbEditorArgs.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "CustomEditorArgs", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbEditorArgs.Location = new System.Drawing.Point(168, 68);
			this.tbEditorArgs.Name = "tbEditorArgs";
			this.tbEditorArgs.Size = new System.Drawing.Size(271, 20);
			this.tbEditorArgs.TabIndex = 4;
			this.tbEditorArgs.Text = global::dnGREP.Properties.Settings.Default.CustomEditorArgs;
			// 
			// tbEditorPath
			// 
			this.tbEditorPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbEditorPath.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "CustomEditor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbEditorPath.Location = new System.Drawing.Point(108, 41);
			this.tbEditorPath.Name = "tbEditorPath";
			this.tbEditorPath.Size = new System.Drawing.Size(331, 20);
			this.tbEditorPath.TabIndex = 2;
			this.tbEditorPath.Text = global::dnGREP.Properties.Settings.Default.CustomEditor;
			// 
			// OptionsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(488, 297);
			this.Controls.Add(this.grUI);
			this.Controls.Add(this.grUpdate);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.grEditor);
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
			this.grEditor.ResumeLayout(false);
			this.grEditor.PerformLayout();
			this.grUpdate.ResumeLayout(false);
			this.grUpdate.PerformLayout();
			this.grUI.ResumeLayout(false);
			this.grUI.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox grShell;
		private System.Windows.Forms.CheckBox cbRegisterShell;
		private System.Windows.Forms.GroupBox grEditor;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbEditorPath;
		private System.Windows.Forms.RadioButton rbSpecificEditor;
		private System.Windows.Forms.RadioButton rbDefaultEditor;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox tbEditorArgs;
		private System.Windows.Forms.GroupBox grUpdate;
		private System.Windows.Forms.CheckBox cbCheckForUpdates;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.MaskedTextBox tbUpdateInterval;
		private System.Windows.Forms.GroupBox grUI;
		private System.Windows.Forms.CheckBox cbShowPath;
	}
}