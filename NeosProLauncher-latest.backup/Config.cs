using System;
using System.Text.Json.Serialization;

namespace NeosProLauncher
{
	// Token: 0x02000004 RID: 4
	public class Config
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000B RID: 11 RVA: 0x000020C7 File Offset: 0x000002C7
		// (set) Token: 0x0600000C RID: 12 RVA: 0x000020CF File Offset: 0x000002CF
		[JsonPropertyName("supressKey")]
		public bool SupressKey { get; set; }
	}
}
