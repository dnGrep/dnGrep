using System;
using System.Collections.Generic;
using System.Text;

namespace dnGREP.Engines
{
	public class GrepEngineFactory
	{
		public static IGrepEngine GetEngine(string fileName, bool showLinesInContext, int linesBefore, int linesAfter)
		{
			// TODO: Implement factory
			GrepEnginePlainText plainTextEngine = new GrepEnginePlainText(showLinesInContext, linesBefore, linesAfter);
			return plainTextEngine;
		}
	}
}
