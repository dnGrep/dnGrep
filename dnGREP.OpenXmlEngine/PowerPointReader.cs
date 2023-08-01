using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Paragraph = DocumentFormat.OpenXml.Drawing.Paragraph;
using Text = DocumentFormat.OpenXml.Drawing.Text;

namespace dnGREP.Engines.OpenXml
{
    internal static class PowerPointReader
    {
        private static readonly string bar = char.ConvertFromUtf32(0x2016) + " ";

        public static IEnumerable<Tuple<int, string>> ExtractPowerPointText(Stream stream,
            PauseCancelToken pauseCancelToken)
        {
            // Open a given PointPoint document as readonly
            using PresentationDocument ppt = PresentationDocument.Open(stream, false);
            int numberOfSlides = CountSlides(ppt);

            for (int idx = 0; idx < numberOfSlides; idx++)
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                string text = GetSlideText(ppt, idx, pauseCancelToken);

                yield return new Tuple<int, string>(idx + 1, text);
            }
        }

        private static int CountSlides(PresentationDocument presentationDocument)
        {
            int slidesCount = 0;

            // Get the presentation part of document.
            PresentationPart? presentationPart = presentationDocument.PresentationPart;

            // Get the slide count from the SlideParts.
            if (presentationPart != null)
            {
                slidesCount = presentationPart.SlideParts.Count();
            }
            return slidesCount;
        }

        internal static string GetSlideText(PresentationDocument ppt, int idx,
            PauseCancelToken pauseCancelToken)
        {
            // Get the relationship ID of the first slide.
            PresentationPart? part = ppt.PresentationPart;
            if (part == null) return string.Empty;

            OpenXmlElementList? slideIds = part.Presentation.SlideIdList?.ChildElements;
            if (slideIds == null) return string.Empty;

            string? relId = (slideIds[idx] as SlideId)?.RelationshipId;
            if (relId == null) return string.Empty;

            // Get the slide part from the relationship ID.
            SlidePart slidePart = (SlidePart)part.GetPartById(relId);

            StringBuilder sb = new();

            var paragraphs = slidePart.Slide.Descendants<Paragraph>();
            foreach (Paragraph paragraph in paragraphs)
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                var elements = paragraph.Descendants();
                foreach (OpenXmlElement element in elements)
                {
                    if (element is Break)
                    {
                        sb.Append(Environment.NewLine);
                    }
                    else if (element is Text text)
                    {
                        sb.Append(text.Text);
                    }
                }
                sb.Append(Environment.NewLine);
            }


            if (slidePart.NotesSlidePart != null)
            {
                sb.Append(Environment.NewLine);
                var texts = slidePart.NotesSlidePart.NotesSlide.Descendants<Text>();
                foreach (Text text in texts)
                {
                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    if (text.Parent is Field field && (field.Type?.HasValue is true) && field.Type.Value == "slidenum")
                    {
                        continue;
                    }

                    sb.Append(bar).Append(text.Text);
                    sb.Append(Environment.NewLine);
                }
            }

            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            return sb.ToString();
        }
    }
}
