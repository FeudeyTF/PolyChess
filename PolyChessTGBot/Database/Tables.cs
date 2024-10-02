namespace PolyChessTGBot.Database
{
    public struct FAQEntry
    {
        public int ID;

        public string Question;

        public string Answer;

        public FAQEntry(int id, string question, string answer)
        {
            ID = id;
            Question = question;
            Answer = answer;
        }
    }

    public struct HelpLink
    {
        public int ID;

        public string Title;

        public string Text;

        public string Footer;

        public string? FileID;

        public HelpLink(int id, string title, string text, string footer, string fileID)
        {
            ID = id;
            Title = title;
            Text = text;
            Footer = footer;
            FileID = fileID;
        }
    }
}
