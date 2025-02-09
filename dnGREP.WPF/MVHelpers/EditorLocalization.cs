using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    public class EditorLocalization : ICSharpCode.AvalonEdit.Search.Localization
    {
        /// <summary>
        /// Default: 'Match case'
        /// </summary>
        public override string MatchCaseText => Resources.Preview_MatchCase;

        /// <summary>
        /// Default: 'Match whole words'
        /// </summary>
        public override string MatchWholeWordsText => Resources.Preview_MatchWholeWords;


        /// <summary>
        /// Default: 'Use regular expressions'
        /// </summary>
        public override string UseRegexText => Resources.Preview_UseRegularExpressions;

        /// <summary>
        /// Default: 'Find next (F3)'
        /// </summary>
        public override string FindNextText => Resources.Preview_FindNext;

        /// <summary>
        /// Default: 'Find previous (Shift+F3)'
        /// </summary>
        public override string FindPreviousText => Resources.Preview_FindPrevious;

        /// <summary>
        /// Default: 'Error: '
        /// </summary>
        public override string ErrorText => Resources.Preview_Error;

        /// <summary>
        /// Default: 'No matches found!'
        /// </summary>
        public override string NoMatchesFoundText => Resources.Preview_NoMatchesFound;
    }
}
