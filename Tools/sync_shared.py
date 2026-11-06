#!/usr/bin/env python3
"""从旧仓同步生成物（Lua 表 / 配置表 C#）并写 sha256 清单（02 §6.2）。

用法：
  python Tools/sync_shared.py            # 同步 Lua 表（P0 默认）
  python Tools/sync_shared.py --config   # 连配置表 C# 一起同步（需 Luban 运行时包）
  python Tools/sync_shared.py --check    # 只比对，不写盘；有漂移返回 1
"""
import argparse
import hashlib
import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OLD_REPO = ROOT.parent / "Test"
MANIFEST = ROOT / "Tools" / "sync-manifest.json"

# 源目录（旧仓相对） → 目标目录（本工程相对）
LUA = ("Assets/LiteGame/Lua/Cfg", "Assets/Scripts/Lua/cfg")
DATA = ("Assets/GameData/Config", "Assets/GameData/Config")
CONFIG = ("Assets/GameData/Generated", "Assets/Scripts/Core/Config/Generated")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    digest.update(path.read_bytes())
    return digest.hexdigest()


def collect(source: Path):
    if not source.exists():
        return []
    # 跳过 .meta（Unity 按目标工程重新生成）与 .asmdef（生成物并入 LiteGame.Core，asmdef 预算固定 5 个）
    return sorted(p for p in source.rglob("*")
                  if p.is_file() and p.suffix not in (".meta", ".asmdef"))


def sync_pair(source_rel, target_rel, check_only):
    source = OLD_REPO / source_rel
    target = ROOT / target_rel
    files = collect(source)
    if not files:
        print(f"跳过：旧仓没有 {source_rel}（换机后确认旧仓路径）")
        return {}, []

    copied = []
    entries = {}
    for item in files:
        rel = item.relative_to(source)
        dest = target / rel
        digest = sha256(item)
        entries[f"{target_rel}/{rel.as_posix()}"] = digest
        if dest.exists() and sha256(dest) == digest:
            continue
        copied.append(dest.relative_to(ROOT).as_posix())
        if not check_only:
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(item, dest)
    return entries, copied


def git_head():
    try:
        result = subprocess.run(
            ["git", "-C", str(OLD_REPO), "rev-parse", "--short", "HEAD"],
            capture_output=True, text=True, encoding="utf-8", errors="replace")
        return result.stdout.strip() if result.returncode == 0 else ""
    except OSError:
        return ""


def main() -> int:
    parser = argparse.ArgumentParser(description="同步旧仓生成物")
    parser.add_argument("--config", action="store_true", help="同时同步配置表 C#（需 Luban 运行时）")
    parser.add_argument("--check", action="store_true", help="只比对，不写盘")
    args = parser.parse_args()

    if not OLD_REPO.exists():
        print(f"旧仓不存在：{OLD_REPO}（02 §6.2 要求同级目录）")
        return 1

    pairs = [LUA, DATA] + ([CONFIG] if args.config else [])
    entries, copied = {}, []
    for source_rel, target_rel in pairs:
        got, changed = sync_pair(source_rel, target_rel, args.check)
        entries.update(got)
        copied.extend(changed)

    if args.check:
        print(f"比对 {len(entries)} 个文件，漂移 {len(copied)} 个")
        for name in copied:
            print("  " + name)
        return 1 if copied else 0

    manifest = {"_sourceHead": git_head(), "_syncedAt": datetime.now(timezone.utc).isoformat(timespec="seconds")}
    manifest.update(entries)
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"同步 {len(entries)} 个文件（更新 {len(copied)} 个），清单 {MANIFEST.relative_to(ROOT)}")
    if not args.config:
        print("提示：配置表 C# 未同步（P1 接 ConfigHost 时加 --config，并先引 com.code-philosophy.luban）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
