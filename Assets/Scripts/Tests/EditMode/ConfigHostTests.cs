using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LiteGame.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace LiteGame.Tests.EditMode
{
    /// <summary>ConfigHost / ConfigDigest 契约：真实表、幂等、缺表、未加载访问，以及摘要的黄金值与覆盖面。</summary>
    public sealed class ConfigHostTests
    {
        // 由 Tools/config_digest.py 独立复算（Python 侧算法与 LiteSim.CombatConfigDigest 对齐）；
        // 表数据或判定面清单变化时必须同步更新，否则这里先红。
        private const uint GoldenDigest = 0xa0557022u;

        [Test]
        public void Load_ReadsProjectTables()
        {
            var host = new ConfigHost(ReadProjectTable);

            host.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(host.IsLoaded);
            Assert.IsNotNull(host.Tables.Tbmap);
            Assert.AreNotEqual(0u, host.Digest);
        }

        [Test]
        public void Load_Twice_IsIdempotent()
        {
            var host = new ConfigHost(ReadProjectTable);
            host.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var digest = host.Digest;

            host.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(digest, host.Digest);
        }

        [Test]
        public void Digest_MatchesGolden()
        {
            var host = new ConfigHost(ReadProjectTable);

            host.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(GoldenDigest, host.Digest);
        }

        [Test]
        public void Digest_IgnoresNonJudgmentTables()
        {
            var bytes = ReadAllTables();
            var baseline = ConfigDigest.Compute(bytes);
            bytes["tbuiform"][0] ^= 0xFF;

            Assert.AreEqual(baseline, ConfigDigest.Compute(bytes));
        }

        [Test]
        public void Digest_ChangesWhenJudgmentBytesChange()
        {
            var bytes = ReadAllTables();
            var baseline = ConfigDigest.Compute(bytes);
            bytes["tbweapon"][0] ^= 0xFF;

            Assert.AreNotEqual(baseline, ConfigDigest.Compute(bytes));
        }

        [Test]
        public void Load_MissingTable_ThrowsWithLocation()
        {
            var host = new ConfigHost(_ => throw new FileNotFoundException("missing"));

            var error = Assert.Throws<InvalidOperationException>(
                () => host.LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            StringAssert.Contains("配置表读取失败", error.Message);
        }

        [Test]
        public void Access_BeforeLoad_Throws()
        {
            var host = new ConfigHost(_ => Array.Empty<byte>());

            Assert.Throws<InvalidOperationException>(() => _ = host.Tables);
            Assert.Throws<InvalidOperationException>(() => _ = host.Digest);
        }

        private static Dictionary<string, byte[]> ReadAllTables()
        {
            var bytes = new Dictionary<string, byte[]>(ConfigTableLayout.Files.Length, StringComparer.Ordinal);
            foreach (var file in ConfigTableLayout.Files)
            {
                bytes[ConfigTableLayout.TableName(file)] = ReadProjectTable(ConfigTableLayout.TablePath(file));
            }

            return bytes;
        }

        private static byte[] ReadProjectTable(string location)
        {
            var relative = location.Substring("Assets/".Length);
            return File.ReadAllBytes(Path.Combine(Application.dataPath, relative));
        }
    }
}
