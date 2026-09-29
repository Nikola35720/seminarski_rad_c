namespace CsvChartClient.Models
{
    public class FileInfoModel
    {
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
    public class DataPoint
    {
        public string Label { get; set; } = "";
        public List<double> Values { get; set; } = new();
    }
}
