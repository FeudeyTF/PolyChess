namespace PolyChessTGBot.Database
{
    public struct FAQEntry(int id, string question, string answer)
    {
        public int ID = id;

        public string Question = question;

        public string Answer = answer;
    }

    public struct HelpLink(int id, string title, string text, string footer, string fileID)
    {
        public int ID = id;

        public string Title = title;

        public string Text = text;

        public string Footer = footer;

        public string? FileID = fileID;
    }
}
