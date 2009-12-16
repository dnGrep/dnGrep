namespace dnGREP
{
	partial class BookmarkDetails
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookmarkDetails));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.tbSearchFor = new System.Windows.Forms.TextBox();
			this.tbReplaceWith = new System.Windows.Forms.TextBox();
			this.tbFileNames = new System.Windows.Forms.TextBox();
			this.tbDescription = new System.Windows.Forms.TextBox();
			this.btnCreateOrEdit = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(80, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Search pattern:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 87);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(86, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Replace pattern:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 165);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(60, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "File names:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 204);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(63, 13);
			this.label4.TabIndex = 3;
			this.label4.Text = "Description:";
			// 
			// tbSearchFor
			// 
			this.tbSearchFor.Location = new System.Drawing.Point(12, 25);
			this.tbSearchFor.Multiline = true;
			this.tbSearchFor.Name = "tbSearchFor";
			this.tbSearchFor.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbSearchFor.Size = new System.Drawing.Size(313, 59);
			this.tbSearchFor.TabIndex = 4;
			// 
			// tbReplaceWith
			// 
			this.tbReplaceWith.Location = new System.Drawing.Point(12, 103);
			this.tbReplaceWith.Multiline = true;
			this.tbReplaceWith.Name = "tbReplaceWith";
			this.tbReplaceWith.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbReplaceWith.Size = new System.Drawing.Size(313, 59);
			this.tbReplaceWith.TabIndex = 5;
			// 
			// tbFileNames
			// 
			this.tbFileNames.Location = new System.Drawing.Point(12, 181);
			this.tbFileNames.Name = "tbFileNames";
			this.tbFileNames.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbFileNames.Size = new System.Drawing.Size(313, 20);
			this.tbFileNames.TabIndex = 6;
			// 
			// tbDescription
			// 
			this.tbDescription.Location = new System.Drawing.Point(12, 220);
			this.tbDescription.Multiline = true;
			this.tbDescription.Name = "tbDescription";
			this.tbDescription.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbDescription.Size = new System.Drawing.Size(313, 59);
			this.tbDescription.TabIndex = 7;
			// 
			// btnCreateOrEdit
			// 
			this.btnCreateOrEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCreateOrEdit.Location = new System.Drawing.Point(250, 285);
			this.btnCreateOrEdit.Name = "btnCreateOrEdit";
			this.btnCreateOrEdit.Size = new System.Drawing.Size(75, 23);
			this.btnCreateOrEdit.TabIndex = 8;
			this.btnCreateOrEdit.Text = "Edit";
			this.btnCreateOrEdit.UseVisualStyleBackColor = true;
			this.btnCreateOrEdit.Click += new System.EventHandler(this.btnCreateOrEdit_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(169, 285);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 9;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// helpProvider
			// 
			this.helpProvider.HelpNamespace = "Doc\\dnGREP.chm";
			// 
			// BookmarkDetails
			// 
			this.AcceptButton = this.btnCreateOrEdit;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(337, 318);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnCreateOrEdit);
			this.Controls.Add(this.tbDescription);
			this.Controls.Add(this.tbFileNames);
			this.Controls.Add(this.tbReplaceWith);
			this.Controls.Add(this.tbSearchFor);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.helpProvider.SetHelpKeyword(this, "bookmarks.html");
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "BookmarkDetails";
			this.helpProvider.SetShowHelp(this, true);
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Bookmark details...";
			this.Load += new System.EventHandler(this.BookmarkDetails_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formKeyDown);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox tbSearchFor;
		private System.Windows.Forms.TextBox tbReplaceWith;
		private System.Windows.Forms.TextBox tbFileNames;
		private System.Windows.Forms.TextBox tbDescription;
		private System.Windows.Forms.Button btnCreateOrEdit;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.HelpProvider helpProvider;
	}
}