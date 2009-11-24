namespace dnGREP
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.gbSearchIn = new System.Windows.Forms.GroupBox();
			this.btnSelectFolder = new System.Windows.Forms.Button();
			this.tbFolderName = new System.Windows.Forms.TextBox();
			this.gbSearchFor = new System.Windows.Forms.GroupBox();
			this.rbXPathSearch = new System.Windows.Forms.RadioButton();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.tbSearchFor = new System.Windows.Forms.TextBox();
			this.tbReplaceWith = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.cbMultiline = new System.Windows.Forms.CheckBox();
			this.btnTest = new System.Windows.Forms.Button();
			this.cbCaseSensitive = new System.Windows.Forms.CheckBox();
			this.rbTextSearch = new System.Windows.Forms.RadioButton();
			this.rbRegexSearch = new System.Windows.Forms.RadioButton();
			this.btnSearch = new wyDay.Controls.SplitButton();
			this.searchResultsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.searchInResultsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.gbFilter = new System.Windows.Forms.GroupBox();
			this.fileSearchPanel = new System.Windows.Forms.Panel();
			this.rbFileAsterisk = new System.Windows.Forms.RadioButton();
			this.rbFileRegex = new System.Windows.Forms.RadioButton();
			this.cbEncoding = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
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
			this.tvContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openContainingFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.barProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.workerSearchReplace = new System.ComponentModel.BackgroundWorker();
			this.folderSelectDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.btnCancel = new System.Windows.Forms.Button();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.parentHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label6 = new System.Windows.Forms.Label();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.btnBookmark = new wyDay.Controls.SplitButton();
			this.bookmarkMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.btnOtherActions = new wyDay.Controls.SplitButton();
			this.otherMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copyFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.moveFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.saveAsCSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.gbSearchIn.SuspendLayout();
			this.gbSearchFor.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.searchResultsMenu.SuspendLayout();
			this.gbFilter.SuspendLayout();
			this.fileSearchPanel.SuspendLayout();
			this.tvContextMenu.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.helpMenu.SuspendLayout();
			this.bookmarkMenu.SuspendLayout();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.otherMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// gbSearchIn
			// 
			this.gbSearchIn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gbSearchIn.Controls.Add(this.btnSelectFolder);
			this.gbSearchIn.Controls.Add(this.tbFolderName);
			this.gbSearchIn.Location = new System.Drawing.Point(6, 24);
			this.gbSearchIn.Name = "gbSearchIn";
			this.gbSearchIn.Size = new System.Drawing.Size(488, 54);
			this.gbSearchIn.TabIndex = 2;
			this.gbSearchIn.TabStop = false;
			this.gbSearchIn.Text = "Search in";
			// 
			// btnSelectFolder
			// 
			this.btnSelectFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSelectFolder.Location = new System.Drawing.Point(438, 17);
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
			this.tbFolderName.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "SearchFolder", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbFolderName.Location = new System.Drawing.Point(6, 19);
			this.tbFolderName.Name = "tbFolderName";
			this.tbFolderName.ReadOnly = true;
			this.tbFolderName.Size = new System.Drawing.Size(426, 20);
			this.tbFolderName.TabIndex = 0;
			this.tbFolderName.Text = global::dnGREP.Properties.Settings.Default.SearchFolder;
			// 
			// gbSearchFor
			// 
			this.gbSearchFor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gbSearchFor.Controls.Add(this.rbXPathSearch);
			this.gbSearchFor.Controls.Add(this.tableLayoutPanel1);
			this.gbSearchFor.Controls.Add(this.cbMultiline);
			this.gbSearchFor.Controls.Add(this.btnTest);
			this.gbSearchFor.Controls.Add(this.cbCaseSensitive);
			this.gbSearchFor.Controls.Add(this.rbTextSearch);
			this.gbSearchFor.Controls.Add(this.rbRegexSearch);
			this.gbSearchFor.Location = new System.Drawing.Point(6, 0);
			this.gbSearchFor.Name = "gbSearchFor";
			this.gbSearchFor.Size = new System.Drawing.Size(487, 160);
			this.gbSearchFor.TabIndex = 0;
			this.gbSearchFor.TabStop = false;
			this.gbSearchFor.Text = "Search";
			// 
			// rbXPathSearch
			// 
			this.rbXPathSearch.AutoSize = true;
			this.rbXPathSearch.Location = new System.Drawing.Point(64, 17);
			this.rbXPathSearch.Name = "rbXPathSearch";
			this.rbXPathSearch.Size = new System.Drawing.Size(54, 17);
			this.rbXPathSearch.TabIndex = 1;
			this.rbXPathSearch.TabStop = true;
			this.rbXPathSearch.Text = "XPath";
			this.rbXPathSearch.UseVisualStyleBackColor = true;
			this.rbXPathSearch.CheckedChanged += new System.EventHandler(this.regexText_CheckedChanged);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.tbSearchFor, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.tbReplaceWith, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(2, 40);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(479, 118);
			this.tableLayoutPanel1.TabIndex = 7;
			// 
			// tbSearchFor
			// 
			this.tbSearchFor.AcceptsReturn = true;
			this.tbSearchFor.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "SearchFor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbSearchFor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpKeyword(this.tbSearchFor, "regular-expressions.html");
			this.helpProvider.SetHelpNavigator(this.tbSearchFor, System.Windows.Forms.HelpNavigator.Topic);
			this.helpProvider.SetHelpString(this.tbSearchFor, "");
			this.tbSearchFor.Location = new System.Drawing.Point(3, 19);
			this.tbSearchFor.MaxLength = 327670;
			this.tbSearchFor.Multiline = true;
			this.tbSearchFor.Name = "tbSearchFor";
			this.tbSearchFor.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.helpProvider.SetShowHelp(this.tbSearchFor, true);
			this.tbSearchFor.Size = new System.Drawing.Size(473, 37);
			this.tbSearchFor.TabIndex = 0;
			this.tbSearchFor.Text = global::dnGREP.Properties.Settings.Default.SearchFor;
			this.toolTip.SetToolTip(this.tbSearchFor, ". matches all characters\r\n\\w matches alpha-numerics\r\n\\d matches digits\r\n\\s matche" +
					"s space\r\n* matches any number of characters\r\n{1,3} matches 1 to 3 characters\r\nFo" +
					"r more Regex patterns Google \"Regex\"");
			this.tbSearchFor.TextChanged += new System.EventHandler(this.textBoxTextChanged);
			this.tbSearchFor.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.onHelpRequested);
			// 
			// tbReplaceWith
			// 
			this.tbReplaceWith.AcceptsReturn = true;
			this.tbReplaceWith.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "ReplaceWith", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbReplaceWith.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbReplaceWith.Location = new System.Drawing.Point(3, 78);
			this.tbReplaceWith.MaxLength = 327670;
			this.tbReplaceWith.Multiline = true;
			this.tbReplaceWith.Name = "tbReplaceWith";
			this.tbReplaceWith.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbReplaceWith.Size = new System.Drawing.Size(473, 37);
			this.tbReplaceWith.TabIndex = 1;
			this.tbReplaceWith.Text = global::dnGREP.Properties.Settings.Default.ReplaceWith;
			this.tbReplaceWith.TextChanged += new System.EventHandler(this.textBoxTextChanged);
			// 
			// label1
			// 
			this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(473, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Search for:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label2
			// 
			this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label2.Location = new System.Drawing.Point(3, 59);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(473, 16);
			this.label2.TabIndex = 3;
			this.label2.Text = "Replace with:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// cbMultiline
			// 
			this.cbMultiline.AutoSize = true;
			this.cbMultiline.Checked = global::dnGREP.Properties.Settings.Default.Multiline;
			this.cbMultiline.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::dnGREP.Properties.Settings.Default, "Multiline", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbMultiline.Location = new System.Drawing.Point(279, 17);
			this.cbMultiline.Name = "cbMultiline";
			this.cbMultiline.Size = new System.Drawing.Size(103, 17);
			this.cbMultiline.TabIndex = 4;
			this.cbMultiline.Text = "Multiline (slower)";
			this.cbMultiline.UseVisualStyleBackColor = true;
			this.cbMultiline.CheckedChanged += new System.EventHandler(this.cbMultiline_CheckedChanged);
			// 
			// btnTest
			// 
			this.btnTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnTest.Location = new System.Drawing.Point(414, 17);
			this.btnTest.Name = "btnTest";
			this.btnTest.Size = new System.Drawing.Size(67, 23);
			this.btnTest.TabIndex = 5;
			this.btnTest.Text = "Test";
			this.btnTest.UseVisualStyleBackColor = true;
			this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
			// 
			// cbCaseSensitive
			// 
			this.cbCaseSensitive.AutoSize = true;
			this.cbCaseSensitive.Checked = global::dnGREP.Properties.Settings.Default.CaseSensitive;
			this.cbCaseSensitive.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbCaseSensitive.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::dnGREP.Properties.Settings.Default, "CaseSensitive", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbCaseSensitive.Location = new System.Drawing.Point(179, 17);
			this.cbCaseSensitive.Name = "cbCaseSensitive";
			this.cbCaseSensitive.Size = new System.Drawing.Size(94, 17);
			this.cbCaseSensitive.TabIndex = 3;
			this.cbCaseSensitive.Text = "Case sensitive";
			this.cbCaseSensitive.UseVisualStyleBackColor = true;
			this.cbCaseSensitive.CheckedChanged += new System.EventHandler(this.cbCaseSensitive_CheckedChanged);
			// 
			// rbTextSearch
			// 
			this.rbTextSearch.AutoSize = true;
			this.rbTextSearch.Location = new System.Drawing.Point(119, 17);
			this.rbTextSearch.Name = "rbTextSearch";
			this.rbTextSearch.Size = new System.Drawing.Size(46, 17);
			this.rbTextSearch.TabIndex = 2;
			this.rbTextSearch.TabStop = true;
			this.rbTextSearch.Text = "Text";
			this.rbTextSearch.UseVisualStyleBackColor = true;
			this.rbTextSearch.CheckedChanged += new System.EventHandler(this.regexText_CheckedChanged);
			// 
			// rbRegexSearch
			// 
			this.rbRegexSearch.AutoSize = true;
			this.rbRegexSearch.Location = new System.Drawing.Point(9, 17);
			this.rbRegexSearch.Name = "rbRegexSearch";
			this.rbRegexSearch.Size = new System.Drawing.Size(56, 17);
			this.rbRegexSearch.TabIndex = 0;
			this.rbRegexSearch.TabStop = true;
			this.rbRegexSearch.Text = "Regex";
			this.rbRegexSearch.UseVisualStyleBackColor = true;
			this.rbRegexSearch.CheckedChanged += new System.EventHandler(this.regexText_CheckedChanged);
			// 
			// btnSearch
			// 
			this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSearch.AutoSize = true;
			this.btnSearch.ContextMenuStrip = this.searchResultsMenu;
			this.btnSearch.Location = new System.Drawing.Point(202, 107);
			this.btnSearch.Name = "btnSearch";
			this.btnSearch.Size = new System.Drawing.Size(75, 23);
			this.btnSearch.SplitMenuStrip = this.searchResultsMenu;
			this.btnSearch.TabIndex = 3;
			this.btnSearch.Text = "Search";
			this.btnSearch.UseVisualStyleBackColor = true;
			this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
			// 
			// searchResultsMenu
			// 
			this.searchResultsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.searchInResultsToolStripMenuItem});
			this.searchResultsMenu.Name = "contextMenuStrip1";
			this.searchResultsMenu.ShowImageMargin = false;
			this.searchResultsMenu.Size = new System.Drawing.Size(140, 26);
			// 
			// searchInResultsToolStripMenuItem
			// 
			this.searchInResultsToolStripMenuItem.Name = "searchInResultsToolStripMenuItem";
			this.searchInResultsToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
			this.searchInResultsToolStripMenuItem.Text = "Search in results";
			this.searchInResultsToolStripMenuItem.Click += new System.EventHandler(this.searchInResultsToolStripMenuItem_Click);
			// 
			// gbFilter
			// 
			this.gbFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gbFilter.Controls.Add(this.fileSearchPanel);
			this.gbFilter.Controls.Add(this.cbEncoding);
			this.gbFilter.Controls.Add(this.label7);
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
			this.gbFilter.Location = new System.Drawing.Point(6, -1);
			this.gbFilter.Name = "gbFilter";
			this.gbFilter.Size = new System.Drawing.Size(488, 103);
			this.gbFilter.TabIndex = 1;
			this.gbFilter.TabStop = false;
			this.gbFilter.Text = "Filter";
			// 
			// fileSearchPanel
			// 
			this.fileSearchPanel.Controls.Add(this.rbFileAsterisk);
			this.fileSearchPanel.Controls.Add(this.rbFileRegex);
			this.fileSearchPanel.Location = new System.Drawing.Point(327, 35);
			this.fileSearchPanel.Name = "fileSearchPanel";
			this.fileSearchPanel.Size = new System.Drawing.Size(158, 17);
			this.fileSearchPanel.TabIndex = 11;
			// 
			// rbFileAsterisk
			// 
			this.rbFileAsterisk.AutoSize = true;
			this.rbFileAsterisk.Location = new System.Drawing.Point(57, 1);
			this.rbFileAsterisk.Name = "rbFileAsterisk";
			this.rbFileAsterisk.Size = new System.Drawing.Size(98, 17);
			this.rbFileAsterisk.TabIndex = 1;
			this.rbFileAsterisk.TabStop = true;
			this.rbFileAsterisk.Text = "Asterisk pattern";
			this.toolTip.SetToolTip(this.rbFileAsterisk, "e.g. file??.*");
			this.rbFileAsterisk.UseVisualStyleBackColor = true;
			this.rbFileAsterisk.CheckedChanged += new System.EventHandler(this.regexText_CheckedChanged);
			// 
			// rbFileRegex
			// 
			this.rbFileRegex.AutoSize = true;
			this.rbFileRegex.Location = new System.Drawing.Point(2, 1);
			this.rbFileRegex.Name = "rbFileRegex";
			this.rbFileRegex.Size = new System.Drawing.Size(56, 17);
			this.rbFileRegex.TabIndex = 0;
			this.rbFileRegex.Text = "Regex";
			this.toolTip.SetToolTip(this.rbFileRegex, "e.g. file[0-9]{1,2}\\.txt");
			this.rbFileRegex.UseVisualStyleBackColor = true;
			this.rbFileRegex.CheckedChanged += new System.EventHandler(this.regexText_CheckedChanged);
			// 
			// cbEncoding
			// 
			this.cbEncoding.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cbEncoding.DisplayMember = "Auto detection (default)";
			this.cbEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbEncoding.FormattingEnabled = true;
			this.cbEncoding.Location = new System.Drawing.Point(327, 56);
			this.cbEncoding.Name = "cbEncoding";
			this.cbEncoding.Size = new System.Drawing.Size(153, 21);
			this.cbEncoding.TabIndex = 8;
			this.cbEncoding.ValueMember = "Auto detection (default)";
			this.cbEncoding.SelectedIndexChanged += new System.EventHandler(this.cbEncoding_SelectedIndexChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(266, 59);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(55, 13);
			this.label7.TabIndex = 10;
			this.label7.Text = "Encoding:";
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
			this.tbFileSizeTo.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "SizeTo", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbFileSizeTo.Location = new System.Drawing.Point(141, 34);
			this.tbFileSizeTo.Name = "tbFileSizeTo";
			this.tbFileSizeTo.Size = new System.Drawing.Size(47, 20);
			this.tbFileSizeTo.TabIndex = 5;
			this.tbFileSizeTo.Text = global::dnGREP.Properties.Settings.Default.SizeTo;
			// 
			// tbFileSizeFrom
			// 
			this.tbFileSizeFrom.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "SizeFrom", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbFileSizeFrom.Location = new System.Drawing.Point(67, 34);
			this.tbFileSizeFrom.Name = "tbFileSizeFrom";
			this.tbFileSizeFrom.Size = new System.Drawing.Size(47, 20);
			this.tbFileSizeFrom.TabIndex = 4;
			this.tbFileSizeFrom.Text = global::dnGREP.Properties.Settings.Default.SizeFrom;
			// 
			// rbFilterSpecificSize
			// 
			this.rbFilterSpecificSize.AutoSize = true;
			this.rbFilterSpecificSize.Location = new System.Drawing.Point(6, 35);
			this.rbFilterSpecificSize.Name = "rbFilterSpecificSize";
			this.rbFilterSpecificSize.Size = new System.Drawing.Size(55, 17);
			this.rbFilterSpecificSize.TabIndex = 1;
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
			this.rbFilterAllSizes.Text = "All sizes";
			this.rbFilterAllSizes.UseVisualStyleBackColor = true;
			this.rbFilterAllSizes.CheckedChanged += new System.EventHandler(this.rbFilterSizes_CheckedChanged);
			// 
			// cbIncludeHiddenFolders
			// 
			this.cbIncludeHiddenFolders.AutoSize = true;
			this.cbIncludeHiddenFolders.Checked = global::dnGREP.Properties.Settings.Default.IncludeHidden;
			this.cbIncludeHiddenFolders.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::dnGREP.Properties.Settings.Default, "IncludeHidden", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
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
			this.cbIncludeSubfolders.Checked = global::dnGREP.Properties.Settings.Default.IncludeSubfolder;
			this.cbIncludeSubfolders.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbIncludeSubfolders.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::dnGREP.Properties.Settings.Default, "IncludeSubfolder", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbIncludeSubfolders.Location = new System.Drawing.Point(6, 60);
			this.cbIncludeSubfolders.Name = "cbIncludeSubfolders";
			this.cbIncludeSubfolders.Size = new System.Drawing.Size(112, 17);
			this.cbIncludeSubfolders.TabIndex = 2;
			this.cbIncludeSubfolders.Text = "Include subfolders";
			this.cbIncludeSubfolders.UseVisualStyleBackColor = true;
			// 
			// tbFilePattern
			// 
			this.tbFilePattern.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbFilePattern.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "FilePattern", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.helpProvider.SetHelpKeyword(this.tbFilePattern, "searchreplace.html");
			this.helpProvider.SetHelpNavigator(this.tbFilePattern, System.Windows.Forms.HelpNavigator.Topic);
			this.tbFilePattern.Location = new System.Drawing.Point(327, 14);
			this.tbFilePattern.Name = "tbFilePattern";
			this.helpProvider.SetShowHelp(this.tbFilePattern, true);
			this.tbFilePattern.Size = new System.Drawing.Size(153, 20);
			this.tbFilePattern.TabIndex = 7;
			this.tbFilePattern.Text = global::dnGREP.Properties.Settings.Default.FilePattern;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(237, 17);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(84, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Files that match:";
			// 
			// btnReplace
			// 
			this.btnReplace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnReplace.Location = new System.Drawing.Point(283, 107);
			this.btnReplace.Name = "btnReplace";
			this.btnReplace.Size = new System.Drawing.Size(75, 23);
			this.btnReplace.TabIndex = 4;
			this.btnReplace.Text = "Replace";
			this.btnReplace.UseVisualStyleBackColor = true;
			this.btnReplace.Click += new System.EventHandler(this.btnReplace_Click);
			// 
			// tvSearchResult
			// 
			this.tvSearchResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tvSearchResult.ContextMenuStrip = this.tvContextMenu;
			this.tvSearchResult.Location = new System.Drawing.Point(4, 136);
			this.tvSearchResult.Name = "tvSearchResult";
			this.tvSearchResult.ShowLines = false;
			this.tvSearchResult.Size = new System.Drawing.Size(488, 58);
			this.tvSearchResult.TabIndex = 6;
			this.tvSearchResult.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvSearchResult_NodeMouseDoubleClick);
			this.tvSearchResult.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvSearchResult_NodeMouseClick);
			// 
			// tvContextMenu
			// 
			this.tvContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.openContainingFolderToolStripMenuItem,
            this.toolStripSeparator2,
            this.expandAllToolStripMenuItem});
			this.tvContextMenu.Name = "tvContextMenu";
			this.tvContextMenu.Size = new System.Drawing.Size(195, 76);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Image = global::dnGREP.Properties.Resources.OpenSelectedItemHS;
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// openContainingFolderToolStripMenuItem
			// 
			this.openContainingFolderToolStripMenuItem.Image = global::dnGREP.Properties.Resources.openHS;
			this.openContainingFolderToolStripMenuItem.Name = "openContainingFolderToolStripMenuItem";
			this.openContainingFolderToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
			this.openContainingFolderToolStripMenuItem.Text = "Open containing &folder";
			this.openContainingFolderToolStripMenuItem.Click += new System.EventHandler(this.openContainingFolderToolStripMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(191, 6);
			// 
			// expandAllToolStripMenuItem
			// 
			this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
			this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(194, 22);
			this.expandAllToolStripMenuItem.Text = "Expand all";
			this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.barProgressBar,
            this.lblStatus});
			this.statusStrip1.Location = new System.Drawing.Point(0, 441);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(498, 22);
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
			// workerSearchReplace
			// 
			this.workerSearchReplace.WorkerReportsProgress = true;
			this.workerSearchReplace.WorkerSupportsCancellation = true;
			this.workerSearchReplace.DoWork += new System.ComponentModel.DoWorkEventHandler(this.doSearchReplace);
			this.workerSearchReplace.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.searchComplete);
			this.workerSearchReplace.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.searchProgressChanged);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(418, 107);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// menuStrip1
			// 
			this.menuStrip1.BackColor = System.Drawing.SystemColors.ButtonFace;
			this.menuStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.parentHelpToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.undoToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.menuStrip1.Size = new System.Drawing.Size(498, 24);
			this.menuStrip1.TabIndex = 9;
			this.menuStrip1.Text = "topMenuStrip";
			// 
			// parentHelpToolStripMenuItem
			// 
			this.parentHelpToolStripMenuItem.DropDown = this.helpMenu;
			this.parentHelpToolStripMenuItem.Name = "parentHelpToolStripMenuItem";
			this.parentHelpToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
			this.parentHelpToolStripMenuItem.Text = "&Help";
			// 
			// helpMenu
			// 
			this.helpMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2});
			this.helpMenu.Name = "otherMenu";
			this.helpMenu.OwnerItem = this.parentHelpToolStripMenuItem;
			this.helpMenu.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.helpMenu.ShowImageMargin = false;
			this.helpMenu.Size = new System.Drawing.Size(102, 48);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(101, 22);
			this.toolStripMenuItem1.Text = "&Help";
			this.toolStripMenuItem1.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(101, 22);
			this.toolStripMenuItem2.Text = "&About...";
			this.toolStripMenuItem2.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(68, 20);
			this.optionsToolStripMenuItem.Text = "&Options...";
			this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.undoToolStripMenuItem.Text = "&Undo";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.Location = new System.Drawing.Point(6, 3);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(73, 18);
			this.label6.TabIndex = 10;
			this.label6.Text = "dnGREP";
			// 
			// btnBookmark
			// 
			this.btnBookmark.AutoSize = true;
			this.btnBookmark.ContextMenuStrip = this.bookmarkMenu;
			this.btnBookmark.Location = new System.Drawing.Point(5, 107);
			this.btnBookmark.Name = "btnBookmark";
			this.btnBookmark.Size = new System.Drawing.Size(83, 23);
			this.btnBookmark.SplitMenuStrip = this.bookmarkMenu;
			this.btnBookmark.TabIndex = 11;
			this.btnBookmark.Text = "Bookmark";
			this.btnBookmark.UseVisualStyleBackColor = true;
			this.btnBookmark.Click += new System.EventHandler(this.btnBookmark_Click);
			// 
			// bookmarkMenu
			// 
			this.bookmarkMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.openToolStripMenuItem1});
			this.bookmarkMenu.Name = "bookmarkMenu";
			this.bookmarkMenu.Size = new System.Drawing.Size(124, 48);
			// 
			// addToolStripMenuItem
			// 
			this.addToolStripMenuItem.Image = global::dnGREP.Properties.Resources.NewDocumentHS;
			this.addToolStripMenuItem.Name = "addToolStripMenuItem";
			this.addToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.addToolStripMenuItem.Text = "Add";
			this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
			// 
			// openToolStripMenuItem1
			// 
			this.openToolStripMenuItem1.Image = global::dnGREP.Properties.Resources.openHS;
			this.openToolStripMenuItem1.Name = "openToolStripMenuItem1";
			this.openToolStripMenuItem1.Size = new System.Drawing.Size(123, 22);
			this.openToolStripMenuItem1.Text = "Open...";
			this.openToolStripMenuItem1.Click += new System.EventHandler(this.openToolStripMenuItem1_Click);
			// 
			// splitContainer
			// 
			this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer.Location = new System.Drawing.Point(0, 79);
			this.splitContainer.Name = "splitContainer";
			this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer.Panel1
			// 
			this.splitContainer.Panel1.Controls.Add(this.gbSearchFor);
			// 
			// splitContainer.Panel2
			// 
			this.splitContainer.Panel2.Controls.Add(this.btnOtherActions);
			this.splitContainer.Panel2.Controls.Add(this.gbFilter);
			this.splitContainer.Panel2.Controls.Add(this.btnCancel);
			this.splitContainer.Panel2.Controls.Add(this.btnBookmark);
			this.splitContainer.Panel2.Controls.Add(this.btnSearch);
			this.splitContainer.Panel2.Controls.Add(this.btnReplace);
			this.splitContainer.Panel2.Controls.Add(this.tvSearchResult);
			this.splitContainer.Size = new System.Drawing.Size(497, 359);
			this.splitContainer.SplitterDistance = 163;
			this.splitContainer.SplitterWidth = 2;
			this.splitContainer.TabIndex = 12;
			// 
			// btnOtherActions
			// 
			this.btnOtherActions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOtherActions.AutoSize = true;
			this.btnOtherActions.ContextMenuStrip = this.otherMenu;
			this.btnOtherActions.Location = new System.Drawing.Point(364, 107);
			this.btnOtherActions.Name = "btnOtherActions";
			this.btnOtherActions.Size = new System.Drawing.Size(47, 23);
			this.btnOtherActions.SplitMenuStrip = this.otherMenu;
			this.btnOtherActions.TabIndex = 12;
			this.btnOtherActions.Text = ">>";
			this.btnOtherActions.UseVisualStyleBackColor = true;
			this.btnOtherActions.Click += new System.EventHandler(this.btnOtherActions_Click);
			// 
			// otherMenu
			// 
			this.otherMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyFilesToolStripMenuItem,
            this.moveFilesToolStripMenuItem,
            this.deleteFilesToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveAsCSVToolStripMenuItem});
			this.otherMenu.Name = "otherMenu";
			this.otherMenu.ShowImageMargin = false;
			this.otherMenu.Size = new System.Drawing.Size(133, 98);
			// 
			// copyFilesToolStripMenuItem
			// 
			this.copyFilesToolStripMenuItem.Name = "copyFilesToolStripMenuItem";
			this.copyFilesToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
			this.copyFilesToolStripMenuItem.Text = "&Copy files...";
			this.copyFilesToolStripMenuItem.Click += new System.EventHandler(this.copyFilesToolStripMenuItem_Click);
			// 
			// moveFilesToolStripMenuItem
			// 
			this.moveFilesToolStripMenuItem.Name = "moveFilesToolStripMenuItem";
			this.moveFilesToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
			this.moveFilesToolStripMenuItem.Text = "&Move files...";
			this.moveFilesToolStripMenuItem.Click += new System.EventHandler(this.moveFilesToolStripMenuItem_Click);
			// 
			// deleteFilesToolStripMenuItem
			// 
			this.deleteFilesToolStripMenuItem.Name = "deleteFilesToolStripMenuItem";
			this.deleteFilesToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
			this.deleteFilesToolStripMenuItem.Text = "&Delete files...";
			this.deleteFilesToolStripMenuItem.Click += new System.EventHandler(this.deleteFilesToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(129, 6);
			// 
			// saveAsCSVToolStripMenuItem
			// 
			this.saveAsCSVToolStripMenuItem.Name = "saveAsCSVToolStripMenuItem";
			this.saveAsCSVToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
			this.saveAsCSVToolStripMenuItem.Text = "&Save as CSV...";
			this.saveAsCSVToolStripMenuItem.Click += new System.EventHandler(this.saveAsCSVToolStripMenuItem_Click);
			// 
			// saveFileDialog
			// 
			this.saveFileDialog.DefaultExt = "csv";
			this.saveFileDialog.Filter = "CSV file|*.csv";
			// 
			// helpProvider
			// 
			this.helpProvider.HelpNamespace = "Doc\\dnGREP.chm";
			// 
			// MainForm
			// 
			this.AcceptButton = this.btnSearch;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(498, 463);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.splitContainer);
			this.Controls.Add(this.gbSearchIn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.helpProvider.SetHelpKeyword(this, "search-window.html");
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(506, 450);
			this.Name = "MainForm";
			this.helpProvider.SetShowHelp(this, true);
			this.Text = "dnGREP";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formKeyDown);
			this.gbSearchIn.ResumeLayout(false);
			this.gbSearchIn.PerformLayout();
			this.gbSearchFor.ResumeLayout(false);
			this.gbSearchFor.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.searchResultsMenu.ResumeLayout(false);
			this.gbFilter.ResumeLayout(false);
			this.gbFilter.PerformLayout();
			this.fileSearchPanel.ResumeLayout(false);
			this.fileSearchPanel.PerformLayout();
			this.tvContextMenu.ResumeLayout(false);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.helpMenu.ResumeLayout(false);
			this.bookmarkMenu.ResumeLayout(false);
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.splitContainer.Panel2.PerformLayout();
			this.splitContainer.ResumeLayout(false);
			this.otherMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox tbFolderName;
		private System.Windows.Forms.GroupBox gbSearchIn;
		private System.Windows.Forms.Button btnSelectFolder;
		private System.Windows.Forms.GroupBox gbSearchFor;
		private System.Windows.Forms.FolderBrowserDialog folderSelectDialog;
		private wyDay.Controls.SplitButton btnSearch;
		private System.Windows.Forms.RadioButton rbTextSearch;
		private System.Windows.Forms.RadioButton rbRegexSearch;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbSearchFor;
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
		private System.ComponentModel.BackgroundWorker workerSearchReplace;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem parentHelpToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip tvContextMenu;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.CheckBox cbCaseSensitive;
		private System.Windows.Forms.Button btnTest;
		private System.Windows.Forms.ComboBox cbEncoding;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.CheckBox cbMultiline;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private wyDay.Controls.SplitButton btnBookmark;
		private System.Windows.Forms.ContextMenuStrip bookmarkMenu;
		private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem1;
		private System.Windows.Forms.TextBox tbReplaceWith;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.SplitContainer splitContainer;
		private System.Windows.Forms.ToolStripMenuItem openContainingFolderToolStripMenuItem;
		private System.Windows.Forms.RadioButton rbXPathSearch;
		private wyDay.Controls.SplitButton btnOtherActions;
		private System.Windows.Forms.ContextMenuStrip otherMenu;
		private System.Windows.Forms.ToolStripMenuItem copyFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem moveFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem saveAsCSVToolStripMenuItem;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.ContextMenuStrip searchResultsMenu;
		private System.Windows.Forms.ToolStripMenuItem searchInResultsToolStripMenuItem;
		private System.Windows.Forms.RadioButton rbFileAsterisk;
		private System.Windows.Forms.RadioButton rbFileRegex;
		private System.Windows.Forms.Panel fileSearchPanel;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem expandAllToolStripMenuItem;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.ContextMenuStrip helpMenu;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
	}
}

