using System.Text;
using System.Text.Json;

namespace SmartLogArchiver;

class Program
{
    class LogIssue
    {
        public string Message { get; set; } = "";
        public string Level { get; set; } = "";
        public int Count { get; set; }
    }

    class Report
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalErrors { get; set; }
        public int TotalWarns { get; set; }
        public List<LogIssue> TopIssues { get; set; } = new();
    }

    static void Main(string[] args)
    {
        string logFilePath;
        
        if (args.Length == 0)
        {
            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            Console.WriteLine($"Путь к логу не указан. Использую путь по умолчанию: {logFilePath}");
        }
        else
        {
            logFilePath = args[0];
        }

        if (!File.Exists(logFilePath))
        {
            Console.WriteLine($"Ошибка: Файл не найден по пути {logFilePath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(logFilePath, Encoding.UTF8);
            
            Dictionary<string, LogIssue> issuesDict = new();
            
            int totalErrors = 0;
            int totalWarns = 0;
            
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                int firstBracketClose = line.IndexOf(']');
                if (firstBracketClose == -1) continue;
                
                int secondBracketOpen = line.IndexOf('[', firstBracketClose);
                if (secondBracketOpen == -1) continue;
                
                int secondBracketClose = line.IndexOf(']', secondBracketOpen);
                if (secondBracketClose == -1) continue;
                
                string level = line.Substring(secondBracketOpen + 1, secondBracketClose - secondBracketOpen - 1).Trim();
                
                if (level != "ERROR" && level != "WARN") continue;
                
                string message = line.Substring(secondBracketClose + 1).Trim();
                
                string key = $"{level}|{message}";
                
                if (issuesDict.ContainsKey(key))
                {
                    issuesDict[key].Count++;
                }
                else
                {
                    issuesDict[key] = new LogIssue
                    {
                        Message = message,
                        Level = level,
                        Count = 1
                    };
                }
                
                if (level == "ERROR") totalErrors++;
                else if (level == "WARN") totalWarns++;
            }
            
            LogIssue? topError = issuesDict.Values
                .Where(i => i.Level == "ERROR")
                .OrderByDescending(i => i.Count)
                .FirstOrDefault();
            
            LogIssue? topWarn = issuesDict.Values
                .Where(i => i.Level == "WARN")
                .OrderByDescending(i => i.Count)
                .FirstOrDefault();
            
            List<LogIssue> topIssues = new();
            if (topError != null) topIssues.Add(topError);
            if (topWarn != null) topIssues.Add(topWarn);
            
            Report report = new Report
            {
                GeneratedAt = DateTime.Now,
                TotalErrors = totalErrors,
                TotalWarns = totalWarns,
                TopIssues = topIssues
            };
            
            string reportPath = Path.Combine(Path.GetDirectoryName(logFilePath) ?? "", "report.json");
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(report, options);
            
            File.WriteAllText(reportPath, jsonString, Encoding.UTF8);
            
            Console.WriteLine("Содержимое report.json:");
            Console.WriteLine(jsonString);
            Console.WriteLine($"\nОтчет сохранен по пути: {reportPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }
}