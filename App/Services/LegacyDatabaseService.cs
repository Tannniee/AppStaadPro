using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Xml.Linq;

namespace StaadSteelApp
{
    public class LegacyDatabaseService
    {
        private readonly string _sectionsPath;
        private readonly string _xmlPath;

        // User allowed zones
        private readonly HashSet<string> _allowedZones = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "American", "British", "European", "Australian", "Chinese", "Japanese"
        };

        // STAAD API Country Codes for Hot Rolled Sections
        // Reference: STAAD OpenSTAAD CreateBeamPropertyFromTable documentation
        private static readonly Dictionary<string, int> _staadCountryCodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "American", 1 },
            { "Australian", 2 },
            { "British", 3 },
            { "Canadian", 4 },
            { "Chinese", 5 },
            { "Dutch", 6 },
            { "European", 7 },
            { "French", 8 },
            { "German", 9 },
            { "Indian", 10 },
            { "Japanese", 11 },
            { "Russian", 12 },
            { "South African", 13 },
            { "Spanish", 14 },
            { "Venezuelan", 15 },
            { "Korean", 16 }
        };

        public class ZoneInfo
        {
            public string Name { get; set; }
            public string DbFileName { get; set; }
            public int CountryId { get; set; }      // XML ID (for reference only)
            public int StaadCountryCode { get; set; } // STAAD API expects this!
        }

        public class SectionItem
        {
            public string DisplayName { get; set; }
            public string StaadName { get; set; }

            public override string ToString()
            {
                return !string.IsNullOrEmpty(StaadName) ? StaadName : (DisplayName ?? "");
            }
        }

        public LegacyDatabaseService(string sectionsPath)
        {
            _sectionsPath = sectionsPath;
            _xmlPath = Path.Combine(_sectionsPath, "SectionDatabaseInfo.xml");
        }

        public List<ZoneInfo> GetZones()
        {
            var zones = new List<ZoneInfo>();
            if (!File.Exists(_xmlPath)) return zones;

            var doc = XDocument.Load(_xmlPath);
            // CategoryID="1" is usually "Rolled Steel"
            var countries = doc.Descendants("Country")
                .Where(x => (string)x.Attribute("CategoryID") == "1")
                .Select(x => new
                {
                    Name = (string)x.Attribute("Name"),
                    DbName = (string)x.Attribute("DefaultDbName"),
                    Id = (int)x.Attribute("ID")
                });

            foreach (var c in countries)
            {
                if (_allowedZones.Contains(c.Name))
                {
                    // Some names might be duplicated in XML; return unique names.
                    if (!zones.Any(z => z.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Lookup STAAD API country code from zone name.
                        int staadCode = 1; // Default to American
                        if (_staadCountryCodes.TryGetValue(c.Name, out int code))
                            staadCode = code;

                        zones.Add(new ZoneInfo
                        {
                            Name = c.Name,
                            DbFileName = c.DbName + ".db3",
                            CountryId = c.Id,
                            StaadCountryCode = staadCode
                        });
                    }
                }
            }

            return zones.OrderBy(z => z.Name).ToList();
        }

        public List<string> GetShapes(string dbFileName)
        {
            var shapes = new List<string>();
            string dbPath = Path.Combine(_sectionsPath, dbFileName);
            if (!File.Exists(dbPath)) return shapes;

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;Read Only=True;"))
            {
                conn.Open();
                // Get all table names
                string sql = "SELECT name FROM sqlite_master WHERE type='table';";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tableName = reader.GetString(0);
                        // Filter out likely non-shape tables if any (e.g. metadata)
                        if (!IsSystemOrMetaTable(tableName))
                        {
                            shapes.Add(tableName);
                        }
                    }
                }
            }

            return shapes.OrderBy(s => s).ToList();
        }


        public List<SectionItem> GetSections(string dbFileName, string shapeTableName)
        {
            var sections = new List<SectionItem>();
            string dbPath = Path.Combine(_sectionsPath, dbFileName);
            if (!File.Exists(dbPath)) return sections;

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;Read Only=True;"))
            {
                conn.Open();
                // Check if table has "SectionName" or similar column.
                var cols = DetectSectionColumns(conn, shapeTableName);
                if (string.IsNullOrEmpty(cols.DisplayCol)) return sections;

                // Note: column names may contain spaces/special chars; bracket them for safety.
                string displayCol = $"[{cols.DisplayCol}]";
                string staadCol = cols.StaadCol != null ? $"[{cols.StaadCol}]" : null;

                string sql = staadCol != null && !cols.StaadCol.Equals(cols.DisplayCol, StringComparison.OrdinalIgnoreCase)
                    ? $"SELECT DISTINCT {displayCol}, {staadCol} FROM [{shapeTableName}] ORDER BY {displayCol}"
                    : $"SELECT DISTINCT {displayCol} FROM [{shapeTableName}] ORDER BY {displayCol}";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0)) continue;
                        string displayName = reader.GetString(0)?.Trim();
                        string staadName = displayName;
                        if (cols.StaadCol != null && reader.FieldCount > 1 && !reader.IsDBNull(1))
                        {
                            staadName = reader.GetString(1)?.Trim();
                        }

                        if (string.IsNullOrEmpty(displayName)) continue;
                        if (string.IsNullOrEmpty(staadName)) staadName = displayName;

                        sections.Add(new SectionItem
                        {
                            DisplayName = displayName,
                            StaadName = staadName
                        });
                    }
                }
            }

            return sections;
        }

        private bool IsSystemOrMetaTable(string tableName)
        {
             var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
             { 
                 "Version", "Revision", "Units", "Source", "Description",
                 "DBInfo", "Field Units",
                 "android_metadata", "Filter" 
             };
             return ignored.Contains(tableName);
        }

        private (string DisplayCol, string StaadCol) DetectSectionColumns(SQLiteConnection conn, string tableName)
        {
            // Heuristic to find the column holding the section name
            // Valid candidates: "SectionName", "Name", "Designation", "Shape", "Type", "StaadName"
            // We fetch the schema
            using (var cmd = new SQLiteCommand($"PRAGMA table_info([{tableName}])", conn))
            using (var reader = cmd.ExecuteReader())
            {
                var columns = new List<string>();
                while (reader.Read())
                {
                    columns.Add(reader["name"].ToString());
                }

                string staadCol = columns.FirstOrDefault(c => c.Equals("StaadName", StringComparison.OrdinalIgnoreCase));

                // Preference list for display
                var candidates = new[] { "Name", "SectionName", "Section_Name", "Designation", "Shape", "Type" };
                foreach (var c in candidates)
                {
                    if (columns.Contains(c, StringComparer.OrdinalIgnoreCase))
                        return (columns.First(x => x.Equals(c, StringComparison.OrdinalIgnoreCase)), staadCol);
                }
                
                // Fallback: first column that is text? Or just the first column?
                // Often the first column is the key.
                if (columns.Count > 0) return (columns[0], staadCol);
            }
            return (null, null);
        }
    }
}
