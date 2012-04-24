////////////////////////////////////////////////////////////////////////////////
// StickyWindows
// 
// Copyright (c) 2009 Riccardo Pietrucci
//
// This software is provided 'as-is', without any express or implied warranty.
// In no event will the author be held liable for any damages arising from 
// the use of this software.
// Permission to use, copy, modify, distribute and sell this software for any 
// purpose is hereby granted without fee, provided that the above copyright 
// notice appear in all copies and that both that copyright notice and this 
// permission notice appear in supporting documentation.
//
//////////////////////////////////////////////////////////////////////////////////


using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyWindowLibrary
{
    public class WinFormAdapter : IFormAdapter
    {
        private readonly Form _form;

        public WinFormAdapter(Form form)
        {
            _form = form;
        }

        #region IFormAdapter Members

        public IntPtr Handle
        {
            get { return _form.Handle; }
        }

        public Rectangle Bounds
        {
            get { return _form.Bounds; }
            set { _form.Bounds = value; }
        }

        public Size MaximumSize
        {
            get { return _form.MaximumSize; }
            set { _form.MaximumSize = value; }
        }

        public Size MinimumSize
        {
            get { return _form.MinimumSize; }
            set { _form.MinimumSize = value; }
        }

        public bool Capture
        {
            get { return _form.Capture; }
            set { _form.Capture = value; }
        }

        public void Activate()
        {
            _form.Activate();
        }

        public Point PointToScreen(Point point)
        {
            return _form.PointToScreen(point);
        }

        #endregion
    }
}