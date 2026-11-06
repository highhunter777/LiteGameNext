using System;
using System.Collections.Generic;

namespace LiteGame.Core.Config
{
    /// <summary>表数据布局单源（对齐旧仓 TableLayout）：领域相对路径 → 判定面清单。
    /// Files 的声明序即摘要规范的一部分，新增表只改这里。</summary>
    public static class ConfigTableLayout
    {
        public const string DataDir = "Assets/GameData/Config/";

        public static readonly string[] Files =
        {
            "Item/tbitemconfig",
            "Movement/tbmovementconfig",
            "Entity/tbentityconfig",
            "UI/tbuiform",
            "UI/tbltext",
            "UI/tbcrosshair",
            "Content/tbcontententry",
            "Strategy/tbstrategy",
            "Strategy/tbbattlestrategy",
            "Combat/tbcombatnum",
            "Combat/tbcombatgeom",
            "Combat/tbweapon",
            "Animation/tbanimationprofile",
            "Map/tbmap",
        };

        public static readonly string[] JudgmentDomains = { "Combat", "Movement", "Entity", "Item", "Map" };

        /// <summary>判定面表清单（按 Files 声明序派生）。</summary>
        public static readonly string[] JudgmentFiles = Derive();

        /// <summary>Luban 装载键：取领域路径末段（裸表名）。</summary>
        public static string TableName(string file)
        {
            var slash = file.LastIndexOf('/');
            return slash < 0 ? file : file.Substring(slash + 1);
        }

        public static string TablePath(string file)
        {
            return DataDir + file + ".bytes";
        }

        private static string[] Derive()
        {
            var files = new List<string>();
            foreach (var file in Files)
            {
                var domain = file.Substring(0, file.IndexOf('/'));
                foreach (var judgment in JudgmentDomains)
                {
                    if (string.Equals(domain, judgment, StringComparison.Ordinal))
                    {
                        files.Add(file);
                        break;
                    }
                }
            }

            if (files.Count == 0)
            {
                throw new InvalidOperationException("判定面表清单为空：JudgmentDomains 与 Files 对不上");
            }

            return files.ToArray();
        }
    }
}
