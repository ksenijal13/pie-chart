using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;

class Program
{
    static async Task Main()
    {
        string employeesRestEndpointUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
        List<EmployeeData> employeeDataList = await getEmployeesData(employeesRestEndpointUrl);

        var groupedData = employeeDataList
            .GroupBy(data => data.EmployeeName)
            .Select(group => new EmployeeData
            {
                Id = group.Key,
                EmployeeName = group.First().EmployeeName,
                TotalHours = group.Sum(data => EmployeesHours(data.StarTimeUtc, data.EndTimeUtc))
            })
            .ToList();

        string imagePath = "..\\..\\..\\pie-chart.png";
        GeneratePieChart(groupedData, imagePath);

        Console.WriteLine("Pie chart generated. Image is saved in the pie-chart directory.");
    }

    static async Task<List<EmployeeData>> getEmployeesData(string apiUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonContent = await response.Content.ReadAsStringAsync();
                List<EmployeeData> employeeDataList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EmployeeData>>(jsonContent);
                return employeeDataList;
            }
            else
            {
                throw new Exception($"Error: {response.StatusCode}");
            }
        }
    }

    static void GeneratePieChart(List<EmployeeData> employeeDataList, string imagePath)
    {
        using (var bitmap = new SKBitmap(800, 800))
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);

            var pieRect = new SKRect(50, 50, 750, 750);
            var totalHours = employeeDataList.Sum(data => data.TotalHours);

            float startAngle = 0;
            foreach (var employeeData in employeeDataList)
            {
                var sweepAngle = (employeeData.TotalHours / totalHours) * 360;
                using (var paint = new SKPaint
                {
                    Color = GetRandChartColor(),
                    IsAntialias = true
                })
                {
                    canvas.DrawArc(pieRect, startAngle, sweepAngle, true, paint);
                    var midAngle = startAngle + sweepAngle / 2;

                    var x = pieRect.MidX + (float)(pieRect.Width / 2.5 * Math.Cos(Math.PI * midAngle / 180));
                    var y = pieRect.MidY + (float)(pieRect.Height / 2.5 * Math.Sin(Math.PI * midAngle / 180));

                    if (employeeData.EmployeeName != null)
                    {
                        canvas.DrawText(employeeData.EmployeeName, x, y, new SKPaint
                        {
                            Color = SKColors.Black,
                            TextAlign = SKTextAlign.Center,
                            TextSize = 24,
                            IsAntialias = true
                        });
                    }
                }
                startAngle += sweepAngle;
            }

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(imagePath))
            {
                data.SaveTo(stream);
            }
        }
    }

    static SKColor GetRandChartColor()
    {
        var rand = new Random();
        return new SKColor((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256));
    }

    static float EmployeesHours(DateTime startTime, DateTime endTime)
    {
        TimeSpan timeWorked = endTime - startTime;
        return (float)timeWorked.TotalHours;
    }
}

class EmployeeData
{
    public string Id { get; set; }
    public string EmployeeName { get; set; }
    public DateTime StarTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public float TotalHours { get; set; }
}
