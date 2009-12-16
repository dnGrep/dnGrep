using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using dnGREP.Common;

namespace dnGREP
{
	public partial class BookmarkDetails : Form
	{
		private CreateOrEdit action = CreateOrEdit.Edit;
		
		private Bookmark bookmark = null;
		public Bookmark Bookmark
		{
			get { return bookmark; }
			set { bookmark = value; changeState();  }
		}

		private void changeState()
		{
			if (action == CreateOrEdit.Create)
			{
				btnCreateOrEdit.Text = "Create";
			}
			else
			{
				btnCreateOrEdit.Text = "Edit";
			}
			if (bookmark != null)
			{
				tbDescription.Text = bookmark.Description;
				tbFileNames.Text = bookmark.FileNames;
				tbReplaceWith.Text = bookmark.ReplacePattern;
				tbSearchFor.Text = bookmark.SearchPattern;
			}
		}

		public BookmarkDetails(CreateOrEdit action)
		{
			InitializeComponent();
			this.action = action;
		}

		private void BookmarkDetails_Load(object sender, EventArgs e)
		{
			changeState();
		}

		private void btnCreateOrEdit_Click(object sender, EventArgs e)
		{
			if (bookmark == null)
				bookmark = new Bookmark();
			bookmark.Description = tbDescription.Text;
			bookmark.FileNames = tbFileNames.Text;
			bookmark.SearchPattern = tbSearchFor.Text;
			bookmark.ReplacePattern = tbReplaceWith.Text;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void formKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				Close();
		}
	}

	public enum CreateOrEdit
	{
		Create,
		Edit
	}
}