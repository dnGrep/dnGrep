using System;
using System.Linq;
using dnGREP.Common;
using Xunit;

namespace Tests
{
    public class GrepMatchTest
    {
        [Fact]
        public void NewMatchHasNoGroups()
        {
            GrepMatch match = new("pattern", 1, 2, 3);

            Assert.Empty(match.Groups);
            Assert.Empty(match.Groups);
            Assert.DoesNotContain(match.Groups, g => g.Length > 0);
        }

        [Fact]
        public void AddGroupKeepsOrder()
        {
            GrepMatch match = new("pattern", 1, 2, 3);
            match.AddGroup(new GrepCaptureGroup("a", 2, 1, "x"));
            match.AddGroup(new GrepCaptureGroup("b", 3, 1, "y"));

            Assert.Equal(["a", "b"], match.Groups.Select(g => g.Name));
        }

        [Fact]
        public void SetGroupReplacesInPlace()
        {
            GrepMatch match = new("pattern", 1, 2, 3);
            match.AddGroup(new GrepCaptureGroup("a", 2, 1, "x"));
            match.AddGroup(new GrepCaptureGroup("b", 3, 1, "y"));

            match.SetGroup(0, new GrepCaptureGroup("c", 2, 1, "z"));

            Assert.Equal(["c", "b"], match.Groups.Select(g => g.Name));
        }

        [Fact]
        public void SetGroupWithoutGroupsThrows()
        {
            GrepMatch match = new("pattern", 1, 2, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => match.SetGroup(0, new GrepCaptureGroup("a", 2, 1, "x")));
        }

        [Fact]
        public void CopyKeepsIdentityAndHasIndependentGroups()
        {
            GrepMatch match = new("pattern", 4, 5, 6, "value");
            match.AddGroup(new GrepCaptureGroup("a", 5, 1, "x"));

            GrepMatch copy = match.Copy();
            copy.AddGroup(new GrepCaptureGroup("b", 6, 1, "y"));

            Assert.Equal(match.FileMatchId, copy.FileMatchId);
            Assert.True(copy.ReplaceMatch);
            Assert.Single(match.Groups);
            Assert.Equal(2, copy.Groups.Count);
        }

        [Fact]
        public void CopyOfMatchWithoutGroupsHasNoGroups()
        {
            GrepMatch copy = new GrepMatch("pattern", 4, 5, 6).Copy(false);

            Assert.Empty(copy.Groups);
            Assert.False(copy.ReplaceMatch);
        }
    }
}
