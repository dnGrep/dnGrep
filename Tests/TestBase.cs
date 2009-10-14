using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using System.Reflection;
using System.IO;

namespace Tests
{
	public class TestBase
	{
		public string GetDllPath()
		{
			Assembly thisAssembly = Assembly.GetAssembly(typeof(TestBase));
			return Path.GetDirectoryName(thisAssembly.Location);
		}
	}
}
