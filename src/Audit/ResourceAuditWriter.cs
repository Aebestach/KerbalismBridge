using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KERBALISM;

namespace KerbalismResourceAudit
{
	static class ResourceAuditWriter
	{
		public static string LogRoot =>
			Path.Combine(KSPUtil.ApplicationRootPath, "Logs", "zKerbalismResourceAudit");

		public static string WriteScan(List<AuditFinding> findings)
		{
			string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
			string scanDir = Path.Combine(LogRoot, "scan_" + stamp);
			Directory.CreateDirectory(scanDir);

			WriteSummary(scanDir, stamp, findings);
			WriteParts(scanDir, findings);
			WriteByMod(scanDir, findings);
			WriteByModule(scanDir, findings);

			return scanDir;
		}

		static void WriteSummary(string scanDir, string stamp, List<AuditFinding> findings)
		{
			string path = Path.Combine(scanDir, "summary.log");
			using (var w = new StreamWriter(path, false, Encoding.UTF8))
			{
				w.WriteLine("# zKerbalismResourceAudit — summary");
				w.WriteLine("# scan: " + stamp);
				w.WriteLine("# kerbalism: " + Lib.KerbalismVersion);
				w.WriteLine("# parts loaded: " + PartLoader.LoadedPartsList.Count);
				w.WriteLine("# findings: " + findings.Count);
				w.WriteLine();

				var byMod = findings.GroupBy(f => f.ModFolder).OrderByDescending(g => g.Count());
				w.WriteLine("## by mod folder");
				foreach (var g in byMod)
					w.WriteLine(g.Count().ToString().PadLeft(5) + "  " + g.Key);

				w.WriteLine();
				var byModule = findings.GroupBy(f => f.ModuleName).OrderByDescending(g => g.Count());
				w.WriteLine("## by module name");
				foreach (var g in byModule)
					w.WriteLine(g.Count().ToString().PadLeft(5) + "  " + g.Key);
			}
		}

		static void WriteParts(string scanDir, List<AuditFinding> findings)
		{
			string path = Path.Combine(scanDir, "parts-unintegrated.log");
			using (var w = new StreamWriter(path, false, Encoding.UTF8))
			{
				w.WriteLine("# parts with stock/native resource modules not integrated with Kerbalism/Bridge");
				w.WriteLine("# columns: mod | part | title | module | resources | config url");
				w.WriteLine();

				foreach (AuditFinding f in findings.OrderBy(x => x.ModFolder).ThenBy(x => x.PartName).ThenBy(x => x.ModuleName))
				{
					w.WriteLine(
						f.ModFolder + " | " +
						f.PartName + " | " +
						f.PartTitle + " | " +
						f.ModuleName + " | " +
						(string.IsNullOrEmpty(f.Resources) ? "-" : f.Resources) + " | " +
						f.PartUrl);
				}
			}
		}

		static void WriteByMod(string scanDir, List<AuditFinding> findings)
		{
			string path = Path.Combine(scanDir, "by-mod.log");
			using (var w = new StreamWriter(path, false, Encoding.UTF8))
			{
				w.WriteLine("# findings grouped by GameData mod folder");
				w.WriteLine();

				foreach (var modGroup in findings.GroupBy(f => f.ModFolder).OrderBy(g => g.Key))
				{
					w.WriteLine("[" + modGroup.Key + "] (" + modGroup.Count() + ")");
					foreach (AuditFinding f in modGroup.OrderBy(x => x.PartName).ThenBy(x => x.ModuleName))
					{
						w.WriteLine(
							"  " + f.PartName + " | " + f.PartTitle + " | " + f.ModuleName +
							" | " + (string.IsNullOrEmpty(f.Resources) ? "-" : f.Resources));
					}
					w.WriteLine();
				}
			}
		}

		static void WriteByModule(string scanDir, List<AuditFinding> findings)
		{
			string path = Path.Combine(scanDir, "by-module.log");
			using (var w = new StreamWriter(path, false, Encoding.UTF8))
			{
				w.WriteLine("# findings grouped by PartModule name");
				w.WriteLine();

				foreach (var moduleGroup in findings.GroupBy(f => f.ModuleName).OrderBy(g => g.Key))
				{
					w.WriteLine("[" + moduleGroup.Key + "] (" + moduleGroup.Count() + ")");
					foreach (AuditFinding f in moduleGroup.OrderBy(x => x.ModFolder).ThenBy(x => x.PartName))
					{
						w.WriteLine(
							"  " + f.ModFolder + " | " + f.PartName + " | " + f.PartTitle +
							" | " + (string.IsNullOrEmpty(f.Resources) ? "-" : f.Resources));
					}
					w.WriteLine();
				}
			}
		}
	}
}
