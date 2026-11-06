#!/usr/bin/env python3
"""独立复算玩法数据身份摘要（对照 LiteGame.Core.Config.ConfigDigest）。

算法（与 LiteSim.CombatConfigDigest 一致）：
  规范文本 = "gameplay-digest-v1\n" + 每张判定面表 "<领域路径> <表字节 sha256>\n"
  摘要     = sha256(规范文本) 前 4 字节按小端转 uint

判定面清单不在这里重复：直接解析 ConfigTableLayout.cs 的 Files / JudgmentDomains。
"""
import hashlib
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LAYOUT = ROOT / "Assets" / "Scripts" / "Core" / "Config" / "ConfigTableLayout.cs"
DATA = ROOT / "Assets" / "GameData" / "Config"


def parse_list(name):
    text = LAYOUT.read_text(encoding="utf-8")
    block = re.search(name + r"\s*=\s*\{(.*?)\};", text, re.S)
    if not block:
        raise SystemExit("ConfigTableLayout 里找不到 " + name)
    return re.findall(r'"([^"]+)"', block.group(1))


def main():
    files = parse_list("Files")
    domains = set(parse_list("JudgmentDomains"))
    judgment = [f for f in files if f.split("/")[0] in domains]
    lines = ["gameplay-digest-v1"]
    for name in judgment:
        data = (DATA / (name + ".bytes")).read_bytes()
        lines.append(name + " " + hashlib.sha256(data).hexdigest())
    digest = int.from_bytes(hashlib.sha256(("\n".join(lines) + "\n").encode()).digest()[:4], "little")
    print("0x%08x  (%d judgment tables)" % (digest, len(judgment)))


if __name__ == "__main__":
    main()
