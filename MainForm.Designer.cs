namespace nGREP
{
	partial class MainForm
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
			this.gbSearchIn = new System.Windows.Forms.GroupBox();
			this.btnSelectFolder = new System.Windows.Forms.Button();
			this.tbFolderName = new System.Windows.Forms.TextBox();
			this.gbSearchFor = new System.Windows.Forms.GroupBox();
			this.tbReplaceWith = new System.Windows.Forms.TextBox();
			this.tbSearchFor = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.rbTextSearch = new System.Windows.Forms.RadioButton();
			this.rbRegexSearch = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.btnSearch = new System.Windows.Forms.Button();
			this.gbFilter = new System.Windows.Forms.GroupBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.tbFileSizeTo = new System.Windows.Forms.TextBox();
			this.tbFileSizeFrom = new System.Windows.Forms.TextBox();
			this.rbFilterSpecificSize = new System.Windows.Forms.RadioButton();
			this.rbFilterAllSizes = new System.Windows.Forms.RadioButton();
			this.cbIncludeHiddenFolders = new System.Windows.Forms.CheckBox();
			this.cbIncludeSubfolders = new System.Windows.Forms.CheckBox();
			this.tbFilePattern = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.btnReplace = new System.Windows.Forms.Button();
			this.tvSearchResult = new System.Windows.Forms.TreeView();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.barProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.workerSearcher = new System.ComponentModel.BackgroundWorker();
			this.folderSelectDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.btnCancel = new System.Windows.Forms.Button();
			this.gbSearchIn.SuspendLayout();
			this.gbSearchFor.SuspendLayout();
			this.gbFilter.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// gbSearchIn
			// 
			this.gbSearchIn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gbSearchIn.Controls.Add(this.btnSelectFolder);
			this.gbSearchIn.Controls.Add(this.tbFolderName);
			this.gbSearchIn.Location = new System.Drawing.Point(6, 3);
			this.gbSearchIn.Name = "gbSearchIn";
			this.gbSearchIn.Size = new System.Drawing.Size(473, 52);
			this.gbSearchIn.TabIndex = 2;
			this.gbSearchIn.TabStop = false;
			this.gbSearchIn.Text = "Search in";
			// 
			// btnSelectFolder
			// 
			this.btnSelectFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSelectFolder.Location = new System.Drawing.Point(423, 17);
			this.btnSelectFolder.Name = "btnSelectFolder";
			this.btnSelectFolder.Size = new System.Drawing.Size(42, 23);
			this.btnSelectFolder.TabIndex = 0;
			this.btnSelectFolder.Text = "...";
			this.btnSelectFolder.UseVisualStyleBackColor = true;
			this.btnSelectFolder.Click += new System.EventHandler(this.btnSelectFolder_Click);
			// 
			// tbFolderName
			// 
			this.tbFolderName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbFolderName.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::nGREP.Properties.Settings.Default, "SearchFolder", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbFolderName.Location = new System.Drawing.Point(6, 19);
			this.tbFolderName.Name = "tbFolderName";
			this.tbFolderName.ReadOnly = true;
			this.tbFolderName.Size = new System.Drawing.Size(411, 20);
			this.tbFolderName.TabIndex = 0;
			this.tbFolderName.Text = global::nGREP.Properties.Settings.Default.SearchFolder;
			// 
			// gbSearchFor
			// 
			this.gbSearchFor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gbSearchFor.Controls.Add(this.tbReplaceWith);
			this.gbSearchFor.Controls.Add(this.tbSearchFor);
			this.gbSearchFor.Controls.Add(this.label2);
			this.gbSearchFor.Controls.Add(this.rbTextSearch);
			this.gbSearchFor.Controls.Add(this.rbRegexSearch);
			this.gbSearchFor.Controls.Add(this.label1);
			this.gbSearchFor.Location = new System.Drawing.Point(6, 61);
			this.gbSearchFor.Name = "gbSearchFor";
			this.gbSearchFor.Size = new System.Drawing.Size(473, 125);
			this.gbSearchFor.TabIndex = 0;
			this.gbSearchFor.TabStop = false;
			this.gbSearchFor.Text = "Search";
			// 
			// tbReplaceWith
			// 
			this.tbReplaceWith.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbReplaceWith.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::nGREP.Properties.Settings.Default, "ReplaceWith", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbReplaceWith.Location = new System.Drawing.Point(6, 92);
			this.tbReplaceWith.Name = "tbReplaceWith";
			this.tbReplaceWith.Size = new System.Drawing.Size(459, 20);
			this.tbReplaceWith.TabIndex = 1;
			this.tbReplaceWith.Text = global::nGREP.Properties.Settings.Default.ReplaceWith;
			// 
			// tbSearchFor
			// 
			this.tbSearchFor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbSearchFor.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::nGREP.Properties.Settings.Default, "SearchFor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbSearchFor.Location = new System.Drawing.Point(6, 53);
			this.tbSearchFor.Name = "tbSearchFor";
			this.tbSearchFor.Size = new System.Drawing.Size(459, 20);
			this.tbSearchFor.TabIndex = 0;
			this.tbSearchFor.Text = global::nGREP.Properties.Settings.Default.SearchFor;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 76);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Replace with:";
			// 
			// rbTextSearch
			// 
			this.rbTextSearch.AutoSize = true;
			this.rbTextSearch.Location = new System.Drawing.Point(71, 17);
			this.rbTextSearch.Name = "rbTextSearch";
			this.rbTextSearch.Size = new System.Drawing.Size(46, 17);
			this.rbTextSearch.TabIndex = 3;
			this.rbTextSearch.TabStop = true;
			this.rbTextSearch.Text = "Text";
			this.rbTextSearch.UseVisualStyleBackColor = true;
			// 
			// rbRegexSearch
			// 
			this.rbRegexSearch.AutoSize = true;
			this.rbRegexSearch.Location = new System.Drawing.Point(9, 17);
			this.rbRegexSearch.Name = "rbRegexSearch";
			this.rbRegexSearch.Size = new System.Drawing.Size(56, 17);
			this.rbRegexSearch.TabIndex = 2;
			this.rbRegexSearch.TabStop = true;
			this.rbRegexSearch.Text = "Regex";
			this.rbRegexSearch.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 37);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(59, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Search for:";
			// 
			// btnSearch
			// 
			this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSearch.Location = new System.Drawing.Point(323, 300);
			this.btnSearch.Name = "btnSearch";
			this.btnSearch.Size = new System.Drawing.Size(75, 23);
			this.btnSearch.TabIndex = 3;
			this.btnSearch.Text = "Search";
			this.btnSearch.UseVisualStyleBackColor = true;
			this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
			// 
			// gbFilter
			// 
			this.gbFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gbFilter.Controls.Add(this.label5);
			this.gbFilter.Controls.Add(this.label4);
			this.gbFilter.Controls.Add(this.tbFileSizeTo);
			this.gbFilter.Controls.Add(this.tbFileSizeFrom);
			this.gbFilter.Controls.Add(this.rbFilterSpecificSize);
			this.gbFilter.Controls.Add(this.rbFilterAllSizes);
			this.gbFilter.Controls.Add(this.cbIncludeHiddenFolders);
			this.gbFilter.Controls.Add(this.cbIncludeSubfolders);
			this.gbFilter.Controls.Add(this.tbFilePattern);
			this.gbFilter.Controls.Add(this.label3);
			this.gbFilter.Location = new System.Drawing.Point(6, 192);
			this.gbFilter.Name = "gbFilter";
			this.gbFilter.Size = new System.Drawing.Size(473, 102);
			this.gbFilter.TabIndex = 1;
			this.gbFilter.TabStop = false;
			this.gbFilter.Text = "Filter";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(194, 37);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(21, 13);
			this.label5.TabIndex = 9;
			this.label5.Text = "KB";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(119, 38);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(16, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "to";
			// 
			// tbFileSizeTo
			// 
			this.tbFileSizeTo.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::nGREP.Properties.Settings.Default, "SizeTo", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbFileSizeTo.Location = new System.Drawing.Point(141, 34);
			this.tbFileSizeTo.Name = "tbFileSizeTo";
			this.tbFileSizeTo.Size = new System.Drawing.Size(47, 20);
			this.tbFileSizeTo.TabIndex = 5;
			this.tbFileSizeTo.Text = global::nGREP.Properties.Settings.Default.SizeTo;
			// 
			// tbFileSizeFrom
			// 
			this.tbFileSizeFrom.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::nGREP.Properties.Settings.Default, "SizeFrom", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbFileSizeFrom.Location = new System.Drawing.Point(67, 34);
			this.tbFileSizeFrom.Name = "tbFileSizeFrom";
			this.tbFileSizeFrom.Size = new System.Drawing.Size(47, 20);
			this.tbFileSizeFrom.TabIndex = 4;
			this.tbFileSizeFrom.Text = global::nGREP.Properties.Settings.Default.SizeFrom;
			// 
			// rbFilterSpecificSize
			// 
			this.rbFilterSpecificSize.AutoSize = true;
			this.rbFilterSpecificSize.Location = new System.Drawing.Point(6, 35);
			this.rbFilterSpecificSize.Name = "rbFilterSpecificSize";
			this.rbFilterSpecificSize.Size = new System.Drawing.Size(55, 17);
			this.rbFilterSpecificSize.TabIndex = 1;
			this.rbFilterSpecificSize.TabStop = true;
			this.rbFilterSpecificSize.Text = "Size is";
			this.rbFilterSpecificSize.UseVisualStyleBackColor = true;
			this.rbFilterSpecificSize.CheckedChanged += new System.EventHandler(this.rbFilterSizes_CheckedChanged);
			// 
			// rbFilterAllSizes
			// 
			this.rbFilterAllSizes.AutoSize = true;
			this.rbFilterAllSizes.Location = new System.Drawing.Point(6, 15);
			this.rbFilterAllSizes.Name = "rbFilterAllSizes";
			this.rbFilterAllSizes.Size = new System.Drawing.Size(62, 17);
			this.rbFilterAllSizes.TabIndex = 0;
			this.rbFilterAllSizes.TabStop = true;
			this.rbFilterAllSizes.Text = "All sizes";
			this.rbFilterAllSizes.UseVisualStyleBackColor = true;
			this.rbFilterAllSizes.CheckedChanged += new System.EventHandler(this.rbFilterSizes_CheckedChanged);
			// 
			// cbIncludeHiddenFolders
			// 
			this.cbIncludeHiddenFolders.AutoSize = true;
			this.cbIncludeHiddenFolders.Checked = global::nGREP.Properties.Settings.Default.IncludeHidden;
			this.cbIncludeHiddenFolders.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::nGREP.Properties.Settings.Default, "IncludeHidden", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbIncludeHiddenFolders.Location = new System.Drawing.Point(6, 79);
			this.cbIncludeHiddenFolders.Name = "cbIncludeHiddenFolders";
			this.cbIncludeHiddenFolders.Size = new System.Drawing.Size(130, 17);
			this.cbIncludeHiddenFolders.TabIndex = 3;
			this.cbIncludeHiddenFolders.Text = "Include hidden folders";
			this.cbIncludeHiddenFolders.UseVisualStyleBackColor = true;
			// 
			// cbIncludeSubfolders
			// 
			this.cbIncludeSubfolders.AutoSize = true;
			this.cbIncludeSubfolders.Checked = global::nGREP.Properties.Settings.Default.IncludeSubfolder;
			this.cbIncludeSubfolders.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::nGREP.Properties.Settings.Default, "IncludeSubfolder", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbIncludeSubfolders.Location = new System.Drawing.Point(6, 60);
			this.cbIncludeSubfolders.Name = "cbIncludeSubfolders";
			this.cbIncludeSubfolders.Size = new System.Drawing.Size(112, 17);
			this.cbIncludeSubfolders.TabIndex = 2;
			this.cbIncludeSubfolders.Text = "Include subfolders";
			this.cbIncludeSubfolders.UseVisualStyleBackColor = true;
			// 
			// tbFilePattern
			// 
			this.tbFilePattern.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.tbFilePattern.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::nGREP.Properties.Settings.Default, "FilePattern", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbFilePattern.Location = new System.Drawing.Point(334, 14);
			this.tbFilePattern.Name = "tbFilePattern";
			this.tbFilePattern.Size = new System.Drawing.Size(131, 20);
			this.tbFilePattern.TabIndex = 6;
			this.tbFilePattern.Text = global::nGREP.Properties.Settings.Default.FilePattern;
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(244, 17);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(84, 13);
			this.label3.TabIndex = 0;
			this.label3.Text = "Files that match:";
			// 
			// btnReplace
			// 
			this.btnReplace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnReplace.Location = new System.Drawing.Point(242, 300);
			this.btnReplace.Name = "btnReplace";
			this.btnReplace.Size = new System.Drawing.Size(75, 23);
			this.btnReplace.TabIndex = 4;
			this.btnReplace.Text = "Replace";
			this.btnReplace.UseVisualStyleBackColor = true;
			// 
			// tvSearchResult
			// 
			this.tvSearchResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tvSearchResult.Location = new System.Drawing.Point(6, 329);
			this.tvSearchResult.Name = "tvSearchResult";
			this.tvSearchResult.Size = new System.Drawing.Size(472, 81);
			this.tvSearchResult.TabIndex = 6;
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.barProgressBar,
            this.lblStatus});
			this.statusStrip1.Location = new System.Drawing.Point(0, 413);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(483, 22);
			this.statusStrip1.TabIndex = 7;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// barProgressBar
			// 
			this.barProgressBar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
			this.barProgressBar.Name = "barProgressBar";
			this.barProgressBar.Size = new System.Drawing.Size(150, 16);
			// 
			// lblStatus
			// 
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(0, 17);
			// 
			// workerSearcher
			// 
			this.workerSearcher.WorkerReportsProgress = true;
			this.workerSearcher.WorkerSupportsCancellation = true;
			this.workerSearcher.DoWork += new System.ComponentModel.DoWorkEventHandler(this.doSearch);
			this.workerSearcher.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.searchComplete);
			this.workerSearcher.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.searchProgressChanged);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(404, 300);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// MainForm
			// 
			this.AcceptButton = this.btnSearch;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(483, 435);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.tvSearchResult);
			this.Controls.Add(this.btnReplace);
			this.Controls.Add(this.gbFilter);
			this.Controls.Add(this.btnSearch);
			this.Controls.Add(this.gbSearchFor);
			this.Controls.Add(this.gbSearchIn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(356, 407);
			this.Name = "MainForm";
			this.Text = "nGREP";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formKeyDown);
			this.gbSearchIn.ResumeLayout(false);
			this.gbSearchIn.PerformLayout();
			this.gbSearchFor.ResumeLayout(false);
			this.gbSearchFor.PerformLayout();
			this.gbFilter.ResumeLayout(false);
			this.gbFilter.PerformLayout();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox tbFolderName;
		private System.Windows.Forms.GroupBox gbSearchIn;
		private System.Windows.Forms.Button btnSelectFolder;
		private System.Windows.Forms.GroupBox gbSearchFor;
		private System.Windows.Forms.FolderBrowserDialog folderSelectDialog;
		private System.Windows.Forms.Button btnSearch;
		private System.Windows.Forms.RadioButton rbTextSearch;
		private System.Windows.Forms.RadioButton rbRegexSearch;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbReplaceWith;
		private System.Windows.Forms.TextBox tbSearchFor;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox gbFilter;
		private System.Windows.Forms.CheckBox cbIncludeHiddenFolders;
		private System.Windows.Forms.CheckBox cbIncludeSubfolders;
		private System.Windows.Forms.TextBox tbFilePattern;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox tbFileSizeTo;
		private System.Windows.Forms.TextBox tbFileSizeFrom;
		private System.Windows.Forms.RadioButton rbFilterSpecificSize;
		private System.Windows.Forms.RadioButton rbFilterAllSizes;
		private System.Windows.Forms.Button btnReplace;
		private System.Windows.Forms.TreeView tvSearchResult;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripProgressBar barProgressBar;
		private System.Windows.Forms.ToolStripStatusLabel lblStatus;
		private System.ComponentModel.BackgroundWorker workerSearcher;
		private System.Windows.Forms.Button btnCancel;
	}
}

