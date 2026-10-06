#!/usr/bin/env python3
import json
import os
import re
import subprocess
import sys

text = open(".github/labels.yml", encoding="utf-8").read()
labels = []
for block in re.split(r"\n(?=- )", text.strip()):
    name = re.search(r"^-\s+name:\s+(.+)$", block, re.M)
    color = re.search(r"^  color:\s+(.+)$", block, re.M)
    description = re.search(r"^  description:\s+(.+)$", block, re.M)
    if not name or not color or not description:
        sys.exit(f"label block is incomplete:\n{block}")
    labels.append((name.group(1).strip(), color.group(1).strip(), description.group(1).strip()))

env = os.environ.copy()
for name, color, description in labels:
    result = subprocess.run(
        ["gh", "label", "create", name, "--color", color, "--description", description, "--force"],
        env=env,
    )
    if result.returncode != 0:
        sys.exit(result.returncode)
print(json.dumps([name for name, _, _ in labels]))
