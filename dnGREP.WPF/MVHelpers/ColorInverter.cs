﻿using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Windows.Media;

namespace dnGREP.WPF
{
    public static class ColorInverter
    {

        public static void TranslateThemeColors(IHighlightingDefinition hl)
        {
            foreach (var item in hl.NamedHighlightingColors)
            {
                //if (color != null && !color.IsFrozen)
                //{
                //    color.Foreground = Invert(color.Foreground);
                //    color.Background = Invert(color.Background);
                //}
                if (!item.IsFrozen)
                {
                    if (item.Foreground != null)
                    {
                        string hex = item.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.Background != null)
                    {
                        string hex = item.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Rules)
            {
                //if (item.Color != null && !item.Color.IsFrozen)
                //{
                //    item.Color.Foreground = Invert(item.Color.Foreground);
                //    item.Color.Background = Invert(item.Color.Background);
                //}

                if (item.Color != null && !item.Color.IsFrozen && string.IsNullOrWhiteSpace(item.Color.Name))
                {
                    if (item.Color.Foreground != null)
                    {
                        string hex = item.Color.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.Color.Background != null)
                    {
                        string hex = item.Color.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Spans)
            {
                //if (item.SpanColor != null && !item.SpanColor.IsFrozen)
                //{
                //    item.SpanColor.Foreground = Invert(item.SpanColor.Foreground);
                //    item.SpanColor.Background = Invert(item.SpanColor.Background);
                //}
                //if (item.StartColor != null && !item.StartColor.IsFrozen)
                //{
                //    item.StartColor.Foreground = Invert(item.StartColor.Foreground);
                //    item.StartColor.Background = Invert(item.StartColor.Background);
                //}
                //if (item.EndColor != null && !item.EndColor.IsFrozen)
                //{
                //    item.EndColor.Foreground = Invert(item.EndColor.Foreground);
                //    item.EndColor.Background = Invert(item.EndColor.Background);
                //}

                if (item.SpanColor != null && !item.SpanColor.IsFrozen && string.IsNullOrWhiteSpace(item.SpanColor.Name))
                {
                    if (item.SpanColor.Foreground != null)
                    {
                        string hex = item.SpanColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.SpanColor.Background != null)
                    {
                        string hex = item.SpanColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
                if (item.StartColor != null && !item.StartColor.IsFrozen && string.IsNullOrWhiteSpace(item.StartColor.Name))
                {
                    if (item.StartColor.Foreground != null)
                    {
                        string hex = item.StartColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.StartColor.Background != null)
                    {
                        string hex = item.StartColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
                if (item.EndColor != null && !item.EndColor.IsFrozen && string.IsNullOrWhiteSpace(item.EndColor.Name))
                {
                    if (item.EndColor.Foreground != null)
                    {
                        string hex = item.EndColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.EndColor.Background != null)
                    {
                        string hex = item.EndColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
        }

        private static HighlightingBrush Invert(HighlightingBrush brush)
        {
            if (brush != null)
            {
                Color c = (Color)ColorConverter.ConvertFromString(brush.ToString());
                return new SimpleHighlightingBrush(Invert(c));
            }
            return null;
        }

        public static Color Invert(Color c)
        {
            byte shift = (byte)(byte.MaxValue - Math.Min(c.R, Math.Min(c.G, c.B)) - Math.Max(c.R, Math.Max(c.G, c.B)));
            Color result = new Color
            {
                A = c.A,
                R = (byte)(shift + c.R),
                G = (byte)(shift + c.G),
                B = (byte)(shift + c.B),
            };
            return result;
        }

        //private static Color Invert(Color c)
        //{
        //    int shift = c.A - Math.Min(c.R, Math.Min(c.G, c.B)) - Math.Max(c.R, Math.Max(c.G, c.B));
        //    Color result = new Color
        //    {
        //        A = c.A,
        //        R = Wrap(shift + c.R),
        //        G = Wrap(shift + c.G),
        //        B = Wrap(shift + c.B),
        //    };
        //    return result;
        //}

        //private static byte Wrap(int v)
        //{
        //    if (v > byte.MaxValue)
        //        v -= byte.MaxValue;
        //    if (v < 0)
        //        v += byte.MaxValue;
        //    return (byte)v;
        //}

    }
}
