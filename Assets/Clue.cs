namespace Coordinates
{
    sealed class Clue
    {
        public string Text { get; private set; }
        public bool IsCorrect { get; private set; }
        public bool IsChinese { get; private set; }
        public int FontSize { get; private set; }
        public string LoggingExtra { get; private set; }
        public Clue(string text, bool correct, bool isChinese, int fontSize, string loggingExtra = null)
        {
            Text = text;
            IsCorrect = correct;
            IsChinese = isChinese;
            FontSize = fontSize;
            LoggingExtra = loggingExtra;
        }
    }
}
