#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把旧仓打过补丁的 com.unity.pipeline 嵌入包还原到本工程 Packages/。

为什么需要它：
  Unity 官方 `com.unity.pipeline` 0.7.0-exp.1 声明最低 Unity 6000.0；本工程是 2022.3.55f1c1，
  用的是旧仓落位并打补丁的同一份（13.4 MB / 822 文件，补丁见 02 §5.1）。
  它是 Unity 版权件，不入库 —— 新克隆/换机后跑一次本脚本。

用法：
  python Tools/restore_pipeline.py                       # 从 ..\\Test\\Packages\\com.unity.pipeline 复制
  python Tools/restore_pipeline.py --from D:\\somewhere  # 指定来源（该目录下应有 com.unity.pipeline）
  python Tools/restore_pipeline.py --force               # 目标已存在时覆盖

退出码：0 成功；1 失败（来源缺失/目标已存在/复制异常）。
"""

import argparse
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Packages" / "com.unity.pipeline"
DEFAULT_SOURCE_ROOT = ROOT.parent / "Test" / "Packages"


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description="还原 com.unity.pipeline 嵌入包")
    parser.add_argument("--from", dest="source_root", default=str(DEFAULT_SOURCE_ROOT),
                        help="旧仓 Packages 目录（默认 ..\\Test\\Packages）")
    parser.add_argument("--force", action="store_true", help="目标已存在时覆盖")
    args = parser.parse_args(argv)

    source = Path(args.source_root) / "com.unity.pipeline"
    if not (source / "package.json").exists():
        print(f"[FAIL] 来源不存在或不完整：{source}")
        print("       指定 --from <旧仓 Packages 目录>，或确认旧仓路径")
        return 1

    if TARGET.exists() and not args.force:
        count = sum(1 for _ in TARGET.rglob("*") if _.is_file())
        print(f"[SKIP] 已存在：{TARGET}（{count} 文件）。要覆盖加 --force")
        return 0

    if TARGET.exists():
        shutil.rmtree(TARGET)
    shutil.copytree(source, TARGET)

    files = sum(1 for _ in TARGET.rglob("*") if _.is_file())
    mb = sum(p.stat().st_size for p in TARGET.rglob("*") if p.is_file()) / (1024 * 1024)
    print(f"[OK] 已还原 {files} 文件 / {mb:.1f} MB -> {TARGET}")
    print("     首次打开 Unity 会导入该包（约 1~3 分钟）；之后 unity CLI 才能连编辑器。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
