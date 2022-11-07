using System;
using System.Collections.Generic;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace NeosProLauncher
{
	// Token: 0x02000005 RID: 5
	public static class KeyVerification
	{
		// Token: 0x0600000E RID: 14 RVA: 0x000020D8 File Offset: 0x000002D8
		public static string GetHardwareUUID()
		{
			string str = GetHardwareString();
			str = Convert.ToBase64String(GetHash(str));
			return str.Substring(0, str.Length - 2);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002108 File Offset: 0x00000308
		private static byte[] GetHash(string inputString)
		{
			byte[] array;
			using (MD5 algorithm = MD5.Create())
			{
				array = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
			}
			return array;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000214C File Offset: 0x0000034C
		private static string GetHardwareString()
		{
			return BaseId() + CpuId() + BiosId();
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002162 File Offset: 0x00000362
		private static string Identifier(string wmiClass, string property)
		{
			return Identifier(wmiClass, new List<string> { property });
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002178 File Offset: 0x00000378
		private static string Identifier(string wmiClass, List<string> properties)
		{
			StringBuilder str = new StringBuilder();
			using (ManagementClass mc = new ManagementClass(wmiClass))
			{
				foreach (ManagementBaseObject mo in mc.GetInstances())
				{
					for (int i = properties.Count - 1; i >= 0; i--)
					{
						object p = mo.GetPropertyValue(properties[i]);
						if (p != null)
						{
							str.AppendLine(p.ToString());
							properties.RemoveAt(i);
						}
					}
					if (properties.Count == 0)
					{
						break;
					}
				}
			}
			return str.ToString();
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002234 File Offset: 0x00000434
		private static string CpuId()
		{
			return Identifier("Win32_Processor", new List<string> { "UniqueId", "ProcessorId", "Name", "Manufacturer" });
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002274 File Offset: 0x00000474
		private static string BiosId()
		{
			return Identifier("Win32_BIOS", new List<string> { "Manufacturer", "SMBIOSBIOSVersion", "IdentificationCode", "SerialNumber", "ReleaseDate", "Version" });
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000022D2 File Offset: 0x000004D2
		private static string BaseId()
		{
			return Identifier("Win32_BaseBoard", new List<string> { "Model", "Manufacturer", "Name", "SerialNumber" });
		}
	}
}
