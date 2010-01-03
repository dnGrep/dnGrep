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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsForm));
            this.grShell = new System.Windows.Forms.GroupBox();
            this.cbRegisterShell = new System.Windows.Forms.CheckBox();
            this.grEditor = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbEditorArgs = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tbEditorPath = new System.Windows.Forms.TextBox();
            this.rbSpecificEditor = new System.Windows.Forms.RadioButton();
            this.rbDefaultEditor = new System.Windows.Forms.RadioButton();
            this.btnClose = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.grUpdate = new System.Windows.Forms.GroupBox();
            this.tbUpdateInterval = new System.Windows.Forms.MaskedTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbCheckForUpdates = new System.Windows.Forms.CheckBox();
            this.grUI = new System.Windows.Forms.GroupBox();
            this.cbPreviewResults = new System.Windows.Forms.CheckBox();
            this.cbSearchFileNameOnly = new System.Windows.Forms.CheckBox();
            this.tbLinesAfter = new System.Windows.Forms.MaskedTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbLinesBefore = new System.Windows.Forms.MaskedTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbShowContext = new System.Windows.Forms.CheckBox();
            this.cbShowPath = new System.Windows.Forms.CheckBox();
            this.helpProvider = new System.Windows.Forms.HelpProvider();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
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
            this.grShell.Size = new System.Drawing.Size(486, 47);
            this.grShell.TabIndex = 0;
            this.grShell.TabStop = false;
            this.grShell.Text = "Shell integration";
            // 
            // cbRegisterShell
            // 
            this.cbRegisterShell.AutoSize = true;
            this.helpProvider.SetHelpKeyword(this.cbRegisterShell, "shell-integration.html");
            this.helpProvider.SetHelpNavigator(this.cbRegisterShell, System.Windows.Forms.HelpNavigator.Topic);
            this.cbRegisterShell.Location = new System.Drawing.Point(9, 19);
            this.cbRegisterShell.Name = "cbRegisterShell";
            this.helpProvider.SetShowHelp(this.cbRegisterShell, true);
            this.cbRegisterShell.Size = new System.Drawing.Size(199, 17);
            this.cbRegisterShell.TabIndex = 0;
            this.cbRegisterShell.Text = "Enable Windows Explorer integration";
            this.toolTip.SetToolTip(this.cbRegisterShell, "Shell integration enables running an application from shell context menu.");
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
            this.grEditor.Location = new System.Drawing.Point(2, 216);
            this.grEditor.Name = "grEditor";
            this.grEditor.Size = new System.Drawing.Size(486, 118);
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
            // tbEditorArgs
            // 
            this.tbEditorArgs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbEditorArgs.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "CustomEditorArgs", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.helpProvider.SetHelpKeyword(this.tbEditorArgs, "custom-editor-integration.html");
            this.helpProvider.SetHelpNavigator(this.tbEditorArgs, System.Windows.Forms.HelpNavigator.Topic);
            this.tbEditorArgs.Location = new System.Drawing.Point(168, 68);
            this.tbEditorArgs.Name = "tbEditorArgs";
            this.helpProvider.SetShowHelp(this.tbEditorArgs, true);
            this.tbEditorArgs.Size = new System.Drawing.Size(273, 20);
            this.tbEditorArgs.TabIndex = 4;
            this.tbEditorArgs.Text = global::dnGREP.Properties.Settings.Default.CustomEditorArgs;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.helpProvider.SetHelpKeyword(this.btnBrowse, "custom-editor-integration.html");
            this.helpProvider.SetHelpNavigator(this.btnBrowse, System.Windows.Forms.HelpNavigator.Topic);
            this.btnBrowse.Location = new System.Drawing.Point(447, 39);
            this.btnBrowse.Name = "btnBrowse";
            this.helpProvider.SetShowHelp(this.btnBrowse, true);
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
            this.label1.Size = new System.Drawing.Size(372, 21);
            this.label1.TabIndex = 3;
            this.label1.Text = "(use %file and %line keywords to specify file location and line number)";
            // 
            // tbEditorPath
            // 
            this.tbEditorPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbEditorPath.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "CustomEditor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.helpProvider.SetHelpKeyword(this.tbEditorPath, "custom-editor-integration.html");
            this.helpProvider.SetHelpNavigator(this.tbEditorPath, System.Windows.Forms.HelpNavigator.Topic);
            this.tbEditorPath.Location = new System.Drawing.Point(108, 41);
            this.tbEditorPath.Name = "tbEditorPath";
            this.helpProvider.SetShowHelp(this.tbEditorPath, true);
            this.tbEditorPath.Size = new System.Drawing.Size(333, 20);
            this.tbEditorPath.TabIndex = 2;
            this.tbEditorPath.Text = global::dnGREP.Properties.Settings.Default.CustomEditor;
            // 
            // rbSpecificEditor
            // 
            this.rbSpecificEditor.AutoSize = true;
            this.helpProvider.SetHelpKeyword(this.rbSpecificEditor, "custom-editor-integration.html");
            this.helpProvider.SetHelpNavigator(this.rbSpecificEditor, System.Windows.Forms.HelpNavigator.Topic);
            this.rbSpecificEditor.Location = new System.Drawing.Point(9, 42);
            this.rbSpecificEditor.Name = "rbSpecificEditor";
            this.helpProvider.SetShowHelp(this.rbSpecificEditor, true);
            this.rbSpecificEditor.Size = new System.Drawing.Size(92, 17);
            this.rbSpecificEditor.TabIndex = 1;
            this.rbSpecificEditor.TabStop = true;
            this.rbSpecificEditor.Text = "Custom editor:";
            this.rbSpecificEditor.UseVisualStyleBackColor = true;
            // 
            // rbDefaultEditor
            // 
            this.rbDefaultEditor.AutoSize = true;
            this.helpProvider.SetHelpKeyword(this.rbDefaultEditor, "custom-editor-integration.html");
            this.helpProvider.SetHelpNavigator(this.rbDefaultEditor, System.Windows.Forms.HelpNavigator.Topic);
            this.rbDefaultEditor.Location = new System.Drawing.Point(9, 19);
            this.rbDefaultEditor.Name = "rbDefaultEditor";
            this.helpProvider.SetShowHelp(this.rbDefaultEditor, true);
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
            this.btnClose.Location = new System.Drawing.Point(413, 345);
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
            this.grUpdate.Size = new System.Drawing.Size(486, 47);
            this.grUpdate.TabIndex = 1;
            this.grUpdate.TabStop = false;
            this.grUpdate.Text = "Checking for updates";
            // 
            // tbUpdateInterval
            // 
            this.tbUpdateInterval.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "UpdateCheckInterval", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.helpProvider.SetHelpKeyword(this.tbUpdateInterval, "automatic-update-notification.html");
            this.helpProvider.SetHelpNavigator(this.tbUpdateInterval, System.Windows.Forms.HelpNavigator.Topic);
            this.tbUpdateInterval.InsertKeyMode = System.Windows.Forms.InsertKeyMode.Overwrite;
            this.tbUpdateInterval.Location = new System.Drawing.Point(194, 17);
            this.tbUpdateInterval.Mask = "000";
            this.tbUpdateInterval.Name = "tbUpdateInterval";
            this.helpProvider.SetShowHelp(this.tbUpdateInterval, true);
            this.tbUpdateInterval.Size = new System.Drawing.Size(40, 20);
            this.tbUpdateInterval.TabIndex = 1;
            this.tbUpdateInterval.Text = global::dnGREP.Properties.Settings.Default.UpdateCheckInterval;
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
            this.helpProvider.SetHelpKeyword(this.cbCheckForUpdates, "automatic-update-notification.html");
            this.helpProvider.SetHelpNavigator(this.cbCheckForUpdates, System.Windows.Forms.HelpNavigator.Topic);
            this.cbCheckForUpdates.Location = new System.Drawing.Point(9, 19);
            this.cbCheckForUpdates.Name = "cbCheckForUpdates";
            this.helpProvider.SetShowHelp(this.cbCheckForUpdates, true);
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
            this.grUI.Controls.Add(this.cbPreviewResults);
            this.grUI.Controls.Add(this.cbSearchFileNameOnly);
            this.grUI.Controls.Add(this.tbLinesAfter);
            this.grUI.Controls.Add(this.label5);
            this.grUI.Controls.Add(this.tbLinesBefore);
            this.grUI.Controls.Add(this.label4);
            this.grUI.Controls.Add(this.cbShowContext);
            this.grUI.Controls.Add(this.cbShowPath);
            this.grUI.Location = new System.Drawing.Point(2, 101);
            this.grUI.Name = "grUI";
            this.grUI.Size = new System.Drawing.Size(486, 109);
            this.grUI.TabIndex = 2;
            this.grUI.TabStop = false;
            this.grUI.Text = "User interface";
            // 
            // cbPreviewResults
            // 
            this.cbPreviewResults.AutoSize = true;
            this.helpProvider.SetHelpKeyword(this.cbPreviewResults, "result-panel-customization.html");
            this.helpProvider.SetHelpNavigator(this.cbPreviewResults, System.Windows.Forms.HelpNavigator.Topic);
            this.cbPreviewResults.Location = new System.Drawing.Point(9, 88);
            this.cbPreviewResults.Name = "cbPreviewResults";
            this.helpProvider.SetShowHelp(this.cbPreviewResults, true);
            this.cbPreviewResults.Size = new System.Drawing.Size(203, 17);
            this.cbPreviewResults.TabIndex = 8;
            this.cbPreviewResults.Text = "Preview results during search (slower)";
            this.cbPreviewResults.UseVisualStyleBackColor = true;
            // 
            // cbSearchFileNameOnly
            // 
            this.cbSearchFileNameOnly.AutoSize = true;
            this.helpProvider.SetHelpKeyword(this.cbSearchFileNameOnly, "result-panel-customization.html");
            this.helpProvider.SetHelpNavigator(this.cbSearchFileNameOnly, System.Windows.Forms.HelpNavigator.Topic);
            this.cbSearchFileNameOnly.Location = new System.Drawing.Point(9, 65);
            this.cbSearchFileNameOnly.Name = "cbSearchFileNameOnly";
            this.helpProvider.SetShowHelp(this.cbSearchFileNameOnly, true);
            this.cbSearchFileNameOnly.Size = new System.Drawing.Size(348, 17);
            this.cbSearchFileNameOnly.TabIndex = 7;
            this.cbSearchFileNameOnly.Text = "Allow searching for file name pattern only when \"search for\" is empty";
            this.cbSearchFileNameOnly.UseVisualStyleBackColor = true;
            // 
            // tbLinesAfter
            // 
            this.helpProvider.SetHelpKeyword(this.tbLinesAfter, "result-panel-customization.html");
            this.helpProvider.SetHelpNavigator(this.tbLinesAfter, System.Windows.Forms.HelpNavigator.Topic);
            this.tbLinesAfter.InsertKeyMode = System.Windows.Forms.InsertKeyMode.Overwrite;
            this.tbLinesAfter.Location = new System.Drawing.Point(311, 40);
            this.tbLinesAfter.Mask = "000";
            this.tbLinesAfter.Name = "tbLinesAfter";
            this.helpProvider.SetShowHelp(this.tbLinesAfter, true);
            this.tbLinesAfter.Size = new System.Drawing.Size(40, 20);
            this.tbLinesAfter.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(356, 43);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "lines after";
            // 
            // tbLinesBefore
            // 
            this.helpProvider.SetHelpKeyword(this.tbLinesBefore, "result-panel-customization.html");
            this.helpProvider.SetHelpNavigator(this.tbLinesBefore, System.Windows.Forms.HelpNavigator.Topic);
            this.tbLinesBefore.InsertKeyMode = System.Windows.Forms.InsertKeyMode.Overwrite;
            this.tbLinesBefore.Location = new System.Drawing.Point(195, 39);
            this.tbLinesBefore.Mask = "000";
            this.tbLinesBefore.Name = "tbLinesBefore";
            this.helpProvider.SetShowHelp(this.tbLinesBefore, true);
            this.tbLinesBefore.Size = new System.Drawing.Size(40, 20);
            this.tbLinesBefore.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(240, 42);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "lines before";
            // 
            // cbShowContext
            // 
            this.cbShowContext.AutoSize = true;
            this.helpProvider.SetHelpKeyword(this.cbShowContext, "result-panel-customization.html");
            this.helpProvider.SetHelpNavigator(this.cbShowContext, System.Windows.Forms.HelpNavigator.Topic);
            this.cbShowContext.Location = new System.Drawing.Point(9, 42);
            this.cbShowContext.Name = "cbShowContext";
            this.helpProvider.SetShowHelp(this.cbShowContext, true);
            this.cbShowContext.Size = new System.Drawing.Size(154, 17);
            this.cbShowContext.TabIndex = 1;
            this.cbShowContext.Text = "Show result lines in context";
            this.cbShowContext.UseVisualStyleBackColor = true;
            this.cbShowContext.CheckedChanged += new System.EventHandler(this.cbShowContext_CheckedChanged);
            // 
            // cbShowPath
            // 
            this.cbShowPath.AutoSize = true;
            this.helpProvider.SetHelpKeyword(this.cbShowPath, "result-panel-customization.html");
            this.helpProvider.SetHelpNavigator(this.cbShowPath, System.Windows.Forms.HelpNavigator.Topic);
            this.cbShowPath.Location = new System.Drawing.Point(9, 19);
            this.cbShowPath.Name = "cbShowPath";
            this.helpProvider.SetShowHelp(this.cbShowPath, true);
            this.cbShowPath.Size = new System.Drawing.Size(165, 17);
            this.cbShowPath.TabIndex = 0;
            this.cbShowPath.Text = "Show file path is results panel";
            this.cbShowPath.UseVisualStyleBackColor = true;
            this.cbShowPath.CheckedChanged += new System.EventHandler(this.cbShowPath_CheckedChanged);
            // 
            // helpProvider
            // 
            this.helpProvider.HelpNamespace = "Doc\\dnGREP.chm";
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 300;
            this.toolTip.ReshowDelay = 100;
            this.toolTip.ShowAlways = true;
            // 
            // OptionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(490, 372);
            this.Controls.Add(this.grUI);
            this.Controls.Add(this.grUpdate);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.grEditor);
            this.Controls.Add(this.grShell);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.helpProvider.SetHelpKeyword(this, "Options.html");
            this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "OptionsForm";
            this.helpProvider.SetShowHelp(this, true);
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
		private System.Windows.Forms.MaskedTextBox tbLinesAfter;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.MaskedTextBox tbLinesBefore;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.CheckBox cbShowContext;
		private System.Windows.Forms.CheckBox cbSearchFileNameOnly;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.CheckBox cbPreviewResults;
		private System.Windows.Forms.ToolTip toolTip;
	}
}