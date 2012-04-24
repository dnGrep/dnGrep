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
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace StickyWindowLibrary
{
    public interface IFormAdapter
    {
        IntPtr Handle { get; }
        Rectangle Bounds { get; set; }
        Size MaximumSize { get; set; }
        Size MinimumSize { get; set; }
        bool Capture { get; set; }
        void Activate();
        Point PointToScreen(Point point);
    }
}
