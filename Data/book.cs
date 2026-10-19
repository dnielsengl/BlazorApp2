namespace BlazorApp2.Data
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int Total { get; set; }
        public int Available { get; set; }  
        public Book()
        {
            Id = 0;
            Title = "";
            Author = "";
            Total = 0;
            Available = 0;
        }
    }
}