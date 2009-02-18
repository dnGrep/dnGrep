namespace nGREP
{
	partial class BookmarksForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookmarksForm));
			this.gridBookmarks = new System.Windows.Forms.DataGridView();
			this.ColSearch = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColReplace = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColFile = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColComments = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.label1 = new System.Windows.Forms.Label();
			this.textSearch = new System.Windows.Forms.TextBox();
			this.btnUse = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.typeTimer = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.gridBookmarks)).BeginInit();
			this.SuspendLayout();
			// 
			// gridBookmarks
			// 
			this.gridBookmarks.AllowUserToAddRows = false;
			this.gridBookmarks.AllowUserToDeleteRows = false;
			this.gridBookmarks.AllowUserToOrderColumns = true;
			this.gridBookmarks.AllowUserToResizeRows = false;
			this.gridBookmarks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.gridBookmarks.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.gridBookmarks.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridBookmarks.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColSearch,
            this.ColReplace,
            this.ColFile,
            this.ColComments});
			this.gridBookmarks.Location = new System.Drawing.Point(12, 39);
			this.gridBookmarks.MultiSelect = false;
			this.gridBookmarks.Name = "gridBookmarks";
			this.gridBookmarks.ReadOnly = true;
			this.gridBookmarks.RowHeadersVisible = false;
			this.gridBookmarks.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridBookmarks.Size = new System.Drawing.Size(542, 207);
			this.gridBookmarks.TabIndex = 1;
			this.gridBookmarks.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridBookmarks_CellDoubleClick);
			this.gridBookmarks.SelectionChanged += new System.EventHandler(this.gridBookmarks_SelectionChanged);
			// 
			// ColSearch
			// 
			this.ColSearch.DataPropertyName = "SearchPattern";
			this.ColSearch.HeaderText = "Search";
			this.ColSearch.Name = "ColSearch";
			this.ColSearch.ReadOnly = true;
			// 
			// ColReplace
			// 
			this.ColReplace.DataPropertyName = "ReplacePattern";
			this.ColReplace.HeaderText = "Replace";
			this.ColReplace.Name = "ColReplace";
			this.ColReplace.ReadOnly = true;
			// 
			// ColFile
			// 
			this.ColFile.DataPropertyName = "FileNames";
			this.ColFile.FillWeight = 50F;
			this.ColFile.HeaderText = "File Pattern";
			this.ColFile.Name = "ColFile";
			this.ColFile.ReadOnly = true;
			// 
			// ColComments
			// 
			this.ColComments.DataPropertyName = "Description";
			this.ColComments.FillWeight = 50F;
			this.ColComments.HeaderText = "Comments";
			this.ColComments.Name = "ColComments";
			this.ColComments.ReadOnly = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Search:";
			// 
			// textSearch
			// 
			this.textSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textSearch.Location = new System.Drawing.Point(62, 12);
			this.textSearch.Name = "textSearch";
			this.textSearch.Size = new System.Drawing.Size(492, 20);
			this.textSearch.TabIndex = 0;
			this.textSearch.TextChanged += new System.EventHandler(this.textSearch_TextChanged);
			// 
			// btnUse
			// 
			this.btnUse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnUse.Location = new System.Drawing.Point(479, 256);
			this.btnUse.Name = "btnUse";
			this.btnUse.Size = new System.Drawing.Size(75, 23);
			this.btnUse.TabIndex = 2;
			this.btnUse.Text = "Use";
			this.btnUse.UseVisualStyleBackColor = true;
			this.btnUse.Click += new System.EventHandler(this.btnUse_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnAdd.Location = new System.Drawing.Point(12, 256);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 3;
			this.btnAdd.Text = "Add";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// btnEdit
			// 
			this.btnEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnEdit.Location = new System.Drawing.Point(93, 256);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Size = new System.Drawing.Size(75, 23);
			this.btnEdit.TabIndex = 4;
			this.btnEdit.Text = "Edit";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			// 
			// btnDelete
			// 
			this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnDelete.Location = new System.Drawing.Point(174, 256);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(75, 23);
			this.btnDelete.TabIndex = 5;
			this.btnDelete.Text = "Delete";
			this.btnDelete.UseVisualStyleBackColor = true;
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.Location = new System.Drawing.Point(398, 256);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 6;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// typeTimer
			// 
			this.typeTimer.Enabled = true;
			this.typeTimer.Interval = 500;
			this.typeTimer.Tick += new System.EventHandler(this.doSearch);
			// 
			// BookmarksForm
			// 
			this.AcceptButton = this.btnUse;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(566, 287);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.btnDelete);
			this.Controls.Add(this.btnEdit);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.btnUse);
			this.Controls.Add(this.textSearch);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.gridBookmarks);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "BookmarksForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Bookmarks";
			this.Load += new System.EventHandler(this.BookmarksForm_Load);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BookmarksForm_FormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formKeyDown);
			((System.ComponentModel.ISupportInitialize)(this.gridBookmarks)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView gridBookmarks;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textSearch;
		private System.Windows.Forms.Button btnUse;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColSearch;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColReplace;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColFile;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColComments;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnEdit;
		private System.Windows.Forms.Button btnDelete;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Timer typeTimer;
	}
}