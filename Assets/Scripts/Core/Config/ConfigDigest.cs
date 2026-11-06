using System;
using System.Collections.Generic;
using LiteSim;

namespace LiteGame.Core.Config
{
    /// <summary>玩法数据身份摘要：判定面表字节 → uint；算法单源 LiteSim.CombatConfigDigest，本类只取表与顺序。</summary>
    public static class ConfigDigest
    {
        public static uint Compute(IReadOnlyDictionary<string, byte[]> bytesByTableName)
        {
            if (bytesByTableName == null)
            {
                throw new ArgumentNullException(nameof(bytesByTableName));
            }

            var files = ConfigTableLayout.JudgmentFiles;
            var entries = new CombatConfigDigest.Entry[files.Length];
            for (var i = 0; i < files.Length; i++)
            {
                var name = ConfigTableLayout.TableName(files[i]);
                if (!bytesByTableName.TryGetValue(name, out var bytes) || bytes == null)
                {
                    throw new InvalidOperationException("判定面表缺字节：" + files[i]);
                }

                entries[i] = new CombatConfigDigest.Entry(files[i], bytes);
            }

            return CombatConfigDigest.ComputeBase(entries);
        }
    }
}
