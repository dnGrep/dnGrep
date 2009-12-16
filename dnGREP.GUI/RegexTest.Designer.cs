namespace dnGREP
{
	partial class RegexTest
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegexTest));
			this.tbReplaceWith = new System.Windows.Forms.TextBox();
			this.tbSearchFor = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.tbInputText = new System.Windows.Forms.TextBox();
			this.btnReplace = new System.Windows.Forms.Button();
			this.btnSearch = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.tbOutputText = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.btnDone = new System.Windows.Forms.Button();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.btnHelp = new wyDay.Controls.SplitButton();
			this.menuHelp = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.cheatsheetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.regexLookupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cbMultiline = new System.Windows.Forms.CheckBox();
			this.cbCaseSensitive = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.menuHelp.SuspendLayout();
			this.SuspendLayout();
			// 
			// tbReplaceWith
			// 
			this.tbReplaceWith.AcceptsReturn = true;
			this.tbReplaceWith.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "ReplaceWith", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbReplaceWith.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbReplaceWith.Location = new System.Drawing.Point(290, 21);
			this.tbReplaceWith.Multiline = true;
			this.tbReplaceWith.Name = "tbReplaceWith";
			this.tbReplaceWith.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbReplaceWith.Size = new System.Drawing.Size(281, 85);
			this.tbReplaceWith.TabIndex = 1;
			this.tbReplaceWith.Text = global::dnGREP.Properties.Settings.Default.ReplaceWith;
			// 
			// tbSearchFor
			// 
			this.tbSearchFor.AcceptsReturn = true;
			this.tbSearchFor.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::dnGREP.Properties.Settings.Default, "SearchFor", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbSearchFor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbSearchFor.Location = new System.Drawing.Point(3, 21);
			this.tbSearchFor.Multiline = true;
			this.tbSearchFor.Name = "tbSearchFor";
			this.tbSearchFor.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbSearchFor.Size = new System.Drawing.Size(281, 85);
			this.tbSearchFor.TabIndex = 0;
			this.tbSearchFor.Text = global::dnGREP.Properties.Settings.Default.SearchFor;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(290, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 13);
			this.label2.TabIndex = 7;
			this.label2.Text = "Replace with:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(59, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "Search for:";
			// 
			// tbInputText
			// 
			this.tbInputText.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbInputText.Location = new System.Drawing.Point(3, 23);
			this.tbInputText.Multiline = true;
			this.tbInputText.Name = "tbInputText";
			this.tbInputText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbInputText.Size = new System.Drawing.Size(558, 114);
			this.tbInputText.TabIndex = 0;
			// 
			// btnReplace
			// 
			this.btnReplace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnReplace.Location = new System.Drawing.Point(424, 118);
			this.btnReplace.Name = "btnReplace";
			this.btnReplace.Size = new System.Drawing.Size(75, 23);
			this.btnReplace.TabIndex = 2;
			this.btnReplace.Text = "Replace";
			this.btnReplace.UseVisualStyleBackColor = true;
			this.btnReplace.Click += new System.EventHandler(this.btnReplace_Click);
			// 
			// btnSearch
			// 
			this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSearch.Location = new System.Drawing.Point(505, 118);
			this.btnSearch.Name = "btnSearch";
			this.btnSearch.Size = new System.Drawing.Size(75, 23);
			this.btnSearch.TabIndex = 3;
			this.btnSearch.Text = "Search";
			this.btnSearch.UseVisualStyleBackColor = true;
			this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.tableLayoutPanel1);
			this.groupBox1.Location = new System.Drawing.Point(8, 146);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(574, 300);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Regex test:";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Controls.Add(this.tbOutputText, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.tbInputText, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(564, 281);
			this.tableLayoutPanel1.TabIndex = 14;
			// 
			// tbOutputText
			// 
			this.tbOutputText.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbOutputText.Location = new System.Drawing.Point(3, 163);
			this.tbOutputText.Multiline = true;
			this.tbOutputText.Name = "tbOutputText";
			this.tbOutputText.ReadOnly = true;
			this.tbOutputText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbOutputText.Size = new System.Drawing.Size(558, 115);
			this.tbOutputText.TabIndex = 1;
			// 
			// label3
			// 
			this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label3.Location = new System.Drawing.Point(3, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(558, 20);
			this.label3.TabIndex = 13;
			this.label3.Text = "Sample input text:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label4
			// 
			this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label4.Location = new System.Drawing.Point(3, 140);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(558, 20);
			this.label4.TabIndex = 14;
			this.label4.Text = "Output text:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// btnDone
			// 
			this.btnDone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDone.Location = new System.Drawing.Point(507, 455);
			this.btnDone.Name = "btnDone";
			this.btnDone.Size = new System.Drawing.Size(75, 23);
			this.btnDone.TabIndex = 5;
			this.btnDone.Text = "Done";
			this.btnDone.UseVisualStyleBackColor = true;
			this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel2.ColumnCount = 2;
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this.label2, 1, 0);
			this.tableLayoutPanel2.Controls.Add(this.tbReplaceWith, 1, 1);
			this.tableLayoutPanel2.Controls.Add(this.tbSearchFor, 0, 1);
			this.tableLayoutPanel2.Location = new System.Drawing.Point(8, 3);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 2;
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 18F));
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel2.Size = new System.Drawing.Size(574, 109);
			this.tableLayoutPanel2.TabIndex = 9;
			// 
			// btnHelp
			// 
			this.btnHelp.AutoSize = true;
			this.btnHelp.ContextMenuStrip = this.menuHelp;
			this.btnHelp.Location = new System.Drawing.Point(8, 118);
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Size = new System.Drawing.Size(75, 23);
			this.btnHelp.SplitMenuStrip = this.menuHelp;
			this.btnHelp.TabIndex = 10;
			this.btnHelp.Text = "Help";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// menuHelp
			// 
			this.menuHelp.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cheatsheetToolStripMenuItem,
            this.regexLookupToolStripMenuItem});
			this.menuHelp.Name = "menuHelp";
			this.menuHelp.Size = new System.Drawing.Size(151, 48);
			// 
			// cheatsheetToolStripMenuItem
			// 
			this.cheatsheetToolStripMenuItem.Name = "cheatsheetToolStripMenuItem";
			this.cheatsheetToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
			this.cheatsheetToolStripMenuItem.Text = "Cheat-sheet";
			this.cheatsheetToolStripMenuItem.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// regexLookupToolStripMenuItem
			// 
			this.regexLookupToolStripMenuItem.Name = "regexLookupToolStripMenuItem";
			this.regexLookupToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
			this.regexLookupToolStripMenuItem.Text = "Regex lookup";
			// 
			// cbMultiline
			// 
			this.cbMultiline.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbMultiline.AutoSize = true;
			this.cbMultiline.Checked = global::dnGREP.Properties.Settings.Default.Multiline;
			this.cbMultiline.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::dnGREP.Properties.Settings.Default, "Multiline", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbMultiline.Location = new System.Drawing.Point(354, 122);
			this.cbMultiline.Name = "cbMultiline";
			this.cbMultiline.Size = new System.Drawing.Size(64, 17);
			this.cbMultiline.TabIndex = 12;
			this.cbMultiline.Text = "Multiline";
			this.cbMultiline.UseVisualStyleBackColor = true;
			// 
			// cbCaseSensitive
			// 
			this.cbCaseSensitive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbCaseSensitive.AutoSize = true;
			this.cbCaseSensitive.Checked = global::dnGREP.Properties.Settings.Default.CaseSensitive;
			this.cbCaseSensitive.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbCaseSensitive.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::dnGREP.Properties.Settings.Default, "CaseSensitive", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbCaseSensitive.Location = new System.Drawing.Point(254, 122);
			this.cbCaseSensitive.Name = "cbCaseSensitive";
			this.cbCaseSensitive.Size = new System.Drawing.Size(94, 17);
			this.cbCaseSensitive.TabIndex = 11;
			this.cbCaseSensitive.Text = "Case sensitive";
			this.cbCaseSensitive.UseVisualStyleBackColor = true;
			// 
			// RegexTest
			// 
			this.AcceptButton = this.btnDone;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(592, 487);
			this.Controls.Add(this.cbMultiline);
			this.Controls.Add(this.cbCaseSensitive);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.tableLayoutPanel2);
			this.Controls.Add(this.btnDone);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnReplace);
			this.Controls.Add(this.btnSearch);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(600, 513);
			this.Name = "RegexTest";
			this.Text = "Regex Test";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formKeyDown);
			this.groupBox1.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			this.menuHelp.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox tbReplaceWith;
		private System.Windows.Forms.TextBox tbSearchFor;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbInputText;
		private System.Windows.Forms.Button btnReplace;
		private System.Windows.Forms.Button btnSearch;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox tbOutputText;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button btnDone;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private wyDay.Controls.SplitButton btnHelp;
		private System.Windows.Forms.ContextMenuStrip menuHelp;
		private System.Windows.Forms.ToolStripMenuItem cheatsheetToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem regexLookupToolStripMenuItem;
		private System.Windows.Forms.CheckBox cbMultiline;
		private System.Windows.Forms.CheckBox cbCaseSensitive;
	}
}