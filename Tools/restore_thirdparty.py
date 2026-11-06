#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""还原不入库的第三方原件（UniTask / xLua）。

第三方原件按 02 §5 清单还原，不入库（`.gitignore` 已排除 `Assets/ThirdParty/`）：

    python Tools/restore_thirdparty.py                       # 从 ..\\Test\\Assets\\ThirdParty 复制
    python Tools/restore_thirdparty.py --from D:\\somewhere  # 指定来源（其下应有 ThirdParty 结构）
    python Tools/restore_thirdparty.py --force               # 目标已存在时覆盖

退出码：0 成功；1 失败。
"""

import argparse
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets" / "ThirdParty"
DEFAULT_SOURCE = ROOT.parent / "Test" / "Assets" / "ThirdParty"

# 相对 ThirdParty 的路径
ITEMS = (
    Path("Plugins") / "UniTask",
    Path("Plugins") / "x86_64",   # xLua 原生库（xlua.dll / libxlua.so）；漏了它 LuaEnv 构造会 DllNotFoundException
    Path("Plugins") / "Demigiant",  # DOTween（DLL 形态：预编译程序集，asmdef 无需引用）
    Path("XLua"),
)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description="还原 UniTask / xLua")
    parser.add_argument("--from", dest="source", default=str(DEFAULT_SOURCE),
                        help="旧仓 Assets\\ThirdParty 目录（默认 ..\\Test\\Assets\\ThirdParty）")
    parser.add_argument("--force", action="store_true", help="目标已存在时覆盖")
    args = parser.parse_args(argv)

    source = Path(args.source)
    if not source.exists():
        print(f"[FAIL] 来源不存在：{source}")
        return 1

    for item in ITEMS:
        src = source / item
        dst = TARGET / item
        if not src.exists():
            print(f"[FAIL] 缺少 {src}")
            return 1
        if dst.exists() and not args.force:
            count = sum(1 for _ in dst.rglob("*") if _.is_file())
            print(f"[SKIP] {dst}（{count} 文件）。要覆盖加 --force")
            continue
        if dst.exists():
            shutil.rmtree(dst)
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copytree(src, dst)
        count = sum(1 for _ in dst.rglob("*") if _.is_file())
        print(f"[OK] {item.as_posix()} -> {count} 文件")

    print("     完成后等 Unity 导入（约 1 分钟），再跑 unity command recompile_status")
    return 0


if __name__ == "__main__":
    sys.exit(main())
