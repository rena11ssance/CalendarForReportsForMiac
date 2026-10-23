using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CalendarForReports.Models;

namespace CalendarForReports.Data
{
    public static class JsonStorage
    {
        private static string FilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports.json");

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static List<ReportTask> Load()
        {
            if (!File.Exists(FilePath)) return new List<ReportTask>();
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<ReportTask>>(json, Options)
                       ?? new List<ReportTask>();
            }
            catch
            {
                return new List<ReportTask>();
            }
        }

        public static void Save(List<ReportTask> tasks)
        {
            var json = JsonSerializer.Serialize(tasks, Options);
            File.WriteAllText(FilePath, json);
        }
    }
}