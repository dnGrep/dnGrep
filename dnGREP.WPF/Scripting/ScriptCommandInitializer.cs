using System;
using System.Collections.Generic;
using System.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    internal static class ScriptCommandInitializer
    {
        internal static void LoadScriptCommands(ref List<ScriptCommandDefinition> scriptCommands,
            ref List<ScriptingCompletionData> commandCompletionData)
        {
            int cmdPriority = 100;
            int targPriority = 100;

            var cmd = new ScriptCommandDefinition()
            {
                Command = "set",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_set
            };
            scriptCommands.Add(cmd);

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "folder",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_set_folder,
                ValueHint = Resources.ScriptHint_set_folder_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "pathtomatch",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_set_pathtomatch,
                ValueHint = Resources.ScriptHint_set_pathtomatch_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "pathtoignore",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_set_pathtoignore,
                ValueHint = Resources.ScriptHint_set_pathtoignore_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "searchfor",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_set_searchfor,
                ValueHint = Resources.ScriptHint_set_searchfor_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "replacewith",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_set_replacewith,
                ValueHint = Resources.ScriptHint_set_replacewith_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "casesensitive",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_casesensitive,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "wholeword",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_wholeword,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "multiline",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_multiline,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "dotasnewline",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_dotasnewline,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "booleanoperators",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_booleanoperators,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "patterntype",
                Priority = targPriority--,
                ValueType = typeof(FileSearchType),
                Description = Resources.ScriptHint_set_paterntype,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "searchinarchives",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_searchinarchives,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "sorttype",
                Priority = targPriority--,
                ValueType = typeof(SortType),
                Description = Resources.ScriptHint_set_sorttype,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "sortdirection",
                Priority = targPriority--,
                ValueType = typeof(ListSortDirection),
                Description = Resources.ScriptHint_set_sortdirection,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "includehidden",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_includehidden,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "includebinary",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_includebinary,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "filterbyfilesize",
                Priority = targPriority--,
                ValueType = typeof(FileSizeFilter),
                Description = Resources.ScriptHint_set_filterbyfilesize,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "sizefrom",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_sizefrom,
                ValueHint = Resources.ScriptHint_set_sizefrom_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "sizeto",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_sizeto,
                ValueHint = Resources.ScriptHint_set_sizeto_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "includesubfolder",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_includesubfolder,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "maxsubfolderdepth",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_maxsubfolderdepth,
                ValueHint = Resources.ScriptHint_set_maxsubfolderdepth_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "followsymlinks",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_followsymlinks,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "filedatefilter",
                Priority = targPriority--,
                ValueType = typeof(FileDateFilter),
                Description = Resources.ScriptHint_set_filedatefilter,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "filetimerange",
                Priority = targPriority--,
                ValueType = typeof(FileTimeRange),
                Description = Resources.ScriptHint_set_filetimerange,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "startdate",
                Priority = targPriority--,
                ValueType = typeof(DateTime),
                AllowNullValue = true,
                Description = Resources.ScriptHint_set_startdate,
                ValueHint = Resources.ScriptHint_set_startdate_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "enddate",
                Priority = targPriority--,
                ValueType = typeof(DateTime),
                AllowNullValue = true,
                Description = Resources.ScriptHint_set_enddate,
                ValueHint = Resources.ScriptHint_set_startdate_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "hoursfrom",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_hoursfrom,
                ValueHint = Resources.ScriptHint_set_hoursfrom_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "hoursto",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_hoursto,
                ValueHint = Resources.ScriptHint_set_hoursto_value,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "searchtype",
                Priority = targPriority--,
                ValueType = typeof(SearchType),
                Description = Resources.ScriptHint_set_searchtype,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "capturegroupsearch",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_capturegroupsearch,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "searchinresults",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_searchinresults,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "previewfile",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_previewfile,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "stopafterfirstmatch",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_stopafterfirstmatch,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "searchparallel",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_searchparallel,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "usegitignore",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_usegitignore,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "skipremotecloudstoragefiles",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_skipremotecloudstoragefiles,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "encoding",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_encoding,
                ValueHint = Resources.ScriptHint_set_encoding_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "highlightmatches",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_highlightmatches,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "highlightgroups",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_highlightgroups,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "showcontextlines",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_showcontextlines,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "contextlinesbefore",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_contextlinesbefore,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "contextlinesafter",
                Priority = targPriority--,
                ValueType = typeof(int),
                Description = Resources.ScriptHint_set_contextlinesafter,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "wraptext",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_wraptext,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "resultszoom",
                Priority = targPriority--,
                ValueType = typeof(double),
                Description = Resources.ScriptHint_set_resultszoom,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "reportmode",
                Priority = targPriority--,
                ValueType = typeof(ReportMode),
                Description = Resources.ScriptHint_set_reportmode,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "fileinformation",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_fileinformation,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "trimwhitespace",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_trimwhitespace,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "uniquevalues",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_uniquevalues,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "uniquescope",
                Priority = targPriority--,
                ValueType = typeof(UniqueScope),
                Description = Resources.ScriptHint_set_uniquescope,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "separatelines",
                Priority = targPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_set_separatelines,
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "listitemseparator",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_set_listitemseparator,
                ValueHint = Resources.ScriptHint_set_listitemseparator_value
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "search",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_search
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "replace",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_replace
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "sort",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_sort
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "copyfilenames",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_copyfilenames
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "copyresults",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_copyresults
            });


            cmd = new ScriptCommandDefinition()
            {
                Command = "report",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_report
            };
            scriptCommands.Add(cmd);

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "full",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_report_full,
                ValueHint = Resources.ScriptHint_report_arg_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "text",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_report_text,
                ValueHint = Resources.ScriptHint_report_arg_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "csv",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_report_csv,
                ValueHint = Resources.ScriptHint_report_arg_value
            });


            cmd = new ScriptCommandDefinition()
            {
                Command = "run",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_run
            };
            scriptCommands.Add(cmd);

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "powershell",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_run_powershell,
                ValueHint = Resources.ScriptHint_run_powershell_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "cmd",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_run_cmd,
                ValueHint = Resources.ScriptHint_run_cmd_value
            });

            
            cmd = new ScriptCommandDefinition()
            {
                Command = "env",
                Priority = cmdPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_env_cmd,
                ValueHint = Resources.ScriptHint_env_cmd_value
            };
            scriptCommands.Add(cmd);

            
            cmd = new ScriptCommandDefinition()
            {
                Command = "log",
                Priority = cmdPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_log_cmd,
                ValueHint = Resources.ScriptHint_log_cmd_value
            };
            scriptCommands.Add(cmd);


            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "copyfiles",
                Priority = cmdPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_copyfiles,
                ValueHint = Resources.ScriptHint_copyfiles_value,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "movefiles",
                Priority = cmdPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_movefiles,
                ValueHint = Resources.ScriptHint_movefiles_value,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "deletefiles",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_deletefiles,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "undo",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_undo,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "resetfilters",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_resetfilters,
            });


            cmd = new ScriptCommandDefinition()
            {
                Command = "bookmark",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_bookmark
            };
            scriptCommands.Add(cmd);

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "use",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_bookmark_use,
                ValueHint = Resources.ScriptHint_bookmark_use_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "add",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_bookmark_add,
                ValueHint = Resources.ScriptHint_bookmark_add_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "addfolder",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_bookmark_addfolder,
                ValueHint = Resources.ScriptHint_bookmark_add_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "remove",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_bookmark_remove,
                ValueHint = Resources.ScriptHint_bookmark_remove_value
            });

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "removefolder",
                Priority = targPriority--,
                ValueType = typeof(string),
                AllowNullValue = true,
                Description = Resources.ScriptHint_bookmark_removefolder,
                ValueHint = Resources.ScriptHint_bookmark_remove_value
            });


            cmd = new ScriptCommandDefinition()
            {
                Command = "include",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_include,
            };
            scriptCommands.Add(cmd);

            cmd.Targets.Add(new ScriptTargetDefinition()
            {
                Target = "script",
                Priority = targPriority--,
                ValueType = typeof(string),
                Description = Resources.ScriptHint_include_script,
                ValueHint = Resources.ScriptHint_include_script_value
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "expandfilefilters",
                Priority = cmdPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_expandfilefilters,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "maximizeresults",
                Priority = cmdPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_maximizeresults,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "expandresultoptions",
                Priority = cmdPriority--,
                ValueType = typeof(bool),
                Description = Resources.ScriptHint_expandresultoptions,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "messages",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_messages,
            });

            scriptCommands.Add(new ScriptCommandDefinition()
            {
                Command = "exit",
                Priority = cmdPriority--,
                Description = Resources.ScriptHint_exit,
            });

            scriptCommands.Sort((x, y) => string.Compare(x.Command, y.Command, StringComparison.Ordinal));

            foreach (var item in scriptCommands)
            {
                item.Initialize();

                commandCompletionData.Add(new ScriptingCompletionData(item));
            }

        }
    }
}
