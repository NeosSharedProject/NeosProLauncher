using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace NeosProLauncher
{
	// Token: 0x02000003 RID: 3
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class License
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000004 RID: 4 RVA: 0x0000208C File Offset: 0x0000028C
		// (set) Token: 0x06000005 RID: 5 RVA: 0x00002094 File Offset: 0x00000294
		[JsonProperty(PropertyName = "licenseGroup")]
		[JsonPropertyName("licenseGroup")]
		public string LicenseGroup { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000006 RID: 6 RVA: 0x0000209D File Offset: 0x0000029D
		// (set) Token: 0x06000007 RID: 7 RVA: 0x000020A5 File Offset: 0x000002A5
		[JsonProperty(PropertyName = "licenseKey")]
		[JsonPropertyName("licenseKey")]
		public string LicenseKey { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000008 RID: 8 RVA: 0x000020AE File Offset: 0x000002AE
		// (set) Token: 0x06000009 RID: 9 RVA: 0x000020B6 File Offset: 0x000002B6
		[JsonProperty("pairedMachineUUID")]
		[JsonPropertyName("pairedMachineUUID")]
		public string PairedMachineUUID { get; set; }
	}
}
