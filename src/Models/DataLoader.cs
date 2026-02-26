using System.Collections.Generic;
using System.Xml.Linq;

namespace AvionPlot.Models {
  public static class DataLoader {
    public static List<RowData> LoadFromXml(string path) {
      var doc = XDocument.Load(path);
      var rows = new List<RowData>();

      foreach (var row in doc.Descendants("ROW")) {
        rows.Add(new RowData {
          OSWES = int.Parse(row.Attribute("OSWES")?.Value ?? "0"),
          WES12 = int.Parse(row.Attribute("WES12")?.Value ?? "0"),
          WES34 = int.Parse(row.Attribute("WES34")?.Value ?? "0"),
          WES56 = int.Parse(row.Attribute("WES56")?.Value ?? "0"),
          WES78 = int.Parse(row.Attribute("WES78")?.Value ?? "0"),
          WES910 = int.Parse(row.Attribute("WES910")?.Value ?? "0"),
          WES1112 = int.Parse(row.Attribute("WES1112")?.Value ?? "0")
        });
      }

      return rows;
    }
  }
}

