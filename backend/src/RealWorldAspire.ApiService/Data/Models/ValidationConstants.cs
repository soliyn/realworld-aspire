namespace RealWorldAspire.ApiService.Data.Models;

public static class ValidationConstants
{
    private const int Bio = 500;
    private const int Image = 1000;

    public static class AppUser
    {
        public const int UserNameMaxLength = 256;
        public const int EmailMaxLength = 256;
        public const int BioMaxLength = Bio;
        public const int ImageMaxLength = Image;
    }
    
    public static class Article
    {
        public const int SlugMaxLength = 50;
        public const int TitleMaxLength = 50;
        public const int DescriptionMaxLength = 200;
    }

    public static class Author
    {
        public const int UserNameMaxLength = 50;
        public const int BioMaxLength = Bio;
        public const int ImageMaxLength = Image;
    }
}