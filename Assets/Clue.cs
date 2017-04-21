namespace Coordinates
{
    sealed class Clue
    {
        public string Text { get; private set; }
        public bool IsCorrect { get; private set; }
        public bool IsChinese { get; private set; }
        public int FontSize { get; private set; }
        public string AltText { get; private set; }
        public int? System { get; private set; }
        public Clue(string text, bool correct, bool isChinese, int fontSize, int? system = null, string altText = null)
        {
            Text = text;
            IsCorrect = correct;
            IsChinese = isChinese;
            FontSize = fontSize;
            System = system;
            AltText = altText;
        }

        public string LoggingText
        {
            get
            {
                return (Text + (AltText == null ? null : " (" + AltText + ")")).Replace("\n", " ");
            }
        }
    }
}
