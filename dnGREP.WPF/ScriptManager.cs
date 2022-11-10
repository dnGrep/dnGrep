using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnGREP.Common;
using System.Windows;
using NLog;

namespace dnGREP.WPF
{
    public class ScriptManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static ScriptManager Instance { get; } = new ScriptManager();

        private ScriptManager() 
        {
            LoadScripts();
        }

        private readonly IDictionary<string, string> _scripts = new Dictionary<string, string>();

        public ICollection<string> Scripts { get { return _scripts.Keys; } }

        private void LoadScripts()
        {
            string dataFolder = Utils.GetDataFolderPath();
            foreach (string fileName in Directory.GetFiles(dataFolder, "*.script", SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                if (!_scripts.ContainsKey(name))
                {
                    _scripts.Add(name, fileName);
                }
            }
        }

    }
}
