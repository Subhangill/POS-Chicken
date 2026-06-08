namespace POS.Models
{
    public  class AppDate
    {
        //private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
        //public static DateTime Now => TimeZoneInfo.ConvertTimeToUtc(DateTime.Now,TimeZone);
        //public static DateTime Today => TimeZoneInfo.ConvertTimeToUtc(DateTime.Now,TimeZone).Date;
        private static readonly TimeZoneInfo PakistanTimeZone =
           TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone);

        public static DateTime Today => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone).Date;

    }
}
