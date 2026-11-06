#!/usr/bin/env python3
"""从旧仓按 GUID 依赖闭包导入资产（只导资产，不导旧代码）。

用法:
  python Tools/import_old_assets.py Assets/Scenes/TrainingGround.unity --dry-run
  python Tools/import_old_assets.py Assets/.../Player(Kazuko).prefab --to Assets/Imported

规则:
  - 扫描旧仓 *.meta 建 GUID -> 路径索引；
  - 文本资产（.prefab/.unity/.mat/.asset/.controller/.anim/...）解析 guid 引用递归；
  - .cs 不复制（重写），只记录“需要重接的脚本 GUID”；
  - 复制时保留 .meta（同 GUID，第三方包已按同源还原）；
  - 目标路径 = --to + 旧仓 Assets/ 之后的相对路径。
"""
import argparse
import re
import shutil
import sys
from collections import deque
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OLD = ROOT.parent / "Test"
OLD_ASSETS = OLD / "Assets"
GUID_RE = re.compile(r"guid: ([0-9a-f]{32})")
TEXT_SUFFIXES = {".prefab", ".unity", ".mat", ".asset", ".controller", ".anim", ".overrideController",
                 ".playable", ".physicMaterial", ".spriteatlas", ".shader", ".json", ".lighting", ".mask"}


def build_index():
    index = {}
    for meta in OLD_ASSETS.rglob("*.meta"):
        text = meta.read_text(encoding="utf-8", errors="replace")[:400]
        match = GUID_RE.search(text)
        if match:
            index[match.group(1)] = meta.with_suffix("")
    return index


def deps_of(path: Path):
    if path.suffix.lower() not in TEXT_SUFFIXES:
        return set()
    text = path.read_text(encoding="utf-8", errors="replace")
    return set(GUID_RE.findall(text))


def main() -> int:
    parser = argparse.ArgumentParser(description="按 GUID 闭包导入旧仓资产")
    parser.add_argument("assets", nargs="+", help="旧仓相对路径（Assets/...）")
    parser.add_argument("--to", default="Assets/Imported", help="目标根目录（默认 Assets/Imported）")
    parser.add_argument("--dry-run", action="store_true", help="只统计，不复制")
    args = parser.parse_args()

    if not OLD_ASSETS.exists():
        print("旧仓不存在：" + str(OLD_ASSETS))
        return 1

    index = build_index()
    queue = deque()
    for item in args.assets:
        path = OLD / item
        if not path.exists():
            print("找不到：" + item)
            return 1
        if path.is_dir():
            # 目录：整树导入（shader 的 include 是路径引用，GUID 闭包抓不到）
            queue.extend(p for p in path.rglob("*") if p.is_file() and p.suffix != ".meta")
        else:
            queue.append(path)

    seen, scripts, missing = set(), set(), set()
    while queue:
        path = queue.popleft()
        if path in seen:
            continue
        seen.add(path)
        for guid in deps_of(path):
            dep = index.get(guid)
            if dep is None:
                missing.add(guid)
            elif dep.suffix == ".cs":
                scripts.add(dep)
            elif dep in seen:
                continue
            else:
                queue.append(dep)

    files = [p for p in seen if p.suffix != ".cs"]
    total = sum(p.stat().st_size for p in files)
    by_ext = {}
    for p in files:
        by_ext[p.suffix.lower()] = by_ext.get(p.suffix.lower(), 0) + 1
    print(f"资产 {len(files)} 个，{total / 1024 / 1024:.1f} MB")
    for big in sorted(files, key=lambda p: p.stat().st_size, reverse=True)[:5]:
        print(f"  大文件 {big.stat().st_size / 1024 / 1024:.1f} MB  {big.relative_to(OLD)}")
    print("按类型：" + ", ".join(f"{k or '(无后缀)'}×{v}" for k, v in sorted(by_ext.items(), key=lambda kv: -kv[1])))
    print(f"旧脚本引用 {len(scripts)} 个（不复制，需在新组件上重接）")
    if missing:
        print(f"未解析 GUID {len(missing)} 个（第三方包缺失或旧仓外引用）")

    if args.dry_run:
        return 0

    target_root = ROOT / args.to
    copied = 0
    for path in files:
        rel = path.relative_to(OLD)
        dest = target_root / rel.relative_to("Assets")
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, dest)
        meta = Path(str(path) + ".meta")
        if meta.exists():
            shutil.copy2(meta, Path(str(dest) + ".meta"))
        copied += 1
    print(f"已复制 {copied} 个到 {args.to}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
