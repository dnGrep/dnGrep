using System.Collections.Generic;
using dnGREP.Common;
using dnGREP.WPF;
using Xunit;

namespace Tests
{
    public class ReplaceViewModelTest
    {
        [Fact]
        public void SelectedGrepMatchUpdatesStateBeforeNotificationAndComparesByReference()
        {
            var viewModel = new ReplaceViewModel { IsFullDialog = false };
            var first = new GrepMatch("match", 1, 2, 5);
            var second = new GrepMatch("match", 1, 2, 5);
            Assert.Equal(first, second);
            Assert.NotSame(first, second);

            var firstLineMatch = first.Copy();
            firstLineMatch.DisplayStartLocation = 4;
            var secondLineMatch = second.Copy();
            secondLineMatch.DisplayStartLocation = 9;
            viewModel.SelectedSearchResult = new GrepSearchResult
            {
                SearchResults = new List<GrepLine>
                {
                    new GrepLine(1, "first", false, new List<GrepMatch> { firstLineMatch }) { DisplayFileLineNumber = 3 },
                    new GrepLine(1, "second", false, new List<GrepMatch> { secondLineMatch }) { DisplayFileLineNumber = 7 }
                }
            };

            int changingCount = 0;
            int changedCount = 0;
            GrepMatch? expectedPrevious = null;
            GrepMatch? expectedSelection = first;
            int expectedLine = 3;
            int expectedColumn = 4;
            viewModel.PropertyChanging += (_, e) =>
            {
                if (e.PropertyName == nameof(ReplaceViewModel.SelectedGrepMatch))
                {
                    changingCount++;
                    Assert.Same(expectedPrevious, viewModel.SelectedGrepMatch);
                }
            };
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ReplaceViewModel.SelectedGrepMatch))
                {
                    changedCount++;
                    Assert.Same(expectedSelection, viewModel.SelectedGrepMatch);
                    Assert.False(viewModel.ReviewComplete);
                    Assert.Equal(expectedLine, viewModel.LineNumber);
                    Assert.Equal(expectedColumn, viewModel.ColNumber);
                }
            };

            viewModel.ReviewComplete = true;
            viewModel.SelectedGrepMatch = first;
            Assert.Equal(1, changingCount);
            Assert.Equal(1, changedCount);

            viewModel.ReviewComplete = true;
            viewModel.SelectedGrepMatch = first;
            Assert.True(viewModel.ReviewComplete);
            Assert.Equal(1, changingCount);
            Assert.Equal(1, changedCount);

            expectedPrevious = first;
            expectedSelection = second;
            expectedLine = 7;
            expectedColumn = 9;
            viewModel.SelectedGrepMatch = second;
            Assert.Equal(2, changingCount);
            Assert.Equal(2, changedCount);

            expectedPrevious = second;
            expectedSelection = null;
            viewModel.ReviewComplete = true;
            viewModel.SelectedGrepMatch = null;
            Assert.Equal(3, changingCount);
            Assert.Equal(3, changedCount);

            viewModel.SelectedGrepMatch = null;
            Assert.Equal(3, changingCount);
            Assert.Equal(3, changedCount);
        }
    }
}
